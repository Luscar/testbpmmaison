using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Repositories.Oracle
{
    /// <summary>
    /// Oracle implementation of workflow instance repository
    /// </summary>
    public class OracleWorkflowInstanceRepository : IWorkflowInstanceRepository
    {
        private readonly RepositoryConfiguration _config;

        public OracleWorkflowInstanceRepository(RepositoryConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        protected virtual string TableName => $"{_config.Schema}.{_config.WorkflowInstanceTable}";

        public async Task<WorkflowInstance> GetByIdAsync(string id)
        {
            using var connection = new OracleConnection(_config.ConnectionString);
            await connection.OpenAsync();

            var sql = $@"
                SELECT ID, WORKFLOW_DEFINITION_ID, CURRENT_STEP_ID, STATUS, 
                       VARIABLES_JSON, CREATED_AT, STARTED_AT, COMPLETED_AT, 
                       UPDATED_AT, CREATED_BY, CORRELATION_ID
                FROM {TableName}
                WHERE ID = :Id";

            using var command = new OracleCommand(sql, connection);
            command.Parameters.Add("Id", OracleDbType.Varchar2).Value = id;

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToInstance(reader);
            }

            return null;
        }

        public async Task<IEnumerable<WorkflowInstance>> GetByStatusAsync(WorkflowStatus status)
        {
            using var connection = new OracleConnection(_config.ConnectionString);
            await connection.OpenAsync();

            var sql = $@"
                SELECT ID, WORKFLOW_DEFINITION_ID, CURRENT_STEP_ID, STATUS, 
                       VARIABLES_JSON, CREATED_AT, STARTED_AT, COMPLETED_AT, 
                       UPDATED_AT, CREATED_BY, CORRELATION_ID
                FROM {TableName}
                WHERE STATUS = :Status
                ORDER BY CREATED_AT DESC";

            using var command = new OracleCommand(sql, connection);
            command.Parameters.Add("Status", OracleDbType.Int32).Value = (int)status;

            using var reader = await command.ExecuteReaderAsync();
            var instances = new List<WorkflowInstance>();
            while (await reader.ReadAsync())
            {
                instances.Add(MapToInstance(reader));
            }

            return instances;
        }

        public async Task<IEnumerable<WorkflowInstance>> GetByDefinitionIdAsync(string definitionId)
        {
            using var connection = new OracleConnection(_config.ConnectionString);
            await connection.OpenAsync();

            var sql = $@"
                SELECT ID, WORKFLOW_DEFINITION_ID, CURRENT_STEP_ID, STATUS, 
                       VARIABLES_JSON, CREATED_AT, STARTED_AT, COMPLETED_AT, 
                       UPDATED_AT, CREATED_BY, CORRELATION_ID
                FROM {TableName}
                WHERE WORKFLOW_DEFINITION_ID = :DefinitionId
                ORDER BY CREATED_AT DESC";

            using var command = new OracleCommand(sql, connection);
            command.Parameters.Add("DefinitionId", OracleDbType.Varchar2).Value = definitionId;

            using var reader = await command.ExecuteReaderAsync();
            var instances = new List<WorkflowInstance>();
            while (await reader.ReadAsync())
            {
                instances.Add(MapToInstance(reader));
            }

            return instances;
        }

        public async Task<WorkflowInstance> GetByCorrelationIdAsync(string correlationId)
        {
            using var connection = new OracleConnection(_config.ConnectionString);
            await connection.OpenAsync();

            var sql = $@"
                SELECT ID, WORKFLOW_DEFINITION_ID, CURRENT_STEP_ID, STATUS, 
                       VARIABLES_JSON, CREATED_AT, STARTED_AT, COMPLETED_AT, 
                       UPDATED_AT, CREATED_BY, CORRELATION_ID
                FROM {TableName}
                WHERE CORRELATION_ID = :CorrelationId";

            using var command = new OracleCommand(sql, connection);
            command.Parameters.Add("CorrelationId", OracleDbType.Varchar2).Value = correlationId;

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToInstance(reader);
            }

            return null;
        }

        public async Task<string> CreateAsync(WorkflowInstance instance)
        {
            instance.Id = instance.Id ?? Guid.NewGuid().ToString();
            instance.CreatedAt = DateTime.UtcNow;
            instance.UpdatedAt = DateTime.UtcNow;

            using var connection = new OracleConnection(_config.ConnectionString);
            await connection.OpenAsync();

            var sql = $@"
                INSERT INTO {TableName}
                (ID, WORKFLOW_DEFINITION_ID, CURRENT_STEP_ID, STATUS, 
                 VARIABLES_JSON, CREATED_AT, STARTED_AT, COMPLETED_AT, 
                 UPDATED_AT, CREATED_BY, CORRELATION_ID)
                VALUES
                (:Id, :WorkflowDefinitionId, :CurrentStepId, :Status, 
                 :VariablesJson, :CreatedAt, :StartedAt, :CompletedAt, 
                 :UpdatedAt, :CreatedBy, :CorrelationId)";

            using var command = new OracleCommand(sql, connection);
            command.Parameters.Add("Id", OracleDbType.Varchar2).Value = instance.Id;
            command.Parameters.Add("WorkflowDefinitionId", OracleDbType.Varchar2).Value = instance.WorkflowDefinitionId;
            command.Parameters.Add("CurrentStepId", OracleDbType.Varchar2).Value = instance.CurrentStepId ?? (object)DBNull.Value;
            command.Parameters.Add("Status", OracleDbType.Int32).Value = (int)instance.Status;
            command.Parameters.Add("VariablesJson", OracleDbType.Clob).Value = JsonSerializer.Serialize(instance.Variables);
            command.Parameters.Add("CreatedAt", OracleDbType.TimeStamp).Value = instance.CreatedAt;
            command.Parameters.Add("StartedAt", OracleDbType.TimeStamp).Value = instance.StartedAt ?? (object)DBNull.Value;
            command.Parameters.Add("CompletedAt", OracleDbType.TimeStamp).Value = instance.CompletedAt ?? (object)DBNull.Value;
            command.Parameters.Add("UpdatedAt", OracleDbType.TimeStamp).Value = instance.UpdatedAt;
            command.Parameters.Add("CreatedBy", OracleDbType.Varchar2).Value = instance.CreatedBy ?? (object)DBNull.Value;
            command.Parameters.Add("CorrelationId", OracleDbType.Varchar2).Value = instance.CorrelationId ?? (object)DBNull.Value;

            await command.ExecuteNonQueryAsync();
            return instance.Id;
        }

        public async Task UpdateAsync(WorkflowInstance instance)
        {
            instance.UpdatedAt = DateTime.UtcNow;

            using var connection = new OracleConnection(_config.ConnectionString);
            await connection.OpenAsync();

            var sql = $@"
                UPDATE {TableName}
                SET CURRENT_STEP_ID = :CurrentStepId,
                    STATUS = :Status,
                    VARIABLES_JSON = :VariablesJson,
                    STARTED_AT = :StartedAt,
                    COMPLETED_AT = :CompletedAt,
                    UPDATED_AT = :UpdatedAt
                WHERE ID = :Id";

            using var command = new OracleCommand(sql, connection);
            command.Parameters.Add("CurrentStepId", OracleDbType.Varchar2).Value = instance.CurrentStepId ?? (object)DBNull.Value;
            command.Parameters.Add("Status", OracleDbType.Int32).Value = (int)instance.Status;
            command.Parameters.Add("VariablesJson", OracleDbType.Clob).Value = JsonSerializer.Serialize(instance.Variables);
            command.Parameters.Add("StartedAt", OracleDbType.TimeStamp).Value = instance.StartedAt ?? (object)DBNull.Value;
            command.Parameters.Add("CompletedAt", OracleDbType.TimeStamp).Value = instance.CompletedAt ?? (object)DBNull.Value;
            command.Parameters.Add("UpdatedAt", OracleDbType.TimeStamp).Value = instance.UpdatedAt;
            command.Parameters.Add("Id", OracleDbType.Varchar2).Value = instance.Id;

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(string id)
        {
            using var connection = new OracleConnection(_config.ConnectionString);
            await connection.OpenAsync();

            var sql = $"DELETE FROM {TableName} WHERE ID = :Id";

            using var command = new OracleCommand(sql, connection);
            command.Parameters.Add("Id", OracleDbType.Varchar2).Value = id;

            await command.ExecuteNonQueryAsync();
        }

        protected virtual WorkflowInstance MapToInstance(IDataReader reader)
        {
            return new WorkflowInstance
            {
                Id = reader["ID"].ToString(),
                WorkflowDefinitionId = reader["WORKFLOW_DEFINITION_ID"].ToString(),
                CurrentStepId = reader["CURRENT_STEP_ID"] != DBNull.Value ? reader["CURRENT_STEP_ID"].ToString() : null,
                Status = (WorkflowStatus)Convert.ToInt32(reader["STATUS"]),
                Variables = JsonSerializer.Deserialize<Dictionary<string, object>>(reader["VARIABLES_JSON"].ToString()),
                CreatedAt = Convert.ToDateTime(reader["CREATED_AT"]),
                StartedAt = reader["STARTED_AT"] != DBNull.Value ? Convert.ToDateTime(reader["STARTED_AT"]) : null,
                CompletedAt = reader["COMPLETED_AT"] != DBNull.Value ? Convert.ToDateTime(reader["COMPLETED_AT"]) : null,
                UpdatedAt = reader["UPDATED_AT"] != DBNull.Value ? Convert.ToDateTime(reader["UPDATED_AT"]) : null,
                CreatedBy = reader["CREATED_BY"] as string,
                CorrelationId = reader["CORRELATION_ID"] as string
            };
        }
    }
}

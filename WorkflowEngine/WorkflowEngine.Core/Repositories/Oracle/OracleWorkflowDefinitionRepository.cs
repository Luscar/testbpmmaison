using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Repositories.Oracle
{
    /// <summary>
    /// Oracle implementation of workflow definition repository
    /// </summary>
    public class OracleWorkflowDefinitionRepository : IWorkflowDefinitionRepository
    {
        private readonly RepositoryConfiguration _config;

        public OracleWorkflowDefinitionRepository(RepositoryConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        protected virtual string TableName => $"{_config.Schema}.{_config.WorkflowDefinitionTable}";

        public async Task<WorkflowDefinition> GetByIdAsync(string id)
        {
            using var connection = new OracleConnection(_config.ConnectionString);
            await connection.OpenAsync();

            var sql = $@"
                SELECT ID, NAME, DESCRIPTION, VERSION, STEPS_JSON, INITIAL_STEP_ID, 
                       VARIABLES_JSON, CREATED_AT, UPDATED_AT
                FROM {TableName}
                WHERE ID = :Id";

            using var command = new OracleCommand(sql, connection);
            command.Parameters.Add("Id", OracleDbType.Varchar2).Value = id;

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToDefinition(reader);
            }

            return null;
        }

        public async Task<WorkflowDefinition> GetByNameAndVersionAsync(string name, string version)
        {
            using var connection = new OracleConnection(_config.ConnectionString);
            await connection.OpenAsync();

            var sql = $@"
                SELECT ID, NAME, DESCRIPTION, VERSION, STEPS_JSON, INITIAL_STEP_ID, 
                       VARIABLES_JSON, CREATED_AT, UPDATED_AT
                FROM {TableName}
                WHERE NAME = :Name AND VERSION = :Version";

            using var command = new OracleCommand(sql, connection);
            command.Parameters.Add("Name", OracleDbType.Varchar2).Value = name;
            command.Parameters.Add("Version", OracleDbType.Varchar2).Value = version;

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToDefinition(reader);
            }

            return null;
        }

        public async Task<IEnumerable<WorkflowDefinition>> GetAllAsync()
        {
            using var connection = new OracleConnection(_config.ConnectionString);
            await connection.OpenAsync();

            var sql = $@"
                SELECT ID, NAME, DESCRIPTION, VERSION, STEPS_JSON, INITIAL_STEP_ID, 
                       VARIABLES_JSON, CREATED_AT, UPDATED_AT
                FROM {TableName}
                ORDER BY CREATED_AT DESC";

            using var command = new OracleCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            var definitions = new List<WorkflowDefinition>();
            while (await reader.ReadAsync())
            {
                definitions.Add(MapToDefinition(reader));
            }

            return definitions;
        }

        public async Task<string> CreateAsync(WorkflowDefinition definition)
        {
            definition.Id = definition.Id ?? Guid.NewGuid().ToString();
            definition.CreatedAt = DateTime.UtcNow;

            using var connection = new OracleConnection(_config.ConnectionString);
            await connection.OpenAsync();

            var sql = $@"
                INSERT INTO {TableName} 
                (ID, NAME, DESCRIPTION, VERSION, STEPS_JSON, INITIAL_STEP_ID, 
                 VARIABLES_JSON, CREATED_AT, UPDATED_AT)
                VALUES 
                (:Id, :Name, :Description, :Version, :StepsJson, :InitialStepId, 
                 :VariablesJson, :CreatedAt, :UpdatedAt)";

            using var command = new OracleCommand(sql, connection);
            command.Parameters.Add("Id", OracleDbType.Varchar2).Value = definition.Id;
            command.Parameters.Add("Name", OracleDbType.Varchar2).Value = definition.Name;
            command.Parameters.Add("Description", OracleDbType.Varchar2).Value = definition.Description ?? (object)DBNull.Value;
            command.Parameters.Add("Version", OracleDbType.Varchar2).Value = definition.Version;
            command.Parameters.Add("StepsJson", OracleDbType.Clob).Value = JsonSerializer.Serialize(definition.Steps);
            command.Parameters.Add("InitialStepId", OracleDbType.Varchar2).Value = definition.InitialStepId;
            command.Parameters.Add("VariablesJson", OracleDbType.Clob).Value = JsonSerializer.Serialize(definition.Variables);
            command.Parameters.Add("CreatedAt", OracleDbType.TimeStamp).Value = definition.CreatedAt;
            command.Parameters.Add("UpdatedAt", OracleDbType.TimeStamp).Value = definition.UpdatedAt ?? (object)DBNull.Value;

            await command.ExecuteNonQueryAsync();
            return definition.Id;
        }

        public async Task UpdateAsync(WorkflowDefinition definition)
        {
            definition.UpdatedAt = DateTime.UtcNow;

            using var connection = new OracleConnection(_config.ConnectionString);
            await connection.OpenAsync();

            var sql = $@"
                UPDATE {TableName}
                SET NAME = :Name,
                    DESCRIPTION = :Description,
                    VERSION = :Version,
                    STEPS_JSON = :StepsJson,
                    INITIAL_STEP_ID = :InitialStepId,
                    VARIABLES_JSON = :VariablesJson,
                    UPDATED_AT = :UpdatedAt
                WHERE ID = :Id";

            using var command = new OracleCommand(sql, connection);
            command.Parameters.Add("Name", OracleDbType.Varchar2).Value = definition.Name;
            command.Parameters.Add("Description", OracleDbType.Varchar2).Value = definition.Description ?? (object)DBNull.Value;
            command.Parameters.Add("Version", OracleDbType.Varchar2).Value = definition.Version;
            command.Parameters.Add("StepsJson", OracleDbType.Clob).Value = JsonSerializer.Serialize(definition.Steps);
            command.Parameters.Add("InitialStepId", OracleDbType.Varchar2).Value = definition.InitialStepId;
            command.Parameters.Add("VariablesJson", OracleDbType.Clob).Value = JsonSerializer.Serialize(definition.Variables);
            command.Parameters.Add("UpdatedAt", OracleDbType.TimeStamp).Value = definition.UpdatedAt;
            command.Parameters.Add("Id", OracleDbType.Varchar2).Value = definition.Id;

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

        protected virtual WorkflowDefinition MapToDefinition(IDataReader reader)
        {
            return new WorkflowDefinition
            {
                Id = reader["ID"].ToString(),
                Name = reader["NAME"].ToString(),
                Description = reader["DESCRIPTION"] as string,
                Version = reader["VERSION"].ToString(),
                Steps = JsonSerializer.Deserialize<List<StepDefinition>>(reader["STEPS_JSON"].ToString()),
                InitialStepId = reader["INITIAL_STEP_ID"].ToString(),
                Variables = JsonSerializer.Deserialize<Dictionary<string, object>>(reader["VARIABLES_JSON"].ToString()),
                CreatedAt = Convert.ToDateTime(reader["CREATED_AT"]),
                UpdatedAt = reader["UPDATED_AT"] != DBNull.Value ? Convert.ToDateTime(reader["UPDATED_AT"]) : null
            };
        }
    }
}

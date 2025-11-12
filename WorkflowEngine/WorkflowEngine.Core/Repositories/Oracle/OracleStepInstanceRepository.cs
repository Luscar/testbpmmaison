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
    /// Oracle implementation of step instance repository
    /// </summary>
    public class OracleStepInstanceRepository : IStepInstanceRepository
    {
        private readonly RepositoryConfiguration _config;

        public OracleStepInstanceRepository(RepositoryConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        protected virtual string TableName => $"{_config.Schema}.{_config.StepInstanceTable}";

        public async Task<StepInstance> GetByIdAsync(string id)
        {
            using var connection = new OracleConnection(_config.ConnectionString);
            await connection.OpenAsync();

            var sql = $@"
                SELECT ID, WORKFLOW_INSTANCE_ID, STEP_DEFINITION_ID, STEP_NAME, 
                       STEP_TYPE, STATUS, CREATED_AT, STARTED_AT, COMPLETED_AT, 
                       ASSIGNED_TO, INPUT_DATA_JSON, OUTPUT_DATA_JSON, 
                       ERROR_MESSAGE, TRANSITION_TAKEN, RETRY_COUNT
                FROM {TableName}
                WHERE ID = :Id";

            using var command = new OracleCommand(sql, connection);
            command.Parameters.Add("Id", OracleDbType.Varchar2).Value = id;

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToStepInstance(reader);
            }

            return null;
        }

        public async Task<IEnumerable<StepInstance>> GetByWorkflowInstanceIdAsync(string workflowInstanceId)
        {
            using var connection = new OracleConnection(_config.ConnectionString);
            await connection.OpenAsync();

            var sql = $@"
                SELECT ID, WORKFLOW_INSTANCE_ID, STEP_DEFINITION_ID, STEP_NAME, 
                       STEP_TYPE, STATUS, CREATED_AT, STARTED_AT, COMPLETED_AT, 
                       ASSIGNED_TO, INPUT_DATA_JSON, OUTPUT_DATA_JSON, 
                       ERROR_MESSAGE, TRANSITION_TAKEN, RETRY_COUNT
                FROM {TableName}
                WHERE WORKFLOW_INSTANCE_ID = :WorkflowInstanceId
                ORDER BY CREATED_AT ASC";

            using var command = new OracleCommand(sql, connection);
            command.Parameters.Add("WorkflowInstanceId", OracleDbType.Varchar2).Value = workflowInstanceId;

            using var reader = await command.ExecuteReaderAsync();
            var steps = new List<StepInstance>();
            while (await reader.ReadAsync())
            {
                steps.Add(MapToStepInstance(reader));
            }

            return steps;
        }

        public async Task<IEnumerable<StepInstance>> GetPendingStepsAsync()
        {
            using var connection = new OracleConnection(_config.ConnectionString);
            await connection.OpenAsync();

            var sql = $@"
                SELECT ID, WORKFLOW_INSTANCE_ID, STEP_DEFINITION_ID, STEP_NAME, 
                       STEP_TYPE, STATUS, CREATED_AT, STARTED_AT, COMPLETED_AT, 
                       ASSIGNED_TO, INPUT_DATA_JSON, OUTPUT_DATA_JSON, 
                       ERROR_MESSAGE, TRANSITION_TAKEN, RETRY_COUNT
                FROM {TableName}
                WHERE STATUS IN (:Pending, :WaitingForInput)
                ORDER BY CREATED_AT ASC";

            using var command = new OracleCommand(sql, connection);
            command.Parameters.Add("Pending", OracleDbType.Int32).Value = (int)StepStatus.Pending;
            command.Parameters.Add("WaitingForInput", OracleDbType.Int32).Value = (int)StepStatus.WaitingForInput;

            using var reader = await command.ExecuteReaderAsync();
            var steps = new List<StepInstance>();
            while (await reader.ReadAsync())
            {
                steps.Add(MapToStepInstance(reader));
            }

            return steps;
        }

        public async Task<IEnumerable<StepInstance>> GetScheduledStepsAsync(DateTime beforeDate)
        {
            using var connection = new OracleConnection(_config.ConnectionString);
            await connection.OpenAsync();

            var sql = $@"
                SELECT ID, WORKFLOW_INSTANCE_ID, STEP_DEFINITION_ID, STEP_NAME, 
                       STEP_TYPE, STATUS, CREATED_AT, STARTED_AT, COMPLETED_AT, 
                       ASSIGNED_TO, INPUT_DATA_JSON, OUTPUT_DATA_JSON, 
                       ERROR_MESSAGE, TRANSITION_TAKEN, RETRY_COUNT
                FROM {TableName}
                WHERE STATUS = :Scheduled 
                  AND STARTED_AT <= :BeforeDate
                ORDER BY STARTED_AT ASC";

            using var command = new OracleCommand(sql, connection);
            command.Parameters.Add("Scheduled", OracleDbType.Int32).Value = (int)StepStatus.Scheduled;
            command.Parameters.Add("BeforeDate", OracleDbType.TimeStamp).Value = beforeDate;

            using var reader = await command.ExecuteReaderAsync();
            var steps = new List<StepInstance>();
            while (await reader.ReadAsync())
            {
                steps.Add(MapToStepInstance(reader));
            }

            return steps;
        }

        public async Task<IEnumerable<StepInstance>> GetByAssignedUserAsync(string userId)
        {
            using var connection = new OracleConnection(_config.ConnectionString);
            await connection.OpenAsync();

            var sql = $@"
                SELECT ID, WORKFLOW_INSTANCE_ID, STEP_DEFINITION_ID, STEP_NAME, 
                       STEP_TYPE, STATUS, CREATED_AT, STARTED_AT, COMPLETED_AT, 
                       ASSIGNED_TO, INPUT_DATA_JSON, OUTPUT_DATA_JSON, 
                       ERROR_MESSAGE, TRANSITION_TAKEN, RETRY_COUNT
                FROM {TableName}
                WHERE ASSIGNED_TO = :UserId 
                  AND STATUS = :WaitingForInput
                ORDER BY CREATED_AT ASC";

            using var command = new OracleCommand(sql, connection);
            command.Parameters.Add("UserId", OracleDbType.Varchar2).Value = userId;
            command.Parameters.Add("WaitingForInput", OracleDbType.Int32).Value = (int)StepStatus.WaitingForInput;

            using var reader = await command.ExecuteReaderAsync();
            var steps = new List<StepInstance>();
            while (await reader.ReadAsync())
            {
                steps.Add(MapToStepInstance(reader));
            }

            return steps;
        }

        public async Task<string> CreateAsync(StepInstance stepInstance)
        {
            stepInstance.Id = stepInstance.Id ?? Guid.NewGuid().ToString();
            stepInstance.CreatedAt = DateTime.UtcNow;

            using var connection = new OracleConnection(_config.ConnectionString);
            await connection.OpenAsync();

            var sql = $@"
                INSERT INTO {TableName}
                (ID, WORKFLOW_INSTANCE_ID, STEP_DEFINITION_ID, STEP_NAME, 
                 STEP_TYPE, STATUS, CREATED_AT, STARTED_AT, COMPLETED_AT, 
                 ASSIGNED_TO, INPUT_DATA_JSON, OUTPUT_DATA_JSON, 
                 ERROR_MESSAGE, TRANSITION_TAKEN, RETRY_COUNT)
                VALUES
                (:Id, :WorkflowInstanceId, :StepDefinitionId, :StepName, 
                 :StepType, :Status, :CreatedAt, :StartedAt, :CompletedAt, 
                 :AssignedTo, :InputDataJson, :OutputDataJson, 
                 :ErrorMessage, :TransitionTaken, :RetryCount)";

            using var command = new OracleCommand(sql, connection);
            command.Parameters.Add("Id", OracleDbType.Varchar2).Value = stepInstance.Id;
            command.Parameters.Add("WorkflowInstanceId", OracleDbType.Varchar2).Value = stepInstance.WorkflowInstanceId;
            command.Parameters.Add("StepDefinitionId", OracleDbType.Varchar2).Value = stepInstance.StepDefinitionId;
            command.Parameters.Add("StepName", OracleDbType.Varchar2).Value = stepInstance.StepName;
            command.Parameters.Add("StepType", OracleDbType.Varchar2).Value = stepInstance.StepType;
            command.Parameters.Add("Status", OracleDbType.Int32).Value = (int)stepInstance.Status;
            command.Parameters.Add("CreatedAt", OracleDbType.TimeStamp).Value = stepInstance.CreatedAt;
            command.Parameters.Add("StartedAt", OracleDbType.TimeStamp).Value = stepInstance.StartedAt ?? (object)DBNull.Value;
            command.Parameters.Add("CompletedAt", OracleDbType.TimeStamp).Value = stepInstance.CompletedAt ?? (object)DBNull.Value;
            command.Parameters.Add("AssignedTo", OracleDbType.Varchar2).Value = stepInstance.AssignedTo ?? (object)DBNull.Value;
            command.Parameters.Add("InputDataJson", OracleDbType.Clob).Value = JsonSerializer.Serialize(stepInstance.InputData);
            command.Parameters.Add("OutputDataJson", OracleDbType.Clob).Value = JsonSerializer.Serialize(stepInstance.OutputData);
            command.Parameters.Add("ErrorMessage", OracleDbType.Clob).Value = stepInstance.ErrorMessage ?? (object)DBNull.Value;
            command.Parameters.Add("TransitionTaken", OracleDbType.Varchar2).Value = stepInstance.TransitionTaken ?? (object)DBNull.Value;
            command.Parameters.Add("RetryCount", OracleDbType.Int32).Value = stepInstance.RetryCount;

            await command.ExecuteNonQueryAsync();
            return stepInstance.Id;
        }

        public async Task UpdateAsync(StepInstance stepInstance)
        {
            using var connection = new OracleConnection(_config.ConnectionString);
            await connection.OpenAsync();

            var sql = $@"
                UPDATE {TableName}
                SET STATUS = :Status,
                    STARTED_AT = :StartedAt,
                    COMPLETED_AT = :CompletedAt,
                    ASSIGNED_TO = :AssignedTo,
                    OUTPUT_DATA_JSON = :OutputDataJson,
                    ERROR_MESSAGE = :ErrorMessage,
                    TRANSITION_TAKEN = :TransitionTaken,
                    RETRY_COUNT = :RetryCount
                WHERE ID = :Id";

            using var command = new OracleCommand(sql, connection);
            command.Parameters.Add("Status", OracleDbType.Int32).Value = (int)stepInstance.Status;
            command.Parameters.Add("StartedAt", OracleDbType.TimeStamp).Value = stepInstance.StartedAt ?? (object)DBNull.Value;
            command.Parameters.Add("CompletedAt", OracleDbType.TimeStamp).Value = stepInstance.CompletedAt ?? (object)DBNull.Value;
            command.Parameters.Add("AssignedTo", OracleDbType.Varchar2).Value = stepInstance.AssignedTo ?? (object)DBNull.Value;
            command.Parameters.Add("OutputDataJson", OracleDbType.Clob).Value = JsonSerializer.Serialize(stepInstance.OutputData);
            command.Parameters.Add("ErrorMessage", OracleDbType.Clob).Value = stepInstance.ErrorMessage ?? (object)DBNull.Value;
            command.Parameters.Add("TransitionTaken", OracleDbType.Varchar2).Value = stepInstance.TransitionTaken ?? (object)DBNull.Value;
            command.Parameters.Add("RetryCount", OracleDbType.Int32).Value = stepInstance.RetryCount;
            command.Parameters.Add("Id", OracleDbType.Varchar2).Value = stepInstance.Id;

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

        protected virtual StepInstance MapToStepInstance(IDataReader reader)
        {
            return new StepInstance
            {
                Id = reader["ID"].ToString(),
                WorkflowInstanceId = reader["WORKFLOW_INSTANCE_ID"].ToString(),
                StepDefinitionId = reader["STEP_DEFINITION_ID"].ToString(),
                StepName = reader["STEP_NAME"].ToString(),
                StepType = reader["STEP_TYPE"].ToString(),
                Status = (StepStatus)Convert.ToInt32(reader["STATUS"]),
                CreatedAt = Convert.ToDateTime(reader["CREATED_AT"]),
                StartedAt = reader["STARTED_AT"] != DBNull.Value ? Convert.ToDateTime(reader["STARTED_AT"]) : null,
                CompletedAt = reader["COMPLETED_AT"] != DBNull.Value ? Convert.ToDateTime(reader["COMPLETED_AT"]) : null,
                AssignedTo = reader["ASSIGNED_TO"] as string,
                InputData = JsonSerializer.Deserialize<Dictionary<string, object>>(reader["INPUT_DATA_JSON"].ToString()),
                OutputData = JsonSerializer.Deserialize<Dictionary<string, object>>(reader["OUTPUT_DATA_JSON"].ToString()),
                ErrorMessage = reader["ERROR_MESSAGE"] as string,
                TransitionTaken = reader["TRANSITION_TAKEN"] as string,
                RetryCount = Convert.ToInt32(reader["RETRY_COUNT"])
            };
        }
    }
}

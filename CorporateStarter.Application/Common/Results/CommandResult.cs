using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace CorporateStarter.Application.Common.Results
{
    public sealed class CommandResult<T>
    {
        private CommandResult(
            bool succeeded,
            T? value,
            string? errorCode,
            string? errorMessage)
        {
            Succeeded = succeeded;
            Value = value;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        public bool Succeeded { get; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public T? Value { get; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ErrorCode { get; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ErrorMessage { get; }

        public static CommandResult<T> Success(T value)
        {
            return new CommandResult<T>(true, value, null, null);
        }

        public static CommandResult<T> Failure(
            string errorCode,
            string errorMessage)
        {
            return new CommandResult<T>(false, default, errorCode, errorMessage);
        }
    }
}

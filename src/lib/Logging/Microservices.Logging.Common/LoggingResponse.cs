using Microservices.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microservices.Logging.Common
{
    public class LoggingResponse
    {
        public LoggingResponse()
        {
            Success = true;
            LogMessages = new List<string>();
            WarningMessages = new List<string>();
            ErrorMessages = new List<string>();
            ExceptionMessages = new List<string>();
        }

        public bool Success { get; set; }
        public bool HasMessages => LogMessages.Any() || WarningMessages.Any() || ErrorMessages.Any() || ExceptionMessages.Any();
        public bool HasLogMessages => LogMessages.Any();
        public bool HasWarningMessages => WarningMessages.Any();
        public bool HasErrorMessages => ErrorMessages.Any();
        public bool HasExceptionMessages => ExceptionMessages.Any();
        public List<string> LogMessages { get; set; }
        public List<string> WarningMessages { get; set; }
        public List<string> ErrorMessages { get; set; }
        public List<string> ExceptionMessages { get; set; }

        private const string DefaultSeparator = "\r\n";

        public void AddLogMessage(string message)
        {
            LogMessages.Add(message);
        }

        public string GetAllLogMessages(string separator = DefaultSeparator)
        {
            return HasLogMessages ? string.Join(separator, LogMessages) : string.Empty;
        }

        public void AddWarningMessage(string message)
        {
            WarningMessages.Add(message);
        }

        public string GetAllWarningMessages(string separator = DefaultSeparator)
        {
            return HasWarningMessages ? string.Join(separator, WarningMessages) : string.Empty;
        }

        public void AddErrorMessage(string message)
        {
            Success = false;
            ErrorMessages.Add(message);
        }

        public string GetAllErrorMessages(string separator = DefaultSeparator)
        {
            return HasErrorMessages ? string.Join(separator, ErrorMessages) : string.Empty;
        }

        public void AddExceptionMessage(string message)
        {
            Success = false;
            ExceptionMessages.Add(message);
        }

        public void AddExceptionMessage(Exception e)
        {
            AddExceptionMessage(e.ToDetailedString());
        }

        public string GetAllExceptionMessages(string separator = DefaultSeparator)
        {
            return HasExceptionMessages ? string.Join(separator, ExceptionMessages) : string.Empty;
        }

        public string GetAllMessages(string serparator = DefaultSeparator)
        {
            var sb = new StringBuilder();
            if (HasExceptionMessages)
            {
                sb.AppendLine("Exception Messages:");
                sb.AppendLine(string.Join(serparator, ExceptionMessages));
                sb.AppendLine();
            }
            if (HasErrorMessages)
            {
                sb.AppendLine("Error Messages:");
                sb.AppendLine(string.Join(serparator, ErrorMessages));
                sb.AppendLine();
            }
            if (HasWarningMessages)
            {
                sb.AppendLine("Warning Messages:");
                sb.AppendLine(string.Join(serparator, WarningMessages));
                sb.AppendLine();
            }
            if (HasLogMessages)
            {
                sb.AppendLine("Log Messages:");
                sb.AppendLine(string.Join(serparator, LogMessages));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public void MergeResponse(LoggingResponse response)
        {
            if (response.HasLogMessages)
                LogMessages.AddRange(response.LogMessages);
            if (response.HasWarningMessages)
                WarningMessages.AddRange(response.WarningMessages);
            if (response.HasErrorMessages)
                ErrorMessages.AddRange(response.ErrorMessages);
            if (response.HasExceptionMessages)
                ExceptionMessages.AddRange(response.ExceptionMessages);
        }
    }
}

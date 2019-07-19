using System;
using System.Collections.Generic;
using System.Text;

namespace Microservices.Common.Extensions
{
   public static class ExceptionExtensions
    {
        public static string ToDetailedString(this Exception exception)
        {
            var sb = new StringBuilder();
            sb.AppendLine($@"Message: {exception.Message}\r\n");
            if (exception.InnerException!=null) sb.AppendLine($@"Inner Exception: {exception.Message}\r\n");
            if (exception.StackTrace != null) sb.AppendLine($@"Stack Trace: {exception.StackTrace}\r\n");
            return sb.ToString();
        }
    }
}

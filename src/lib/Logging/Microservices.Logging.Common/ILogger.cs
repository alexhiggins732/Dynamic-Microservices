using System;
using System.Threading.Tasks;
namespace Microservices.Logging.Common
{
    public interface ILogger
    {
        Guid Log(string message);
        Task<Guid> LogAsync(string message);

        Guid LogWarning(string message);
        Task<Guid> LogWarningAsync(string message);

        Guid LogError(string message);
        Task<Guid> LogErrorAsync(string message);

        Guid LogException(string message);
        Task<Guid> LogExceptionAsync(string message);

        Guid LogException(Exception e);
        Task<Guid> LogExceptionAsync(Exception e);

        Guid LogException(Exception e, string message);
        Task<Guid> LogExceptionAsync(Exception e, string message);

        void LogResponse(LoggingResponse response);
        Task LogResponseAsync(LoggingResponse response);
    }

}

using Microservices.Logging.Common;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Web;

namespace Microservices.Logging.ServerSample.FxWebApi
{
    public class LoggingApiModule : IHttpModule
    {
        /// <summary>
        /// You will need to configure this module in the Web.config file of your
        /// web and register it with IIS before being able to use it. For more information
        /// see the following link: https://go.microsoft.com/?linkid=8101007
        /// </summary>
        #region IHttpModule Members

        public void Dispose()
        {
            //clean-up code here.
        }

        public void Init(HttpApplication context)
        {
            // Below is an example of how you can handle LogRequest event and provide 
            // custom logging implementation for it
            context.LogRequest += new EventHandler(OnLogRequest);
            context.BeginRequest += Context_BeginRequest;
        }

        private void Context_BeginRequest(object sender, EventArgs e)
        {
            var context = HttpContext.Current;
            var request = context.Request;
            var path = request.Path.ToLower();
            if (path.StartsWith($"/api/{nameof(ILogger).ToLower()}"))
            {
                handleLoggingApiRequest(context);
                context.Response.End();
                return;
            }
            if (path.StartsWith($"/api/servicelocator/"))
            {
                handleServiceLocatorRequest(context);
                context.Response.End();
                return;
            }
        }

        private void handleServiceLocatorRequest(HttpContext context)
        {
            var request = context.Request;
            var path = request.Path;
            var apiRoot = "/api/ServiceLocator/";
            var appUri = new Uri(request.Url.AbsoluteUri, UriKind.Absolute);
            var method = path.Substring(apiRoot.Length);
            var postData = new StreamReader(context.Request.GetBufferedInputStream()).ReadToEnd();
            string result = "";
            switch (method)
            {
                case "GetServiceLocation":
                    var serviceName = JsonConvert.DeserializeAnonymousType(postData, new { serviceName = "" }).serviceName;
                    switch(serviceName.ToLower())
                    {
                        case "logger":
                        case "ilogger":
                            var loggerUri = new Uri(appUri, $"/api/{nameof(ILogger).ToLower()}/");
                            result = JsonResult(() => loggerUri);
                            break;
                        default:
                            result = JsonErrorResult(() => (object)null, () => new EntryPointNotFoundException());
                            break;
                    }
                    break;
                default:
                    result = JsonErrorResult(() => (object)null, () => new NotImplementedException());
                    break;
            }
            context.Response.Write(result);
            context.Response.End();
        }

        private void handleLoggingApiRequest(HttpContext context)
        {
            var request = context.Request;
            var path = request.Path;
            var apiRoot = "/api/logging/";

            var method = path.Substring(apiRoot.Length);
            var postData = new StreamReader(context.Request.GetBufferedInputStream()).ReadToEnd();
            string result = "";
            switch (method)
            {
                case nameof(ILogger.Log):
                case "log":
                case nameof(ILogger.LogAsync):
                case "logasync":
                    result = JsonResult(() => Guid.NewGuid());
                    break;
                case nameof(ILogger.LogError):
                case "logerror":
                case nameof(ILogger.LogErrorAsync):
                case "logerrorasync":
                    result = JsonResult(() => Guid.NewGuid());
                    break;
                case nameof(ILogger.LogException):
                case "logexception":
                case nameof(ILogger.LogExceptionAsync):
                case "logexceptionasync":
                    result = JsonResult(() => Guid.NewGuid());
                    break;
                case nameof(ILogger.LogWarning):
                case "logwarning":
                case nameof(ILogger.LogWarningAsync):
                case "logwarningasync":
                    result = JsonResult(() => Guid.NewGuid());
                    break;
                default:
                    result = JsonErrorResult(() => Guid.Empty, () => new NotImplementedException($"Method {method} is not implemented"));
                    break;

            }
            context.Response.Write(result);
            context.Response.End();
        }

        #endregion

        public string JsonResult<T>(Func<T> resolver) => JsonConvert.SerializeObject(new { status = "ok", result = resolver() });
        public string JsonErrorResult<T, TException>(Func<T> resolver, Func<TException> errorFunc) => JsonConvert.SerializeObject(new { status = "error", result = resolver(), error = errorFunc() });


        public void OnLogRequest(Object source, EventArgs e)
        {
            //custom logging logic can go here
        }
    }
}

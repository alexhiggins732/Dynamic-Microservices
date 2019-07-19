using Microservices.Logging.Common;
//using Microservices.Logging.Common;
//using Microservices.Logging.Common;
using Nancy;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Microservices.Logging.NancyFx.LoggingService
{
    public interface IGenericLogger
    {
         Guid NewGuid();
    }
    public class GenericService : IGenericLogger
    {
        public Guid NewGuid() => Guid.NewGuid();

    }

    public abstract class NancyApiModule<TService> : NancyModule//, IInterceptor<TService>
           where TService : class
    {
        private TService implementation;
        //private ConcurrentDictionary<Type, Action<Castle.DynamicProxy.IInvocation>> executeMethods;


        public NancyApiModule(TService service)
        {
            this.implementation = service;

            var serviceType = typeof(TService);
            if (!serviceType.IsInterface)
                throw new NotImplementedException();
            var serviceRoot = serviceType.Name;
            var methods = serviceType.GetMethods();
            foreach (var method in methods)
            {
                bool returnsTask = method.ReturnType.BaseType == typeof(Task); ;
                var endpoint = $"{serviceRoot}/{method.Name}";
                Post(endpoint, async (ctx, token) =>
                {
                    try
                    {
                        using (var reader = new StreamReader(Request.Body))
                        {
                            var incomingJson = await reader.ReadToEndAsync();

                            var inputParameters = JObject.Parse(incomingJson);
                            var paramDict = inputParameters.ToObject<Dictionary<string, string>>();
                            var keys = paramDict.Keys.ToList();





                            //var jsonParameters = inputParameters["Value"] as JArray;
                            var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();

                            object[] parameters = new object[parameterTypes.Length];

                            for (var i = 0; i < parameters.Length; i++)
                            {
                                var type = parameterTypes[i];
                                // deserialize each parameter to it's respective type
                                var json = keys[i].ToString();
                                parameters[i] = JsonConvert.DeserializeObject(json, type);
                            }

                            object result = null;
                            if (returnsTask)
                            {
                                dynamic task = method.Invoke(implementation, parameters);
                                result = await task;
                            }
                            else
                            {
                                result = method.Invoke(implementation, parameters);
                            }


                            return JsonConvert.SerializeObject(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorData = new Dictionary<string, object>();
                        errorData["$exception"] = true;
                        errorData["$exceptionMessage"] = ex.InnerException.Message;
                        return JsonConvert.SerializeObject(errorData);
                    }
                });
            }

        }
    }

    public class NancyGeneric : NancyApiModule<IGenericLogger>
    {
        public NancyGeneric(IGenericLogger service) : base(service)
        {
        }
    }



    public class NancyLoggingModule : NancyModule
    {
        static ConcurrentDictionary<string, Func<ILogger, object[], object>> handlers;

        static NancyLoggingModule()
        {
            handlers = new ConcurrentDictionary<string, Func<ILogger, object[], object>>();
            var methods = typeof(ILogger).GetMethods();
            foreach (var method in methods)
            {
                handlers[method.Name] = CompileHandler(method);
            }
        }

        private static Func<ILogger, object[], object> CompileHandler(MethodInfo method)
        {

            var iLogger = Expression.Parameter(typeof(ILogger), "ilogger");
            var parameters = method.GetParameters();
            var arrayExpr = Expression.Parameter(typeof(object[]), "input");
            var accessExpressions = new List<Expression>();
            for (var i = 0; i < parameters.Length; i++)
            {
                accessExpressions.Add(Expression.Convert(Expression.ArrayAccess(
                  arrayExpr,
                  Expression.Constant(i)
              ), parameters[i].ParameterType));
            }

            var serviceCall = Expression.Call(iLogger, method, accessExpressions.ToArray());
            var box = Expression.Convert(Expression.Constant(null), typeof(object));
            if (method.ReturnType != typeof(void))
            {
                box = Expression.Convert(serviceCall, typeof(object));
            }
            var compiled = Expression.Lambda<Func<ILogger, object[], object>>(box, iLogger, arrayExpr).Compile();
            return compiled;

        }
        ILogger loggerService;

        private  object[] getRequestArgsForMethod(MethodInfo method)
        {
            var postData = "";
            using (var reader = new StreamReader(Request.Body))
            {
                postData = reader.ReadToEnd();
            }

            var inputParameters = JObject.Parse(postData);
            var inputDict = inputParameters.ToObject<Dictionary<string, object>>();
            var methodParameters = method.GetParameters();
            for (var i = 0; i < methodParameters.Length; i++)
            {
                var paramName = methodParameters[i].Name;
                var value = inputDict[paramName];
                if (value is JObject jobj)
                {
                    inputDict[paramName] = jobj.ToObject(methodParameters[i].ParameterType);
                }

                if (value is JArray)
                {
                    var json = value.ToString();
                    inputDict[paramName] = JsonConvert.DeserializeObject(json, methodParameters[i].ParameterType);
                }

            }
            var args = inputDict.Values.ToArray();
            return args;
        }

        public NancyLoggingModule(ILogger loggerService) : base("/ILogger/")
        {
            this.loggerService = loggerService;

            var methods = typeof(ILogger).GetMethods();
            foreach (var method in methods)
            {

                Post($"{method.Name}", (ctx) =>
                {
                    var args = getRequestArgsForMethod(method);
                    var result = handlers[method.Name](loggerService, args);

                    if (result.GetType().BaseType == typeof(Task))
                        result = ((dynamic)result).Result;

                    return JsonConvert.SerializeObject(new { status = "ok", result });


                });
            }
        }


    }


    public class LoggerService : ILogger
    {
        public Guid Log(string message) => Guid.NewGuid();

        public Task<Guid> LogAsync(string message) => Task.FromResult(Guid.NewGuid());

        public Guid LogError(string message) => Guid.NewGuid();

        public Task<Guid> LogErrorAsync(string message) => Task.FromResult(Guid.NewGuid());

        public Guid LogException(string message) => Guid.NewGuid();

        public Guid LogException(Exception e) => Guid.NewGuid();

        public Guid LogException(Exception e, string message) => Guid.NewGuid();

        public Task<Guid> LogExceptionAsync(string message) => Task.FromResult(Guid.NewGuid());

        public Task<Guid> LogExceptionAsync(Exception e) => Task.FromResult(Guid.NewGuid());

        public Task<Guid> LogExceptionAsync(Exception e, string message) => Task.FromResult(Guid.NewGuid());

        public void LogResponse(LoggingResponse response) => Guid.NewGuid();

        public Task LogResponseAsync(LoggingResponse response) => Task.FromResult(Guid.NewGuid());

        public Guid LogWarning(string message) => Guid.NewGuid();

        public Task<Guid> LogWarningAsync(string message) => Task.FromResult(Guid.NewGuid());
    }



}

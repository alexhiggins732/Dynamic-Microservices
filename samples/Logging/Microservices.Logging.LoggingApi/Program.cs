using Microservices.Logging.Common;


using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Castle.DynamicProxy;
using System.Collections.Generic;

namespace Microservices.Logging.LoggingApi
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");


            //An example of monolithic architecture.
            MonolithicTest();

            //Bootstrap using ApiServiceHost and WebApiServiceClient.
            ServiceTest();


        }

        public static void MonolithicTest()
        {
            var logger = GetMonolithicLogger();
            logger.Log("Hello World");
        }

        static Monolithic.GuidIdLogger GetMonolithicLogger()
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "test-log.txt");
            var idProvider = new Monolithic.GuidIdProvider();
            var store = new Monolithic.FileSystemLogStore(logPath, idProvider);
            var provider = new Monolithic.GuidIdLogger(store);
            return provider;
        }

        public static void ServiceTest()
        {
            ApiService.BootstrapTest.Run();

        }



    }

    namespace ApiService
    {
        public class BootstrapTest
        {
            public static void Run()
            {
                var services = BuildServices();
                services.AddScoped<IApiServiceClient<ILogger>, ApiServiceClient<ILogger>>();
                using (var provider = services.BuildServiceProvider())
                {
                    //Start the WebApi Host
                    var serviceHost = provider.GetRequiredService<IApiServiceHost<ILogger>>();
                    serviceHost.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

                    var serviceClient = provider.GetRequiredService<IApiServiceClient<ILogger>>();
                    var logger = serviceClient.client;
                    logger.Log("Hello World");
                    serviceHost.StopAsync(CancellationToken.None).GetAwaiter().GetResult();

                }




            }
            static IServiceCollection BuildServices()
            {
                var options = new WebApiServiceOptions
                {
                    Port = 62312,
                    ApiBaseUrl = "/log/"
                };
                return CreateServices(options);
            }


            private static IServiceCollection CreateServices(IWebApiServiceOptions options)
            {
                var services = new ServiceCollection();
                services.AddSingleton<IWebApiServiceOptions>(options);
                return services;
            }

            public interface IApiServiceOptions
            {

            }
            public interface IWebApiServiceOptions : IApiServiceOptions
            {
                string ApiBaseUrl { get; set; }
                string HostName { get; set; }
                ushort Port { get; set; }
            }
            public class WebApiServiceOptions : IWebApiServiceOptions
            {
                public string HostName { get; set; }
                public ushort Port { get; set; }
                public string ApiBaseUrl { get; set; }
            }

        }
        public interface IApiServiceHost
        {
            void HandleRequest(HttpContext context);
        }
        public interface IApiServiceHost<T> : IApiServiceHost
        {
            Task StartAsync(CancellationToken cancellationToken);

            Task StopAsync(CancellationToken cancellationToken);
        }
        public interface IApiServiceClient<T> where T : class
        {
            T client { get; }
        }
        public class ApiClientInterceptor : IInterceptor
        {
            public void Intercept(IInvocation invocation)
            {
                var method = invocation.Method;
                var methodName = method.Name;
                var parameters = method.GetParameters();
                var payload = new Dictionary<string, object>();
                var args = invocation.Arguments;
                for (var i=0; i < parameters.Length; i++)
                {
                    payload[parameters[i].Name] = args[i];
                }


           
                var returnType = method.ReturnType;

            }
        }

        public class ApiServiceClient<T> : IApiServiceClient<T> where T: class
        {
            IServiceProvider serviceProvider;
            public ApiServiceClient(IServiceProvider serviceProvider)
            {
                this.serviceProvider = serviceProvider;
                var serviceType = typeof(T);
                var p = new ProxyGenerator().CreateInterfaceProxyWithoutTarget<T>(new ApiClientInterceptor());


            }
            public T client { get; protected set; }
        }

        public class ApiServiceHostBase<T> : IApiServiceHost<T>
        {
            public void HandleRequest(HttpContext context)
            {
                throw new NotImplementedException();
            }

            public virtual Task StartAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(0);
            }

            public virtual Task StopAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(0);
            }
        }

        public class ApiServiceHost<T> : ApiServiceHostBase<T>
        {
            public override Task StartAsync(CancellationToken cancellationToken)
            {
                string[] args = new string[] { };
                var isService = !(Debugger.IsAttached || args.Contains("--console"));

                if (isService)
                {
                    var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
                    var pathToContentRoot = Path.GetDirectoryName(pathToExe);
                    Directory.SetCurrentDirectory(pathToContentRoot);
                }

                var builder = CreateWebHostBuilder(
                    args.Where(arg => arg != "--console").ToArray(), this);
                builder.ConfigureServices(x => x.AddSingleton<IApiServiceHost>(this));
                var host = builder.Build();
                return Task.Run(() => host.Run());
            }

            public override Task StopAsync(CancellationToken cancellationToken)
            {
                //TODO: Provide Implementation
                return Task.FromResult(0);
            }
            public static IWebHostBuilder CreateWebHostBuilder(string[] args,
                ApiServiceHost<T> apiServiceHost) =>
                    WebHost.CreateDefaultBuilder(args)
                                 .ConfigureLogging((hostingContext, logging) =>
                                 {
                                     //logging.AddEventLog();
                                 })
                                 .ConfigureAppConfiguration((context, config) =>
                                 {
                                     // Configure the app here.
                                 })
                 .UseStartup<Startup>();
        }


        public class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            }

            public void Configure(IApplicationBuilder app, IHostingEnvironment env)
            {
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }
                else
                {
                    app.UseExceptionHandler("/Error");
                }
                app.Use((context, next) =>
                {
                    var serviceProvider = context.RequestServices;
                    var scope = serviceProvider.CreateScope();
                    var apiService = scope.ServiceProvider.GetRequiredService<IApiServiceHost>();
                    apiService.HandleRequest(context);
                    //var cultureQuery = context.Request.Query["culture"];
                    //if (!string.IsNullOrWhiteSpace(cultureQuery))
                    //{
                    //    var culture = new CultureInfo(cultureQuery);

                    //    CultureInfo.CurrentCulture = culture;
                    //    CultureInfo.CurrentUICulture = culture;
                    //}

                    // Call the next delegate/middleware in the pipeline
                    return next();
                });
                app.UseStaticFiles();
                app.UseMvc();
            }
        }



    }


    namespace Monolithic
    {



        public interface ILogStore : ILogger
        {

        }
        public interface IdProvider<T>
        {
            T NextId();
        }

        public class GuidIdProvider : IdProvider<Guid>
        {
            public Guid NextId() => Guid.NewGuid();
        }
        /// <summary>
        /// Defines logging severity levels.
        /// </summary>
        public enum LogLevel
        {
            /// <summary>
            /// Logs that contain the most detailed messages. These messages may contain sensitive application data.
            /// These messages are disabled by default and should never be enabled in a production environment.
            /// </summary>
            Trace = 0,

            /// <summary>
            /// Logs that are used for interactive investigation during development.  These logs should primarily contain
            /// information useful for debugging and have no long-term value.
            /// </summary>
            Debug = 1,

            /// <summary>
            /// Logs that track the general flow of the application. These logs should have long-term value.
            /// </summary>
            Information = 2,

            /// <summary>
            /// Logs that highlight an abnormal or unexpected event in the application flow, but do not otherwise cause the
            /// application execution to stop.
            /// </summary>
            Warning = 3,

            /// <summary>
            /// Logs that highlight when the current flow of execution is stopped due to a failure. These should indicate a
            /// failure in the current activity, not an application-wide failure.
            /// </summary>
            Error = 4,

            /// <summary>
            /// Logs that describe an unrecoverable application or system crash, or a catastrophic failure that requires
            /// immediate attention.
            /// </summary>
            Critical = 5,

            /// <summary>
            /// Not used for writing log messages. Specifies that a logging category should not write any messages.
            /// </summary>
            None = 6,
        }

        public class FileSystemLogStore : ILogStore
        {
            private string logFilePath;
            private IdProvider<Guid> idProvider;
            public FileSystemLogStore(string logFilePath, IdProvider<Guid> idProvider)
            {
                this.logFilePath = logFilePath;
                this.idProvider = idProvider;
            }

            public Guid Log(string message) => Log(message, LogLevel.Trace);

            public Guid Log(string message, LogLevel logLevel = LogLevel.Trace)
            {
                DateTimeOffset LogDate = DateTimeOffset.UtcNow;
                File.WriteAllText(logFilePath, $"{LogDate}|{logLevel}|{message}");
                return idProvider.NextId();
            }

            public Task<Guid> LogAsync(string message) => new Task<Guid>(() => Log(message));


            public Guid LogError(string message) => Log(message, LogLevel.Error);


            public Task<Guid> LogErrorAsync(string message) => new Task<Guid>(() => Log(message, LogLevel.Error));


            public Guid LogException(string message) => Log(message, LogLevel.Error);

            public Guid LogException(Exception e) => Log("Exception: {e}", LogLevel.Error);


            public Guid LogException(Exception e, string message) => Log("Message{message}: Exception: {e}", LogLevel.Error);


            public Task<Guid> LogExceptionAsync(string message) => new Task<Guid>(() => LogException(message));


            public Task<Guid> LogExceptionAsync(Exception e) => new Task<Guid>(() => LogException(e));


            public Task<Guid> LogExceptionAsync(Exception e, string message) => new Task<Guid>(() => LogException(e, message));

            public void LogResponse(LoggingResponse response) => Log($"LoggingResponse: response.GetAllMessages()");


            public Task LogResponseAsync(LoggingResponse response) => new Task(() => LogResponse(response));


            public Guid LogWarning(string message) => Log(message, LogLevel.Warning);


            public Task<Guid> LogWarningAsync(string message) => new Task<Guid>(() => LogWarning(message));

        }

        //TODO: Need to reduce overhead.
        //  1) Developer defines a contract as an interface, EG ILogger
        //  2) Developer provides a concrete implementation, EG: FileSystemLogStore: ILogger
        //  3) Developer simply configures host impementation via DI => services.AddMicroServiceHost<ILogger, FileSystemProvider>();
        //      implement interface for settings, {hostname, cert, port, baseUrl, serviceType[WebApi,WebSocket,Wcf,Wsdl,SignalR,MvcApi]}
        //          TODO: Support allowing the client to specify arbitrary serviceType and have host 
        //              dynamically provide the service using the specified serviceType
        //        
        //  4) Client side developer configures consumer implementation via DI => services.AddMicroServiceClient<ILogger>(opts=>);
        //          developer specifies connection settings, hostname, port, apiBaseUrl, serviceType

        public class GuidIdLogger : ILogger
        {
            ILogStore logStore;

            //Dependencies:
            //  Server implementation might be looking for UserIdentity and AppIdentity.
            //      1) define common subsystem or
            //      2) require developer to configure DI on both ends.
            public GuidIdLogger(ILogStore logStore) => this.logStore = logStore;

            public Guid Log(string message)
            {
                return logStore.Log(message);
            }

            public Task<Guid> LogAsync(string message)
            {
                return logStore.LogAsync(message);
            }

            public Guid LogError(string message)
            {
                return logStore.LogError(message);
            }

            public Task<Guid> LogErrorAsync(string message)
            {
                return logStore.LogErrorAsync(message);
            }

            public Guid LogException(string message)
            {
                return logStore.LogException(message);
            }

            public Guid LogException(Exception e)
            {
                return logStore.LogException(e);
            }

            public Guid LogException(Exception e, string message)
            {
                return logStore.LogException(e, message);
            }

            public Task<Guid> LogExceptionAsync(string message)
            {
                return logStore.LogExceptionAsync(message);
            }

            public Task<Guid> LogExceptionAsync(Exception e)
            {
                return logStore.LogExceptionAsync(e);
            }

            public Task<Guid> LogExceptionAsync(Exception e, string message)
            {
                return logStore.LogExceptionAsync(e, message);
            }

            public void LogResponse(LoggingResponse response)
            {
                logStore.LogResponse(response);
            }

            public Task LogResponseAsync(LoggingResponse response)
            {
                return logStore.LogResponseAsync(response);
            }

            public Guid LogWarning(string message)
            {
                return logStore.LogWarning(message);
            }

            public Task<Guid> LogWarningAsync(string message)
            {
                return logStore.LogWarningAsync(message);
            }
        }

     

   


      

    }

}

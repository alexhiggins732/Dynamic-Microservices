using Moq;
using System;
using Xunit;
using Castle.Components.DictionaryAdapter;
using System.Collections;
using Castle.DynamicProxy;
using Microservices.Logging.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using System.Collections.Concurrent;
using System.Reflection;
using System.Linq.Expressions;
using System.Diagnostics;
using System.Linq;

namespace Microservices.Logging.Tests
{

    public interface IWebApiClient
    {
        string BaseUrl { get; set; }
        T Execute<T>(string endpoint);
        Task<T> ExecuteAsync<T>(string endpoint);
        T Execute<T>(string endpoint, Dictionary<string, object> payload);
        Task<T> ExecuteAsync<T>(string endpoint, Dictionary<string, object> payload);
    }



    public class WebApiClient : IWebApiClient
    {
        public string BaseUrl { get; set; }


        public T Execute<T>(string endpoint) => Execute<T>(endpoint, null);
        public Task<T> ExecuteAsync<T>(string endpoint) => ExecuteAsync<T>(endpoint, null);
        public T Execute<T>(string endpoint, Dictionary<string, object> param)
        {
            var json = JsonConvert.SerializeObject(param);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            using (var client = new WebClient())
            {
                var resultData = client.UploadData(BaseUrl + endpoint, bytes);
                var resultJson = System.Text.Encoding.UTF8.GetString(resultData);
                //TODO: Handler errors
                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<T>>(resultJson);
                return apiResponse.result;
            }
        }

        public Task<T> ExecuteAsync<T>(string endpoint, Dictionary<string, object> param)
            => Task.Run<T>(() => Execute<T>(endpoint, param));

    }

    public static class IInterceptorExtensions
    {
        public static Dictionary<string, object> ArgumentDictionary(this Castle.DynamicProxy.IInvocation invocation)
        {
            var payload = new Dictionary<string, object>();
            var arguments = invocation.Arguments;
            var parameters = invocation.Method.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                payload[parameters[i].Name] = arguments[i];
            }
            return payload;

        }
    }

    public interface IInterceptor<TService> : IInterceptor
    {

    }


    public class ApiFactory<TService> : IInterceptor<TService>
        where TService : class

    {
        private IWebApiClient client;

        private ConcurrentDictionary<Type, Action<Castle.DynamicProxy.IInvocation>> executeMethods;

        public ApiFactory(IWebApiClient client)
        {
            this.client = client;
            this.executeMethods = new ConcurrentDictionary<Type, Action<Castle.DynamicProxy.IInvocation>>();
        }

        public ApiFactory(IApiServiceLocator apiLocator)
        {
            var apiBaseUrl = apiLocator.GetServiceLocation(typeof(TService).Name);
            this.client = new WebApiClient();
            this.client.BaseUrl = apiBaseUrl;
            this.executeMethods = new ConcurrentDictionary<Type, Action<Castle.DynamicProxy.IInvocation>>();
        }
        public ApiFactory(IServiceProvider serviceProvider)
        {
            var locator = (IApiServiceLocator)serviceProvider.GetService(typeof(IApiServiceLocator));
            var serviceLocation = locator.GetServiceLocation(typeof(TService).Name);
            this.client = new WebApiClient();
            this.client.BaseUrl = serviceLocation;
            this.executeMethods = new ConcurrentDictionary<Type, Action<Castle.DynamicProxy.IInvocation>>();
        }



        public void Execute<T>(Castle.DynamicProxy.IInvocation invocation)
                  => invocation.ReturnValue = client.Execute<T>(invocation.Method.Name, invocation.ArgumentDictionary());

        public void ExecuteAsync<T>(Castle.DynamicProxy.IInvocation invocation)
            => invocation.ReturnValue = client.ExecuteAsync<T>(invocation.Method.Name, invocation.ArgumentDictionary());

        public void Intercept(Castle.DynamicProxy.IInvocation invocation)
        {
            var method = invocation.Method;
            var target = executeMethods.GetOrAdd(method.ReturnType, x =>
            {
                var isGeneric = x.IsGenericType;
                var isAsync = x == typeof(Task) || (isGeneric && x.GetGenericTypeDefinition() == typeof(Task<>));
                var interceptMethodName = isAsync ? nameof(ExecuteAsync) : nameof(Execute);
                var interceptMethod = this.GetType()
                    .GetMethod(interceptMethodName)
                    .MakeGenericMethod(isGeneric ? x.GetGenericArguments() : new[] { x });

                var instance = Expression.Constant(this);
                var invocationParameter = Expression.Parameter(typeof(Castle.DynamicProxy.IInvocation), "invocation");
                var call = Expression.Call(instance, interceptMethod, invocationParameter);
                return Expression.Lambda<Action<Castle.DynamicProxy.IInvocation>>(call, invocationParameter).Compile();

            });

            target(invocation);

            return;
        }



        internal TService CreateClient()
        {
            var proxyGenerator = new ProxyGenerator();
            return proxyGenerator.CreateInterfaceProxyWithoutTarget<TService>(this);
        }
    }

    public class TestInterfaceInterceptor : IInterceptor
    {
        private IWebApiClient client;

        private ConcurrentDictionary<Type, Action<Castle.DynamicProxy.IInvocation>> genericInterceptors;

        public TestInterfaceInterceptor(WebApiClient client)
        {
            this.client = client;
            this.genericInterceptors = new ConcurrentDictionary<Type, Action<Castle.DynamicProxy.IInvocation>>();
        }

        public void GenericIntercept<T>(Castle.DynamicProxy.IInvocation invocation)
            => invocation.ReturnValue = client.Execute<T>(invocation.Method.Name, invocation.ArgumentDictionary());

        public void GenericAsyncIntercept<T>(Castle.DynamicProxy.IInvocation invocation)
            => invocation.ReturnValue = client.ExecuteAsync<T>(invocation.Method.Name, invocation.ArgumentDictionary());

        public void Intercept(Castle.DynamicProxy.IInvocation invocation)
        {
            var method = invocation.Method;
            var interceptorMethod = genericInterceptors.GetOrAdd(method.ReturnType, x =>
            {
                var isGeneric = x.IsGenericType;
                var isAsync = x == typeof(Task) || (isGeneric && x.GetGenericTypeDefinition() == typeof(Task<>));
                var interceptMethodName = isAsync ? nameof(GenericAsyncIntercept) : nameof(GenericIntercept);
                var interceptMethod = this.GetType()
                    .GetMethod(interceptMethodName)
                    .MakeGenericMethod(isGeneric ? x.GetGenericArguments() : new[] { x });
                return CompileGenericInterceptor(this, interceptMethod);

            });

            interceptorMethod(invocation);
            return;
        }

        private Action<Castle.DynamicProxy.IInvocation>
            CompileGenericInterceptor(TestInterfaceInterceptor testInterfaceInterceptor, MethodInfo interceptor)
        {
            var instance = Expression.Constant(testInterfaceInterceptor);
            var invocationParameter = Expression.Parameter(typeof(Castle.DynamicProxy.IInvocation), "invocation");
            var call = Expression.Call(instance, interceptor, invocationParameter);

            return Expression.Lambda<Action<Castle.DynamicProxy.IInvocation>>(call, invocationParameter).Compile();

        }
    }

    public interface IApiServiceLocator
    {
        string GetServiceLocation(string serviceName);
    }

  
    public class UnitTest1
    {

      

        [Fact]
        public void Test1()
        {

            var proxyGen = new ProxyGenerator();

            var client = new WebApiClient();
            client.BaseUrl = "http://localhost:28180/api/ilogger/";
            var i = new TestInterfaceInterceptor(client);

            var p = proxyGen.CreateInterfaceProxyWithoutTarget<ILogger>(i);


            var result = p.Log("Hello");
            var t = p.LogAsync("AsyncErrorMessage");
            t.Wait();
            var asyncResult = t.Result;
            Task.Run(() => p.LogAsync("Async Error Message").GetAwaiter().GetResult()).GetAwaiter().GetResult();
            p.LogError("Error Message");
            p.LogException(new Exception("Test Exception"));
            p.LogException("Test Exception Message");
            p.LogException(new Exception("Test Exception"), "Test Exception Message");
            p.LogWarning("Test Warning");

        }

        [Fact]
        public void TestGeneric()
        {
            var proxyGen = new ProxyGenerator();

            var client = new WebApiClient();
            client.BaseUrl = "http://localhost:28180/api/ilogger/";


            var apiLoggerFactory = new ApiFactory<ILogger>(client);
            var logger = apiLoggerFactory.CreateClient();

            var guid1 = logger.Log("Hello");
            var guid2 = logger.LogAsync("Hello Async").GetAwaiter().GetResult();

        }


        [Fact]
        public void TestGenericNancy()
        {
            var proxyGen = new ProxyGenerator();

            var client = new WebApiClient();
            client.BaseUrl = "http://localhost:8080/ilogger/";


            var apiLoggerFactory = new ApiFactory<ILogger>(client);
            var logger = apiLoggerFactory.CreateClient();
            var exceptionType = typeof(System.Exception);
            var guid1 = logger.Log("Hello");
            var guid2 = logger.LogAsync("Hello Async").GetAwaiter().GetResult();
            var guid3 = logger.LogException(new Exception(), "argument x was null");
            var guid4 = logger.LogException(new ArgumentNullException(), "argument x was null");
        }

        [Fact]
        public void TestGenericWithLocator()
        {
            var locatorClient = new WebApiClient();
            locatorClient.BaseUrl = "http://localhost:28180/api/ServiceLocator/";

            var locatorFactory = new ApiFactory<IApiServiceLocator>(locatorClient);
            var apiLocator = locatorFactory.CreateClient();


            //var client = new WebApiClient();
            //client.BaseUrl = "http://localhost:28180/api/ilogger/";


            var apiLoggerFactory = new ApiFactory<ILogger>(apiLocator);
            var logger = apiLoggerFactory.CreateClient();

            var guid1 = logger.Log("Hello");
            var guid2 = logger.LogAsync("Hello Async").GetAwaiter().GetResult();
            var someException = new ArgumentException("Argument error occured");
            var guid3 = logger.LogException(someException, "ArgumentException:");

        }

        [Fact]
        public void TestCompileHandle()
        {
            var args = new object[] { "a", "b" };
            var instance = Expression.Constant(this);

            var arrayExpr = Expression.Parameter(typeof(object[]), "input");

            var vars = new List<ParameterExpression>();
            var assignments = new List<Expression>();

            var accessExpressions = new List<Expression>();
            for (var i = 0; i < args.Length; i++)
            {
                var type = args[i].GetType();
                var varExpression = Expression.Variable(type, "var" + i.ToString());
                vars.Add(varExpression);

                //ParameterExpression arrayExpr = Expression.Parameter(typeof(int[]), "Array");

                // This parameter expression represents an array index.            
                //ParameterExpression indexExpr = System.Linq.Expressions.Expression.Constant(0); ;// Expression.Parameter(typeof(int), "Index");

                // This parameter represents the value that will be added to a corresponding array element.
                ParameterExpression valueExpr = Expression.Parameter(typeof(int), "Value");

                // This expression represents an array access operation.
                // It can be used for assigning to, or reading from, an array element.
                Expression arrayAccessExpr = Expression.ArrayAccess(
                    arrayExpr,
                    System.Linq.Expressions.Expression.Constant(i)
                );
                accessExpressions.Add(Expression.Convert(Expression.ArrayAccess(
                    arrayExpr,
                    System.Linq.Expressions.Expression.Constant(i)
                ), type));


                Expression convert = Expression.Convert(arrayAccessExpr, type);
                BinaryExpression assignment = Expression.Assign(varExpression, convert);
                assignments.Add(assignment);

            }


            var callArgs = new List<ParameterExpression>();
            var method = this.GetType().GetMethod(nameof(HandlerTest1));
            var callExpression = Expression.Call(instance, method, accessExpressions);
            var box = Expression.Convert(callExpression, typeof(object));

            assignments.Add(box);
            BlockExpression execute = Expression.Block(vars.Concat(assignments));
            var exStr = execute.ToString();
            // vars.Insert(0, arrayExpr);
            var fun = Expression.Lambda<Func<object[], object>>(box, arrayExpr).Compile();

            var result = (string)fun(args);
            Assert.True(result == "ab");


        }
        public string HandlerTest1(string a, string b)
        {
            return a + b;
        }
    }


}

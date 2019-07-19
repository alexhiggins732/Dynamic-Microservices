
using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace Microservices.Logging.LoggingApi
{

    namespace ApiService
    {
   

        public interface IWebApiClient
        {
            object Execute(Type returnType, string endpoint);
            object Execute(Type returnType, string endpoint, Dictionary<string, object> payload);
            T Execute<T>(string endpoint);
            Task<T> ExecuteAsync<T>(string endpoint);
            T Execute<T>(string endpoint, Dictionary<string, object> payload);
            Task<T> ExecuteAsync<T>(string endpoint, Dictionary<string, object> payload);
        }
        public class WebApiClient : IWebApiClient
        {

            public object Execute(Type returnType, string endpoint) => Execute(returnType, endpoint, null);
            public object Execute(Type returnType, string endpoint, Dictionary<string, object> payload)
            {
                var json = JsonConvert.SerializeObject(payload);
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                using (var client = new WebClient())
                {
                    var resultData = client.UploadData(endpoint, bytes);
                    var resultJson = System.Text.Encoding.UTF8.GetString(resultData);
                    return JsonConvert.DeserializeObject(resultJson, returnType);
                }
            }
 



            public T Execute<T>(string endpoint) => Execute<T>(endpoint, null);
            public Task<T> ExecuteAsync<T>(string endpoint) => ExecuteAsync<T>(endpoint, null);
            public T Execute<T>(string endpoint, Dictionary<string, object> param)
            {
                var json = JsonConvert.SerializeObject(param);
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                using (var client = new WebClient())
                {
                    var resultData = client.UploadData(endpoint, bytes);
                    var resultJson = System.Text.Encoding.UTF8.GetString(resultData);
                    return JsonConvert.DeserializeObject<T>(resultJson);
                }
            }

            public Task<T> ExecuteAsync<T>(string endpoint, Dictionary<string, object> param)
                => Task.Run<T>(() => Execute<T>(endpoint, param));

        }

    }

}

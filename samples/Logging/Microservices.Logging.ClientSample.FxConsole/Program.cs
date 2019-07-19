using Microservices.Logging.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservices.Logging.ClientSample.FxConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var loggingClient = ClientBuilder<ILogger>()
                .Configure(x => ConfigureClient(x));
                //.Build();
        }

        private static object ConfigureClient(object x)
        {
            throw new NotImplementedException();
        }

        private static IMicroserviceClientBuilder<T> ClientBuilder<T>()
        {
            throw new NotImplementedException();
        }

        public interface IMicroserviceClientBuilder<T>
        {
            object Configure(Func<object, object> p);
        }


    }
}
namespace Microservice
{

    public enum ClientType
    {
        WebApi,
        WebSocket,
        Wcf,
        Wsdl,
        Soap,
        SignalR
    }
    public class ClientOptions<T>
    {
        public virtual ClientType ClientType { get; protected set; }
    }



    public class WebApiClientOptions : ClientOptions<Microservice.WebApiClient>
    {
        public override ClientType ClientType { get => ClientType.WebApi; }
        public string BaseUrl { get; set; }
    }

    public class WebApiClient
    {

    }
}

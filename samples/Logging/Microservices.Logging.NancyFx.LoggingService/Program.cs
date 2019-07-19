using Microservices.Logging.Common;
using Nancy;
using Nancy.Hosting.Self;
using Nancy.TinyIoc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservices.Logging.NancyFx.LoggingService
{
    public class CustomBootstrapper : DefaultNancyBootstrapper
    {
         protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);
            container.Register<ILogger>(new LoggerService());
            //container.Register<ConfigurationRoot>(config);
        }

      
    }
    class Program
    {
        static void Main(string[] args)
        {
            HostConfiguration hostConfigs = new HostConfiguration()
            {
                UrlReservations = new UrlReservations() { CreateAutomatically = true }
            };
            using (var host = new NancyHost(hostConfigs, new Uri("http://localhost:8080")))
            {
               
                host.Start();
                Console.WriteLine("Server started at http://localhost:8080");
                Console.WriteLine("Press any key to exit...");
                Console.ReadLine();
            }

        }
    }
}

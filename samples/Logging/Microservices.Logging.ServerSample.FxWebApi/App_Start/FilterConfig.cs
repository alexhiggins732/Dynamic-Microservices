using System.Web;
using System.Web.Mvc;

namespace Microservices.Logging.ServerSample.FxWebApi
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}

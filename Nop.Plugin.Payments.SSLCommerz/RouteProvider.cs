using Nop.Web.Framework.Mvc.Routes;
using System.Web.Routing;
using System.Web.Mvc;

namespace Nop.Plugin.Payments.EasyPayWay
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            //Success and failure URL
            routes.MapRoute("Plugin.Payments.SSLCommerz.PaymentResult",
                 "Plugins/PaymentSSLCommerz/PaymentResult",
                 new { controller = "PaymentSSLCommerz", action = "PaymentResult" },
                 new[] { "Nop.Plugin.Payments.SSLCommerz.Controllers" }
            );
            //cancel URL
            routes.MapRoute("Plugin.Payments.SSLCommerz.CancelOrder",
                 "Plugins/PaymentSSLCommerz/CancelOrder",
                 new { controller = "PaymentSSLCommerz", action = "CancelOrder" },
                 new[] { "Nop.Plugin.Payments.SSLCommerz.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}

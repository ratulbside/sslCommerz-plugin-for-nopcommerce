using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using System.Collections.Generic;

namespace Nop.Plugin.Payments.SSLCommerz.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        public ConfigurationModel()
        {
            PrefferedCardTypes = new List<string>();
            AvailableCardTypes = new List<string>();
        }

        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.SSLCommerz.Fields.UseSandbox")]
        public bool UseSandbox { get; set; }
        public bool UseSandbox_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.SSLCommerz.Fields.StoreId")]
        public string StoreId { get; set; }
        public bool StoreId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.SSLCommerz.Fields.StorePassword")]
        public string StorePassword { get; set; }
        public bool StorePassword_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.SSLCommerz.Fields.PassProductNamesAndTotals")]
        public bool PassProductNamesAndTotals { get; set; }
        public bool PassProductNamesAndTotals_OverrideForStore { get; set; }

        public IList<string> PrefferedCardTypes { get; set; }
        [NopResourceDisplayName("Plugins.Payments.SSLCommerz.Fields.PrefferedCardType")]
        public IList<string> AvailableCardTypes { get; set; }
        public string[] CheckedCardTypes { get; set; }
        public bool PrefferedCardTypes_OverrideForStore { get; set; }
        

        [NopResourceDisplayName("Plugins.Payments.SSLCommerz.Fields.EnableIpn")]
        public bool EnableIpn { get; set; }
        public bool EnableIpn_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.SSLCommerz.Fields.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFee_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.SSLCommerz.Fields.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }
        public bool AdditionalFeePercentage_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.SSLCommerz.Fields.ReturnFromSSLCommerzWithoutPaymentRedirectsToOrderDetailsPage")]
        public bool ReturnFromSSLCommerzWithoutPaymentRedirectsToOrderDetailsPage { get; set; }
        public bool ReturnFromSSLCommerzWithoutPaymentRedirectsToOrderDetailsPage_OverrideForStore { get; set; }
    }
}

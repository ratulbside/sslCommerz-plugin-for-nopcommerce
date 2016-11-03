using Nop.Core.Configuration;
using Nop.Plugin.Payments.SSLCommerz.Domain;

namespace Nop.Plugin.Payments.SSLCommerz
{
    public class SslCommerzPaymentSettings : ISettings
    {
        public bool UseSandbox { get; set; }
        public string StoreId { get; set; }
        public string StorePassword { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }
        public decimal AdditionalFee { get; set; }
        public bool PassProductNamesAndTotals { get; set; }
        public string PrefferedCardTypes { get; set; }
        public bool EnableIpn { get; set; }
        /// <summary>
        /// Enable if a customer should be redirected to the order details page
        /// when he clicks "cancel transaction" link on SSLCommerz site
        /// WITHOUT completing a payment
        /// </summary>
        public bool ReturnFromSSLCommerzWithoutPaymentRedirectsToOrderDetailsPage { get; set; }
    }
}

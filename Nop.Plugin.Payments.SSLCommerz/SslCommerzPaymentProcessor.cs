using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.SSLCommerz.Controllers;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Orders;
using Nop.Services.Payments;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Routing;
using Nop.Services.Localization;
using System.Text;
using System.Web;
using Nop.Services.Tax;
using Nop.Web.Framework;
using System.Net;
using System.IO;
using Nop.Services.Logging;
using Newtonsoft.Json;
using Nop.Core.Domain.Shipping;

namespace Nop.Plugin.Payments.SSLCommerz
{
    /// <summary>
    /// SSLCommerz payment processor
    /// </summary>
    public class SslCommerzPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly SslCommerzPaymentSettings _sslCommerzPaymentSettings;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ITaxService _taxService;
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly IOrderService _orderService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly HttpContextBase _httpContext;
        private readonly ILogger _logger;

        #endregion

        #region Ctor

        public SslCommerzPaymentProcessor(SslCommerzPaymentSettings sslCommerzPaymentSettings,
            ISettingService settingService,
            IWebHelper webHelper,
            ICheckoutAttributeParser checkoutAttributeParser,
            ITaxService taxService,
            ICurrencyService currencyService,
            CurrencySettings currencySettings,
            IOrderService orderService,
            IOrderTotalCalculationService orderTotalCalculationService,
            HttpContextBase httpContext,
            ILogger logger)
        {
            this._sslCommerzPaymentSettings = sslCommerzPaymentSettings;
            this._settingService = settingService;
            this._webHelper = webHelper;
            this._checkoutAttributeParser = checkoutAttributeParser;
            this._taxService = taxService;
            this._currencyService = currencyService;
            this._currencySettings = currencySettings;
            this._orderService = orderService;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._httpContext = httpContext;
            this._logger = logger;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets SSLCommerz URL
        /// </summary>
        /// <returns></returns>
        private string GetSSLCommerzUrl()
        {
            return _sslCommerzPaymentSettings.UseSandbox ? "https://securepay.sslcommerz.com/gwprocess/testbox/v3/process.php" : "https://securepay.sslcommerz.com/gwprocess/v3/process.php";
        }

        private string GetSSLCommerzValidationUrl()
        {
            return _sslCommerzPaymentSettings.UseSandbox ? "https://securepay.sslcommerz.com/validator/api/testbox/validationserverAPI.php" : "https://securepay.sslcommerz.com/validator/api/validationserverAPI.php";
        }

        private bool IsNaturalNumber(decimal amount)
        {
            decimal diff = Math.Abs(Math.Truncate(amount) - amount);
            return (diff < Convert.ToDecimal(0.0000001)) || (diff > Convert.ToDecimal(0.9999999));
        }

        public bool GetValidationDetails(string valId, out Dictionary<string, string> values)
        {
            var builder = new StringBuilder();
            builder.AppendFormat("val_id={0}", valId);
            builder.AppendFormat("&Store_Id={0}", _sslCommerzPaymentSettings.StoreId);
            builder.AppendFormat("&Store_Passwd={0}", _sslCommerzPaymentSettings.StorePassword);
            builder.Append("&format=json");

            var url = GetSSLCommerzValidationUrl() + "?" + builder.ToString();

            var req = (HttpWebRequest)WebRequest.Create(url);

            string response = string.Empty;

            using (var sr = new StreamReader(req.GetResponse().GetResponseStream()))
            {
                response = HttpUtility.UrlDecode(sr.ReadToEnd());
            }

            if (!String.IsNullOrEmpty(response))
            {
                values = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
                return true;
            }

            values = new Dictionary<string, string>();
            return false;
        }

        #endregion

        #region Methods

        // <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.NewPaymentStatus = PaymentStatus.Pending;
            return result;
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var post = new RemotePost();

            post.FormName = "SSLCommerzForm";
            post.Url = GetSSLCommerzUrl();
            post.Method = "POST";

            var order = postProcessPaymentRequest.Order;

            post.Add("tran_id", postProcessPaymentRequest.Order.Id.ToString());
            //total amount
            var orderTotal = Math.Round(postProcessPaymentRequest.Order.OrderTotal, 2);
            post.Add("total_amount", orderTotal.ToString("0.00", CultureInfo.InvariantCulture));

            var storeLocation = _webHelper.GetStoreLocation(false);
            string successUrl = storeLocation + "Plugins/PaymentSSLCommerz/PaymentResult";
            string cancelUrl = storeLocation + "Plugins/PaymentSSLCommerz/CancelOrder";
            string failureUrl = storeLocation + "Plugins/PaymentSSLCommerz/PaymentResult";
            post.Add("success_url", successUrl);
            post.Add("fail_url", failureUrl);
            post.Add("cancel_url", cancelUrl);

            post.Add("store_id", _sslCommerzPaymentSettings.StoreId);

            if (_sslCommerzPaymentSettings.PassProductNamesAndTotals)
            {
                //get the items in the cart
                decimal cartTotal = decimal.Zero;
                var cartItems = postProcessPaymentRequest.Order.OrderItems;
                int x = 0;
                foreach (var item in cartItems)
                {
                    var unitPriceExclTax = item.UnitPriceExclTax;
                    var priceExclTax = item.PriceExclTax;
                    //round
                    var unitPriceExclTaxRounded = Math.Round(unitPriceExclTax, 2);

                    post.Add(String.Format("cart[{0}][product]", x), item.Product.Name + " (Quantity: " + item.Quantity + ")");
                    post.Add(String.Format("cart[{0}][amount]", x), unitPriceExclTaxRounded.ToString("0.00", CultureInfo.InvariantCulture));
                    x++;
                    cartTotal += priceExclTax;
                }

                //the checkout attributes that have a dollar value and send them to SSLCommerz as items to be paid for
                var attributeValues = _checkoutAttributeParser.ParseCheckoutAttributeValues(postProcessPaymentRequest.Order.CheckoutAttributesXml);
                foreach (var val in attributeValues)
                {
                    var attPrice = _taxService.GetCheckoutAttributePrice(val, false, postProcessPaymentRequest.Order.Customer);
                    //round
                    var attPriceRounded = Math.Round(attPrice, 2);
                    if (attPrice > decimal.Zero) //if it has a price
                    {
                        var attribute = val.CheckoutAttribute;
                        if (attribute != null)
                        {
                            var attName = attribute.Name; //set the name
                            post.Add(String.Format("cart[{0}][product]", x), attName); //name
                            post.Add(String.Format("cart[{0}][amount]", x), attPriceRounded.ToString("0.00", CultureInfo.InvariantCulture)); //amount
                            x++;
                            cartTotal += attPrice;
                        }
                    }
                }

                //shipping
                var orderShippingExclTax = postProcessPaymentRequest.Order.OrderShippingExclTax;
                var orderShippingExclTaxRounded = Math.Round(orderShippingExclTax, 2);
                if (orderShippingExclTax > decimal.Zero)
                {
                    post.Add(String.Format("cart[{0}][product]", x), "Shipping fee");
                    post.Add(String.Format("cart[{0}][amount]", x), orderShippingExclTaxRounded.ToString("0.00", CultureInfo.InvariantCulture));
                    x++;
                    cartTotal += orderShippingExclTax;
                }

                //payment method additional fee
                var paymentMethodAdditionalFeeExclTax = postProcessPaymentRequest.Order.PaymentMethodAdditionalFeeExclTax;
                var paymentMethodAdditionalFeeExclTaxRounded = Math.Round(paymentMethodAdditionalFeeExclTax, 2);
                if (paymentMethodAdditionalFeeExclTax > decimal.Zero)
                {
                    post.Add(String.Format("cart[{0}][product]", x), "Payment method fee");
                    post.Add(String.Format("cart[{0}][amount]", x), paymentMethodAdditionalFeeExclTaxRounded.ToString("0.00", CultureInfo.InvariantCulture));
                    x++;
                    cartTotal += paymentMethodAdditionalFeeExclTax;
                }

                //tax
                var orderTax = postProcessPaymentRequest.Order.OrderTax;
                var orderTaxRounded = Math.Round(orderTax, 2);
                if (orderTax > decimal.Zero)
                {
                    //post.Add("tax_1", orderTax.ToString("0.00", CultureInfo.InvariantCulture));

                    //add tax as item
                    post.Add(String.Format("cart[{0}][product]", x), "Sales Tax"); //name
                    post.Add(String.Format("cart[{0}][amount]", x), orderTaxRounded.ToString("0.00", CultureInfo.InvariantCulture)); //amount

                    cartTotal += orderTax;
                    x++;
                }

                if (cartTotal > postProcessPaymentRequest.Order.OrderTotal)
                {
                    /* Take the difference between what the order total is and what it should be and use that as the "discount".
                     * The difference equals the amount of the gift card and/or reward points used. 
                     */
                    decimal discountTotal = cartTotal - postProcessPaymentRequest.Order.OrderTotal;
                    discountTotal = Math.Round(discountTotal, 2);
                    //gift card or rewared point amount applied to cart in nopCommerce - shows in SSLCommerz as "Product"
                    post.Add(String.Format("cart[{0}][product]", x), "Discount"); //name
                    post.Add(String.Format("cart[{0}][amount]", x), discountTotal.ToString("-0.00", CultureInfo.InvariantCulture)); //amount
                }
            }

            if (!String.IsNullOrEmpty(_sslCommerzPaymentSettings.PrefferedCardTypes))
                 post.Add("multi_card_name", _sslCommerzPaymentSettings.PrefferedCardTypes);

            post.Add("cus_name", HttpUtility.UrlEncode(order.BillingAddress.FirstName) + " " + HttpUtility.UrlEncode(order.BillingAddress.LastName));
            post.Add("cus_email", order.BillingAddress.Email);
            post.Add("cus_add1", HttpUtility.UrlEncode(order.BillingAddress.Address1));
            post.Add("cus_add2", HttpUtility.UrlEncode(order.BillingAddress.Address2));
            post.Add("cus_city", HttpUtility.UrlEncode(order.BillingAddress.City));
            if (order.BillingAddress.StateProvince != null)
                post.Add("cus_state", HttpUtility.UrlEncode(order.BillingAddress.StateProvince.Name));
            else
                post.Add("cus_state", "");
            post.Add("cus_postcode", HttpUtility.UrlEncode(order.BillingAddress.ZipPostalCode));
            if (order.BillingAddress.Country != null)
                post.Add("cus_country", HttpUtility.UrlEncode(order.BillingAddress.Country.Name));
            else
                post.Add("cus_country", "");
            post.Add("cus_phone", HttpUtility.UrlEncode(order.BillingAddress.PhoneNumber));
            post.Add("cus_fax", HttpUtility.UrlEncode(order.BillingAddress.FaxNumber));

            if (order.ShippingStatus != ShippingStatus.ShippingNotRequired && (order.BillingAddress.FirstName != order.ShippingAddress.FirstName || order.BillingAddress.LastName != order.ShippingAddress.LastName || order.BillingAddress.Address1 != order.ShippingAddress.Address1)) //check whether billing & shipping address are the same
            {
                post.Add("ship_name", HttpUtility.UrlEncode(order.ShippingAddress.FirstName) + " " + HttpUtility.UrlEncode(order.ShippingAddress.LastName));
                post.Add("ship_add1", HttpUtility.UrlEncode(order.ShippingAddress.Address1));
                post.Add("ship_add2", HttpUtility.UrlEncode(order.ShippingAddress.Address2));
                post.Add("ship_city", HttpUtility.UrlEncode(order.ShippingAddress.City));
                if (order.ShippingAddress.StateProvince != null)
                    post.Add("ship_state", HttpUtility.UrlEncode(order.ShippingAddress.StateProvince.Name));
                else
                    post.Add("ship_state", "");
                post.Add("ship_postcode", HttpUtility.UrlEncode(order.ShippingAddress.ZipPostalCode));
                if (order.ShippingAddress.Country != null)
                    post.Add("ship_country", HttpUtility.UrlEncode(order.ShippingAddress.Country.Name));
                else
                    post.Add("ship_country", "");
            }

            //custom values
            post.Add("value_a", "Order GUID: " + order.OrderGuid);
            post.Add("value_b", "Order GUID: " + order.OrderGuid);
            post.Add("value_c", "Order GUID: " + order.OrderGuid);
            post.Add("value_d", "Order GUID: " + order.OrderGuid);

            post.Add("currency", order.CustomerCurrencyCode);

            post.Post();
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return false;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            var result = this.CalculateAdditionalFee(_orderTotalCalculationService, cart,
                _sslCommerzPaymentSettings.AdditionalFee, _sslCommerzPaymentSettings.AdditionalFeePercentage);
            return result;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return result;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return result;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            //let's ensure that at least 5 seconds passed after order is placed
            //P.S. there's no any particular reason for that. we just do it
            if (order.OrderStatus == OrderStatus.Pending)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "PaymentSSLCommerz";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Payments.SSLCommerz.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Gets a route for payment info
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "PaymentSSLCommerz";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Payments.SSLCommerz.Controllers" }, { "area", null } };
        }

        public Type GetControllerType()
        {
            return typeof(PaymentSSLCommerzController);
        }

        public override void Install()
        {
            //settings
            var settings = new SslCommerzPaymentSettings()
            {
                UseSandbox = true,
                StoreId = "testbox",
                StorePassword = "qwerty",
                AdditionalFee = 0,
                EnableIpn = true,
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SSLCommerz.RedirectionTip", "You will be redirected to SSLCommerz site to complete the order.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.UseSandbox", "Use Test Mode");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.UseSandbox.Hint", "Mark if you want to test the gateway");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.StoreId", "Store ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.StoreId.Hint", "Enter Store ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.StorePassword", "Store Password");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.StorePassword.Hint", "Store Password (Supplied by SSLCommerz)");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.PassProductNamesAndTotals", "Pass product names and order totals to SSLCommerz");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.PassProductNamesAndTotals.Hint", "Check if product names and order totals should be passed to SSLCommerz.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.PrefferedCardTypes", "Card Types");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.PrefferedCardTypes.Hint", "Select your preffered banks' Payment Gateway as default choice on the SSLCOMMERZ Gateway Page.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.EnableIpn", "Enable IPN");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.EnableIpn.Hint", "Enable IPN");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.AdditionalFee", "Additional fee");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.AdditionalFee.Hint", "Additional fee");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.AdditionalFeePercentage", "Additional fee. Use percentage");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.AdditionalFeePercentage.Hint", "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.ReturnFromSSLCommerzWithoutPaymentRedirectsToOrderDetailsPage", "Return to order details page");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.ReturnFromSSLCommerzWithoutPaymentRedirectsToOrderDetailsPage.Hint", "Enable if a customer should be redirected to the order details page when he clicks \"Cancel Transaction\" link on SSLCommerz site WITHOUT completing a payment");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SSLCommerz.PaymentErrorMessage", "There was a problem with your payment. Please click details below and then try to complete the payment again.");

            base.Install();
        }

        public override void Uninstall()
        {
            //locales
            this.DeletePluginLocaleResource("Plugins.Payments.SSLCommerz.RedirectionTip");
            this.DeletePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.UseSandbox");
            this.DeletePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.UseSandbox.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.StoreId");
            this.DeletePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.StoreId.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.StorePassword");
            this.DeletePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.StorePassword.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.PassProductNamesAndTotals");
            this.DeletePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.PassProductNamesAndTotals.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.PrefferedCardTypes");
            this.DeletePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.PrefferedCardTypes.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.EnableIpn");
            this.DeletePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.EnableIpn.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.AdditionalFee");
            this.DeletePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.AdditionalFee.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.AdditionalFeePercentage");
            this.DeletePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.AdditionalFeePercentage.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.ReturnFromSSLCommerzWithoutPaymentRedirectsToOrderDetailsPage");
            this.DeletePluginLocaleResource("Plugins.Payments.SSLCommerz.Fields.ReturnFromSSLCommerzWithoutPaymentRedirectsToOrderDetailsPage.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.SSLCommerz.PaymentErrorMessage");

            base.Uninstall();
        }

        #endregion

        #region Properies

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get
            {
                return RecurringPaymentType.NotSupported;
            }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get
            {
                return PaymentMethodType.Redirection;
            }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get
            {
                return false;
            }
        }

        #endregion
    }
}

using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.SSLCommerz.Domain;
using Nop.Plugin.Payments.SSLCommerz.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace Nop.Plugin.Payments.SSLCommerz.Controllers
{
    public class PaymentSSLCommerzController : BasePaymentController
    {
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IStoreContext _storeContext;
        private readonly ILogger _logger;
        private readonly IWebHelper _webHelper;
        private readonly PaymentSettings _paymentSettings;
        private readonly SslCommerzPaymentSettings _sslCommerzPaymentSettings;
        private readonly ILocalizationService _localizationService;

        public PaymentSSLCommerzController(
            IWorkContext workContext,
            IStoreService storeService,
            ISettingService settingService,
            IPaymentService paymentService,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            IStoreContext storeContext,
            ILogger logger,
            IWebHelper webHelper,
            PaymentSettings paymentSettings,
            SslCommerzPaymentSettings sslCommerzPaymentSettings,
            ILocalizationService localizationService)
        {
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._storeContext = storeContext;
            this._logger = logger;
            this._webHelper = webHelper;
            this._paymentSettings = paymentSettings;
            this._sslCommerzPaymentSettings = sslCommerzPaymentSettings;
            this._localizationService = localizationService;
        }

        [ChildActionOnly]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var sslCommerzPaymentSettings = _settingService.LoadSetting<SslCommerzPaymentSettings>(storeScope);

            var model = new ConfigurationModel();

            model.UseSandbox = sslCommerzPaymentSettings.UseSandbox;
            model.StoreId = sslCommerzPaymentSettings.StoreId;
            model.StorePassword = sslCommerzPaymentSettings.StorePassword;
            model.PassProductNamesAndTotals = sslCommerzPaymentSettings.PassProductNamesAndTotals;            
            model.EnableIpn = sslCommerzPaymentSettings.EnableIpn;
            model.AdditionalFee = sslCommerzPaymentSettings.AdditionalFee;
            model.AdditionalFeePercentage = sslCommerzPaymentSettings.AdditionalFeePercentage;
            model.ReturnFromSSLCommerzWithoutPaymentRedirectsToOrderDetailsPage = sslCommerzPaymentSettings.ReturnFromSSLCommerzWithoutPaymentRedirectsToOrderDetailsPage;

            var cardTypes = new CardTypes();
            //Load card names
            string prefferedCardTypes = _sslCommerzPaymentSettings.PrefferedCardTypes;
            foreach (string cardType in cardTypes.CardNames)
                model.AvailableCardTypes.Add(cardType);

            if (!String.IsNullOrEmpty(prefferedCardTypes))
                foreach (string cardType in cardTypes.CardNames)
                {
                    string cardId = CardTypes.GetCardId(cardType);
                    if (!String.IsNullOrEmpty(cardId) && !String.IsNullOrEmpty(prefferedCardTypes))
                    {
                        if (prefferedCardTypes.Contains(cardId))
                            model.PrefferedCardTypes.Add(cardType);
                    }
                }

            model.ActiveStoreScopeConfiguration = storeScope;
            if (storeScope > 0)
            {
                model.UseSandbox_OverrideForStore = _settingService.SettingExists(sslCommerzPaymentSettings, x => x.UseSandbox, storeScope);
                model.StoreId_OverrideForStore = _settingService.SettingExists(sslCommerzPaymentSettings, x => x.StoreId, storeScope);
                model.StorePassword_OverrideForStore = _settingService.SettingExists(sslCommerzPaymentSettings, x => x.StorePassword, storeScope);
                model.PassProductNamesAndTotals_OverrideForStore = _settingService.SettingExists(sslCommerzPaymentSettings, x => x.PassProductNamesAndTotals, storeScope);
                model.PrefferedCardTypes_OverrideForStore = _settingService.SettingExists(sslCommerzPaymentSettings, x => x.PrefferedCardTypes, storeScope);
                model.EnableIpn_OverrideForStore = _settingService.SettingExists(sslCommerzPaymentSettings, x => x.EnableIpn, storeScope);
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(sslCommerzPaymentSettings, x => x.AdditionalFee,
                    storeScope);
                model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(sslCommerzPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
                model.ReturnFromSSLCommerzWithoutPaymentRedirectsToOrderDetailsPage_OverrideForStore = _settingService.SettingExists(sslCommerzPaymentSettings, x => x.ReturnFromSSLCommerzWithoutPaymentRedirectsToOrderDetailsPage, storeScope);
            }

            return View("~/Plugins/Payments.SSLCommerz/Views/PaymentSSLCommerz/Configure.cshtml", model);
        }

        [HttpPost]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var sslCommerzPaymentSettings = _settingService.LoadSetting<SslCommerzPaymentSettings>(storeScope);

            //save settings
            sslCommerzPaymentSettings.UseSandbox = model.UseSandbox;
            sslCommerzPaymentSettings.StoreId = model.StoreId;
            sslCommerzPaymentSettings.StorePassword = model.StorePassword;
            sslCommerzPaymentSettings.PassProductNamesAndTotals = model.PassProductNamesAndTotals;
            sslCommerzPaymentSettings.EnableIpn = model.EnableIpn;
            sslCommerzPaymentSettings.AdditionalFee = model.AdditionalFee;
            sslCommerzPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
            sslCommerzPaymentSettings.ReturnFromSSLCommerzWithoutPaymentRedirectsToOrderDetailsPage = model.ReturnFromSSLCommerzWithoutPaymentRedirectsToOrderDetailsPage;

            //save selected cards
            var prefferedCards = new StringBuilder();
            int prefferedCardsSelectedCount = 0;
            if (model.CheckedCardTypes != null)
            {
                foreach (var ct in model.CheckedCardTypes)
                {
                    prefferedCardsSelectedCount++;
                    string cardId = CardTypes.GetCardId(ct);
                    if (!String.IsNullOrEmpty(cardId))
                        prefferedCards.AppendFormat("{0},", cardId);
                }
            }
            _sslCommerzPaymentSettings.PrefferedCardTypes = prefferedCards.ToString();

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            if (model.UseSandbox_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(sslCommerzPaymentSettings, x => x.UseSandbox, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(sslCommerzPaymentSettings, x => x.UseSandbox, storeScope);

            if (model.StoreId_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(sslCommerzPaymentSettings, x => x.StoreId, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(sslCommerzPaymentSettings, x => x.StoreId, storeScope);

            if (model.StorePassword_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(sslCommerzPaymentSettings, x => x.StorePassword, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(sslCommerzPaymentSettings, x => x.StorePassword, storeScope);

            if (model.PassProductNamesAndTotals_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(sslCommerzPaymentSettings, x => x.PassProductNamesAndTotals, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(sslCommerzPaymentSettings, x => x.PassProductNamesAndTotals, storeScope);

            if (model.PrefferedCardTypes_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(sslCommerzPaymentSettings, x => x.PrefferedCardTypes, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(sslCommerzPaymentSettings, x => x.PrefferedCardTypes, storeScope);

            if (model.EnableIpn_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(sslCommerzPaymentSettings, x => x.EnableIpn, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(sslCommerzPaymentSettings, x => x.EnableIpn, storeScope);

            if (model.AdditionalFee_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(sslCommerzPaymentSettings, x => x.AdditionalFee, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(sslCommerzPaymentSettings, x => x.AdditionalFee, storeScope);

            if (model.AdditionalFeePercentage_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(sslCommerzPaymentSettings, x => x.AdditionalFeePercentage, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(sslCommerzPaymentSettings, x => x.AdditionalFeePercentage, storeScope);

            if (model.ReturnFromSSLCommerzWithoutPaymentRedirectsToOrderDetailsPage_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(sslCommerzPaymentSettings, x => x.ReturnFromSSLCommerzWithoutPaymentRedirectsToOrderDetailsPage, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(sslCommerzPaymentSettings, x => x.ReturnFromSSLCommerzWithoutPaymentRedirectsToOrderDetailsPage, storeScope);

            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            var model = new PaymentInfoModel();
            return View("~/Plugins/Payments.SSLCommerz/Views/PaymentSSLCommerz/PaymentInfo.cshtml", model);
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();
            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            return paymentInfo;
        }

        [ValidateInput(false)]
        public ActionResult PaymentResult(FormCollection  form)
        {
            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.SSLCommerz") as SslCommerzPaymentProcessor;
            if (processor == null || !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                throw new NopException("SSLCommerz module cannot be loaded");

            string status = CommonHelper.EnsureNotNull(form["status"]);
            string tranDate = CommonHelper.EnsureNotNull(form["tran_date"]);
            string tranId = CommonHelper.EnsureNotNull(form["tran_id"]);
            string valId = CommonHelper.EnsureNotNull(form["val_id"]);
            string amount = CommonHelper.EnsureNotNull(form["amount"]);
            string storeAmount = CommonHelper.EnsureNotNull(form["store_amount"]);
            string cardType = CommonHelper.EnsureNotNull(form["card_type"]);
            string cardNo = CommonHelper.EnsureNotNull(form["card_no"]);
            string currency = CommonHelper.EnsureNotNull(form["currency"]);
            string bankTranId = CommonHelper.EnsureNotNull(form["bank_tran_id"]);
            string cardIssuer = CommonHelper.EnsureNotNull(form["card_issuer"]);
            string cardBrand = CommonHelper.EnsureNotNull(form["card_brand"]);
            string cardIssuerCountry = CommonHelper.EnsureNotNull(form["card_issuer_country"]);
            string cardIssuerCountryCode = CommonHelper.EnsureNotNull(form["card_issuer_country_code"]);
            string currencyType = CommonHelper.EnsureNotNull(form["currency_type"]);
            string currencyAmount = CommonHelper.EnsureNotNull(form["currency_amount"]);
            string valueA = CommonHelper.EnsureNotNull(form["value_a"]);
            string valueB = CommonHelper.EnsureNotNull(form["value_b"]);
            string valueC = CommonHelper.EnsureNotNull(form["value_c"]);
            string valueD = CommonHelper.EnsureNotNull(form["value_d"]);
            string verifySign = CommonHelper.EnsureNotNull(form["varify_sign"]);
            string verifyKey = CommonHelper.EnsureNotNull(form["verify_key"]);
            string riskLevel = CommonHelper.EnsureNotNull(form["risk_level"]);
            string riskTitle = CommonHelper.EnsureNotNull(form["risk_title"]);
            string error = CommonHelper.EnsureNotNull(form["error"]);

            var sb = new StringBuilder();
            sb.AppendLine("Response:");
            sb.AppendLine("Status: " + status);
            sb.AppendLine("Transaction Date: " + tranDate);
            sb.AppendLine("Transaction Id: " + tranId);
            sb.AppendLine("Validation Id: " + valId);
            sb.AppendLine("Amount: " + amount);
            sb.AppendLine("Store Amount: " + storeAmount);            
            sb.AppendLine("Currency: " + currency);
            sb.AppendLine("Bank Transaction ID: " + bankTranId);
            sb.AppendLine("Card Type: " + cardType);
            sb.AppendLine("Card No: " + cardNo);
            sb.AppendLine("Card Issuer: " + cardIssuer);
            sb.AppendLine("Card Brand: " + cardBrand);
            sb.AppendLine("Card Issuer Country: " + cardIssuerCountry);
            sb.AppendLine("Card Issuer Country Code: " + cardIssuerCountryCode);
            sb.AppendLine("Currency Type: " + currencyType);
            sb.AppendLine("Currency Amount: " + currencyAmount);
            sb.AppendLine(valueA);
            sb.AppendLine("Verify Sign: " + verifySign);
            sb.AppendLine("Verify Key: " + verifyKey);
            sb.AppendLine("Risk Level: " + riskLevel);
            sb.AppendLine("Risk Title: " + riskTitle);

            if (!String.IsNullOrEmpty(error)) {
                sb.AppendLine("Error: " + error);
            }

            var order = _orderService.GetOrderById(Convert.ToInt32(tranId));
            if (order != null)
            {
                //order note
                order.OrderNotes.Add(new OrderNote
                {
                    Note = sb.ToString(),
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });
                _orderService.UpdateOrder(order);
            }
            else
            {
                _logger.Error("SSLCommerz Response. Order is not found", new NopException(sb.ToString()));
            }

            var response = string.Empty;
            Dictionary<string, string> values;

            if (_sslCommerzPaymentSettings.EnableIpn && processor.GetValidationDetails(valId, out values))
            {
                string status2 = string.Empty;
                values.TryGetValue("status", out status2);
                string amount2 = string.Empty;
                values.TryGetValue("currency_amount", out amount2);

                var sb2 = new StringBuilder();
                sb2.AppendLine("SSLCommerz IPN:");
                foreach (KeyValuePair<string, string> kvp in values)
                {
                    sb2.AppendLine(kvp.Key + ": " + kvp.Value);
                }

                if (order != null)
                {
                    //order note
                    order.OrderNotes.Add(new OrderNote
                    {
                        Note = sb2.ToString(),
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow
                    });
                    _orderService.UpdateOrder(order);
                }
                else
                {
                    _logger.Error("SSLCommerz Response. Order is not found", new NopException(sb2.ToString()));
                }

                if (status2 == "VALID" && Convert.ToDecimal(amount2) == order.OrderTotal && _orderProcessingService.CanMarkOrderAsPaid(order))
                {
                    _orderProcessingService.MarkOrderAsPaid(order);
                    return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
                }
                else
                {
                    return RedirectToAction("Index", "Home", new { area = "" });
                }
            }
            else if (_sslCommerzPaymentSettings.EnableIpn)
            {
                _logger.Error("Couldn't reach SSLCommerz Server.", new NopException(sb.ToString()));
                return RedirectToAction("Index", "Home", new { area = "" });
            }
            else
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }
        }

        public ActionResult CancelOrder(FormCollection form)
        {
            if (_sslCommerzPaymentSettings.ReturnFromSSLCommerzWithoutPaymentRedirectsToOrderDetailsPage)
            {
                var order = _orderService.SearchOrders(storeId: _storeContext.CurrentStore.Id,
                    customerId: _workContext.CurrentCustomer.Id, pageSize: 1)
                    .FirstOrDefault();
                if (order != null)
                {
                    return RedirectToRoute("OrderDetails", new { orderId = order.Id });
                }
            }

            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}

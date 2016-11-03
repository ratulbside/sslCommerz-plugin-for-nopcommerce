using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.SSLCommerz.Domain
{
    public class CardTypes
    {
        /// <summary>
        /// SSLCommerz supported cards
        /// </summary>
        private readonly string[] _cardNames =
        {
            "VISA - BRAC Bank",
            "VISA - Dutch-Bangla Bank",
            "VISA - City Bank",
            "VISA - EBL",
            "Master Card - BRAC Bank",
            "Master Card - Dutch-Bangla Bank",
            "Master Card - City Bank",
            "Master Card - EBL",
            "AMEX - City Bank",
            "Q-Cash",
            "Dutch-Bangla Bank Nexus",
            "Bank Asia IB",
            "AB Bank IB",
            "Islami Bank Bangladesh IB and Mobile Banking",
            "Mutual Trust Bank IB",
            "bKash",
            "Dutch-Bangla Bank Mobile Banking",
            "City Touch IB"
        };

        #region Properties

        /// <summary>
        /// SSLCommerz cards string names
        /// </summary>
        public string[] CardNames
        {
            get { return _cardNames; }
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets the text name based on the CardId
        /// </summary>
        /// <param name="cardId">Id of the card - from SSLCommerz</param>
        /// <returns></returns>
        public static string GetCardName(string cardId)
        {
            switch (cardId)
            {
                case "brac_visa":
                    return "VISA - BRAC Bank";
                case "dbbl_visa":
                    return "VISA - Dutch-Bangla Bank";
                case "city_visa":
                    return "VISA - City Bank";
                case "ebl_visa":
                    return "VISA - EBL";
                case "brac_master":
                    return "Master Card - BRAC Bank";
                case "dbbl_master":
                    return "Master Card - Dutch-Bangla Bank";
                case "city_master":
                    return "Master Card - City Bank";
                case "ebl_master":
                    return "Master Card - EBL";
                case "city_amex":
                    return "AMEX - City Bank";
                case "qcash":
                    return "Q-Cash";
                case "dbbl_nexus":
                    return "Dutch-Bangla Bank Nexus";
                case "bankasia":
                    return "Bank Asia IB";
                case "abbank":
                    return "AB Bank IB";
                case "ibbl":
                    return "Islami Bank Bangladesh IB and Mobile Banking";
                case "mtbl":
                    return "Mutual Trust Bank IB";
                case "bkash":
                    return "bKash";
                case "dbblmobilebanking":
                    return "Dutch-Bangla Bank Mobile Banking";
                case "city":
                    return "City Touch IB";
                default:
                    return "UNKNOWN";
            }
        }

        /// <summary>
        /// Gets the CardId based on the text name
        /// </summary>
        /// <param name="cardName">Name of the card(based on the text name returned from GetCardName())</param>
        /// <returns></returns>
        public static string GetCardId(string cardName)
        {
            switch (cardName)
            {
                case "VISA - BRAC Bank":
                    return "brac_visa";
                case "VISA - Dutch-Bangla Bank":
                    return "dbbl_visa";
                case "VISA - City Bank":
                    return "city_visa";
                case "VISA - EBL":
                    return "ebl_visa";
                case "Master Card - BRAC Bank":
                    return "brac_master";
                case "Master Card - Dutch-Bangla Bank":
                    return "dbbl_master";
                case "Master Card - City Bank":
                    return "city_master";
                case "Master Card - EBL":
                    return "ebl_master";
                case "AMEX - City Bank":
                    return "city_amex";
                case "Q-Cash":
                    return "qcash";
                case "Dutch-Bangla Bank Nexus":
                    return "dbbl_nexus";
                case "Bank Asia IB":
                    return "bankasia";
                case "AB Bank IB":
                    return "abbank";
                case "Islami Bank Bangladesh IB and Mobile Banking":
                    return "ibbl";
                case "Mutual Trust Bank IB":
                    return "mtbl";
                case "bKash":
                    return "bkash";
                case "Dutch-Bangla Bank Mobile Banking":
                    return "dbblmobilebanking";
                case "City Touch IB":
                    return "city";
                default:
                    return "";
            }
        }


        #endregion
    }
}

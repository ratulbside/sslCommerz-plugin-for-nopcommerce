using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.SSLCommerz
{
    /// <summary>
    /// Represents SSLCommerz Helper
    /// Credit: http://www.nopcommerce.com/p/1757/g2apay.aspx
    /// </summary>
    public class SSLCommerzHelper
    {
        public static string HttpPost(string URI, List<KeyValuePair<string, string>> Parameters)
        {
            var result = string.Empty;
            using (var client = new HttpClient())
            {
                Task<HttpResponseMessage> task = client.PostAsync(URI, new FormUrlEncodedContent(Parameters));
                task.Wait(3000);

                if (!task.Result.IsSuccessStatusCode)
                    throw new Exception("Error getting response from the server");

                Task<string> task2 = task.Result.Content.ReadAsStringAsync();
                task2.Wait(3000);
                result = task2.Result;
                task.Dispose();
                task2.Dispose();
            }

            return result;
        }

        public static string HttpGet(string URI)
        {
            var result = string.Empty;

            using (var client = new HttpClient())
            {
                //client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", auth_header);
                Task<HttpResponseMessage> task = client.GetAsync(URI);
                task.Wait(3000);

                if (!task.Result.IsSuccessStatusCode)
                    throw new System.Exception("Error getting response from the server");

                Task<string> task2 = task.Result.Content.ReadAsStringAsync();
                task2.Wait(3000);
                result = task2.Result;
                task.Dispose();
                task2.Dispose();
            }

            return result;
        }
    }
}

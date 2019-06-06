using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace ShopProject
{
    public class ViVietAPI
    {
        public static string SECURE_SECRET = "ZL189887215164448888";
        public static string IPG_URL = "https://sandbox.viviet.vn/vipay/ecommerce";
        public static string return_url = "http://localhost:61765";//
        public static string cancel_url = "http://localhost:61765";//
        public static string locale = "vn";
        public static string currency = "VND";

        public static string version = "1.0";//
        public static string merchant_site = "1308";//
        public static string access_code = "OE1616470399";//
        public string merch_txn_ref;//
        public string order_no;//
        public static string order_desc = "Mô tả đơn hàng";//

        public string Merch_txn_ref { get => "REF_" + (new Random()).Next(1000000); }
        public string Order_no { get => "ORDER_" + (new Random()).Next(1000000); }

        public static String computeHash(String message, String algo)
        {
            byte[] sourceBytes = Encoding.UTF8.GetBytes(message);
            byte[] hashBytes = null;

            switch (algo.Trim().ToUpper())
            {
                case "MD5":
                    hashBytes = MD5CryptoServiceProvider.Create().ComputeHash(sourceBytes);
                    break;
                case "SHA1":
                    hashBytes = SHA1Managed.Create().ComputeHash(sourceBytes);
                    break;
                case "SHA256":
                    hashBytes = SHA256Managed.Create().ComputeHash(sourceBytes);
                    break;
                case "SHA384":
                    hashBytes = SHA384Managed.Create().ComputeHash(sourceBytes);
                    break;
                case "SHA512":
                    hashBytes = SHA512Managed.Create().ComputeHash(sourceBytes);
                    break;
                default:
                    break;
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; hashBytes != null && i < hashBytes.Length; i++)
            {
                sb.AppendFormat("{0:x2}", hashBytes[i]);
            }
            return sb.ToString();
        }

        /**
	     * Tạo chuỗi String có value ngăn cách nhau bằng '|' và sắp xếp theo thứ
	     * Thực hiện tạo mã Hash để check tính toàn vẹn của dữ liệu
	    */
        public static String getSecureHash(Dictionary<string, string> parameters)
        {
            List<string> keyList = new List<string>(parameters.Keys);
            // Sắp xếp thứ tự tham số alphabets
            keyList.Sort();
            StringBuilder valueFormated = new StringBuilder();
            foreach (string key in keyList)
            {
                valueFormated.Append(parameters[key]);
                valueFormated.Append("|");
            }
            //security_hash = HEX(SHA256(security_secret + “|” + VerifyString))
            String secureHash = computeHash(SECURE_SECRET
                    + "|"
                    + valueFormated.ToString().Substring(0,
                            valueFormated.Length - 1), "SHA256");
            return secureHash;
        }
        /**
	     * Hàm thực hiện tạo URL request yêu cầu thanh toán trên Ví Việt
	     * 
	     * @param parameters
	     *            tham số request
	     * @return
	    */
        public static String createRedirectUrl(Dictionary<string, string> parameter)
        {
            StringBuilder redirectUrl = new StringBuilder(IPG_URL);
            Boolean firstEntry = true;
            foreach (string key in parameter.Keys)
            {
                string value = parameter[key];
                if (value == null || value.Trim().Length == 0)
                {
                    continue;
                }
                if (firstEntry)
                {
                    redirectUrl.Append("?");
                    firstEntry = false;
                }
                else
                {
                    redirectUrl.Append("&");
                }
                redirectUrl.Append(String.Format("{0}={1}", key, value));
            }
            return redirectUrl.ToString();
        }
    }
}
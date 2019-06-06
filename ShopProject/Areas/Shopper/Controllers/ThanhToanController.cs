using Newtonsoft.Json.Linq;
using ShopProject.Areas.Shopper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;

namespace ShopProject.Areas.Shopper.Controllers
{
    public class ThanhToanController : Controller
    {
        UserContext db = new UserContext();
        // GET: Shopper/ThanhToan
        public ActionResult Index()
        {
            List<CartItem> giohang = Session["giohang"] as List<CartItem>;
            return View(giohang);
        }

        [HttpPost]
        public ActionResult StepEnd()
        {
            //Nhận reqest từ trang index
            string phone = Request.Form["phone"];
            string fullname = Request.Form["fullname"];
            string email = Request.Form["email"];
            string address = Request.Form["address"];
            string note = Request.Form["note"];
            //kiểm tra xem có customer chưa và cập nhật lại
            Customer newCus = new Customer();
            var cus = db.Customers.FirstOrDefault(p => p.cusPhone.Equals(phone));
            if (cus != null)
            {
                //nếu có số điện thoại trong db rồi
                //cập nhật thông tin và lưu
                cus.cusFullName = fullname;
                cus.cusEmail = email;
                cus.cusAddress = address;
                db.Entry(cus).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
            }
            else
            {
                //nếu chưa có sđt trong db
                //thêm thông tin và lưu
                newCus.cusPhone = phone;
                newCus.cusFullName = fullname;
                newCus.cusEmail = email;
                newCus.cusAddress = address;
                db.Customers.Add(newCus);
                db.SaveChanges();
            }
            //Thêm thông tin vào order và orderdetail
            List<CartItem> giohang = Session["giohang"] as List<CartItem>;
            //thêm order mới
            Order newOrder = new Order();
            Random rd = new Random();
            string newIDOrder = (Int32.Parse(db.Orders.OrderBy(p => p.orderDateTime).FirstOrDefault().orderID.Replace("HD", "")) + 1).ToString();
            newOrder.orderID = "HD" + rd.Next(1000000);
            newOrder.cusPhone = phone;
            newOrder.orderMessage = note;
            newOrder.orderDateTime = DateTime.Now.ToString();
            newOrder.orderStatus = "0";
            db.Orders.Add(newOrder);
            db.SaveChanges();
            //thêm details
            for (int i = 0; i < giohang.Count; i++)
            {
                OrderDetail newOrdts = new OrderDetail();
                newOrdts.orderID = newOrder.orderID;
                newOrdts.proID = giohang.ElementAtOrDefault(i).SanPhamID;
                newOrdts.ordtsQuantity = giohang.ElementAtOrDefault(i).SoLuong;
                newOrdts.ordtsThanhTien = giohang.ElementAtOrDefault(i).ThanhTien.ToString();
                db.OrderDetails.Add(newOrdts);
                db.SaveChanges();
            }
            Session["MHD"] = "HD"+newIDOrder;
            Session["Phone"] = phone;
            //lấy tổng số tiền cần thanh toán
            int total = 0;
            foreach (CartItem cartItem in giohang)
            {
                total += cartItem.ThanhTien;
            }
            //xoá sạch giỏ hàng
            giohang.Clear();

            string payment_method = Request.Form["pay"];
            if (payment_method == "NL" || payment_method == "ATM_ONLINE")
            {
                //ngân lượng
                RequestInfo info = new RequestInfo();
                info.Merchant_id = NganLuongAPI.merchant_id;
                info.Merchant_password = NganLuongAPI.merchant_password;
                info.Receiver_email = NganLuongAPI.receiver_email;
                info.cur_code = NganLuongAPI.cur_code;
                info.bank_code = Request.Form["bankcode"];
                info.Order_code = newOrder.orderID;
                info.Total_amount = total.ToString();
                info.fee_shipping = "0";
                info.Discount_amount = "0";
                info.order_description = "order description here...";
                info.return_url = "http://localhost:61765";
                info.cancel_url = "http://localhost:61765";

                info.Buyer_fullname = Request.Form["fullname"];
                info.Buyer_email = Request.Form["email"];
                info.Buyer_mobile = Request.Form["phone"];

                NganLuongAPI objNLChecout = new NganLuongAPI();
                ResponseInfo result = objNLChecout.GetUrlCheckout(info, payment_method);

                if (result.Error_code == "00")
                {
                    Response.Redirect(result.Checkout_url);
                }
            }
            //
            // ví việt
            else if (payment_method == "VV")
            {
                Dictionary<string, string> fields = new Dictionary<string, string>();
                fields.Add("version", ViVietAPI.version);
                fields.Add("locale", ViVietAPI.locale);
                fields.Add("access_code", ViVietAPI.access_code);
                fields.Add("merch_txn_ref", (new ViVietAPI()).Merch_txn_ref);
                fields.Add("merchant_site", ViVietAPI.merchant_site);
                fields.Add("order_info", "order information here...");
                fields.Add("order_no", (new ViVietAPI()).Order_no);
                fields.Add("order_desc", "order description here...");
                fields.Add("shipping_fee", "0");
                fields.Add("tax_fee", "0");
                fields.Add("total_amount", total.ToString());
                fields.Add("currency", ViVietAPI.currency);
                fields.Add("return_url", ViVietAPI.return_url);
                fields.Add("cancel_url", ViVietAPI.cancel_url);

                fields.Add("secure_hash", ViVietAPI.getSecureHash(fields));
                // Create a redirection URL
                string redirectUrl = ViVietAPI.createRedirectUrl(fields);
                Console.WriteLine(redirectUrl);
                Response.Redirect(redirectUrl);
            }
            //
            //Momo
            else if (payment_method == "MM")
            {
                //before sign HMAC SHA256 signature
                string rawHash = "partnerCode=" +
                    MoMoAPI.partnerCode + "&accessKey=" +
                    MoMoAPI.accessKey + "&requestId=" +
                    "123421232144" + "&amount=" +
                    total.ToString() + "&orderId=" +
                    newOrder.orderID + "&orderInfo=" +
                    "order infomation here..." + "&returnUrl=" +
                    MoMoAPI.returnUrl + "&notifyUrl=" +
                    MoMoAPI.notifyUrl + "&extraData=" +
                    "";

                //sign signature SHA256
                string signature = MoMoAPI.signSHA256(rawHash, MoMoAPI.secretKey);

                //build body json request
                JObject message = new JObject
            {
                { "partnerCode", MoMoAPI.partnerCode },
                { "accessKey", MoMoAPI.accessKey },
                { "requestId", "123421232144" },
                { "amount", total.ToString() },
                { "orderId", newOrder.orderID },
                { "orderInfo", "order infomation here..." },
                { "returnUrl", MoMoAPI.returnUrl },
                { "notifyUrl", MoMoAPI.notifyUrl },
                { "requestType", "captureMoMoWallet" },
                { "signature", signature }

            };
                string responseFromMomo = MoMoAPI.sendPaymentRequest(MoMoAPI.endpoint, message.ToString());
                JObject jmessage = JObject.Parse(responseFromMomo);
                try
                {
                    Response.Redirect(jmessage.GetValue("payUrl").ToString());
                }
                catch
                {
                    string fail = "Return from MoMo: " + jmessage.ToString();
                }

            }
            //chuyển sang trang thanh toán
            return RedirectToAction("HoaDon", "ThanhToan");
        }

        public ActionResult HoaDon()
        {
            return View();
        }
    }
}
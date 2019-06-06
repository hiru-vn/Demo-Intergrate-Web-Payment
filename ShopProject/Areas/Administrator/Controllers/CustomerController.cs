using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ShopProject.Areas.Administrator.Controllers
{
    public class CustomerController : Controller
    {
        Models.AdminContext dbCus = new Models.AdminContext();
        //
        // GET: /Administrator/Category/
        [HandleError]
        public ActionResult Index(string name, string error)
        {
            if (Session["accname"] == null)
            {
                Session["accname"] = null;
                return RedirectToAction("Login", "Account");
            }
            else
            {
                ViewBag.CusError = error;
                var model = dbCus.Customers.ToList();
                if (!string.IsNullOrEmpty(name))
                {
                    model = model.Where(p => p.cusPhone.Contains(name)).ToList();
                }
                return View(model);
            }
        }
    }
}
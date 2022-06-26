using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebBanlaptop.Models;

namespace WebBanlaptop.Controllers
{
    public class HomeController : Controller
    {
        QLBANLAPTOPEntities db = new QLBANLAPTOPEntities();
        public ActionResult Index()
        {
            var lstSPNB = db.SANPHAM.Where(n => n.SPNOIBAT == true);
            ViewBag.lstSPNB = lstSPNB;
            var lstSPM = db.SANPHAM.Where(n => n.SANPHAMMOI == true);
            ViewBag.lstSPM = lstSPM;
            return View();
        }
        public ActionResult navbarPartial()
        {
            var lstSP = db.SANPHAM;
            return PartialView(lstSP);
        }
        public ActionResult showUser()
        {
            return PartialView();
        }
        #region Tìm kiếm sản phẩm
        [HttpPost]
        public ActionResult KQTimKiem(string TuKhoa)
        {
            var listSP = db.SANPHAM.Where(n => n.TENSP.Contains(TuKhoa));
            int i = listSP.Count();
            if (i == 0)
            {
                return HttpNotFound();
            }
            ViewBag.lstSP = listSP.OrderBy(n => n.TENSP).ToList();
            return View();
        }
        #endregion


        public ActionResult GioiThieu()
        {
            return View();
        }

        public ActionResult TinTuc()
        {
            var listTintuc = new PostTT().ListAllTinTuc();
            return View(listTintuc);
        }
        public ActionResult NoiDung(long id)
        {
            var tt = new PostTT().ViewDetail(id);
            return View(tt);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebBanlaptop.Models;

namespace WebBanlaptop.Controllers
{
    public class SanPhamController : Controller
    {
        QLBANLAPTOPEntities db = new QLBANLAPTOPEntities();
        // GET: SanPham
        public ActionResult danhMucSP()
        {
            var lstLSP = db.SANPHAM;
            return PartialView(lstLSP);
        }
        public ActionResult showSPNoiBat()
        {
            
            return PartialView();
        }
        public ActionResult showSPPartial()
        {
            return PartialView();
        }
        public ActionResult showmauSP()
        {
            var nsx = db.NHASANXUAT.ToList();
            return PartialView(nsx);
        }
        public ActionResult showAllSP()
        {
            var lstSP = db.SANPHAM;
            if (lstSP.Count() == 0)
            {
                return HttpNotFound();
            }
            ViewBag.lstSP = lstSP;
            return View();
        }
        public ActionResult showSPTheoNSX(int? MaLSP, int? MaNXS)
        {
            if (MaLSP == null || MaNXS == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var lstSP = db.SANPHAM.Where(n => n.MALSP == MaLSP && n.MANSX == MaNXS);
            if (lstSP.Count() == 0)
            {
                return HttpNotFound();
            }
            ViewBag.lstSP = lstSP;
            return View();
        }
        public ActionResult showSPTheoLoai(int? MaLSP)
        {
            if (MaLSP == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var lstSP = db.SANPHAM.Where(n => n.MALSP == MaLSP);
            if (lstSP.Count() == 0)
            {
                return HttpNotFound();
            }
            ViewBag.lstSP = lstSP;
            return View();
        }
        public ActionResult SapXep(string LoaiSX,List<SANPHAM> LoaiSP)
        {
            if (LoaiSX.Equals("LowHigh"))
            {
                var lstSP = LoaiSP.OrderBy(n => n.DONGIA);
                return View(lstSP);
            }else if (LoaiSX.Equals("HighLow"))
            {
                var lstSP = LoaiSP.OrderByDescending(n => n.DONGIA);
                return View(lstSP);
            }
            return View();
        }
        public ActionResult showSPTheoNSX1(int? MaLSP)
        {
            if (MaLSP == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var lstSP = db.SANPHAM.Where(n => n.MANSX == MaLSP);
            if (lstSP.Count() == 0)
            {
                return HttpNotFound();
            }
            ViewBag.lstSP = lstSP;
            return View("showSPTheoNSX", lstSP);
        }
        public ActionResult chiTietSP(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SANPHAM sp = db.SANPHAM.SingleOrDefault(n => n.MASP == id && n.TRANGTHAI == true);
            var SPCungLoai = db.SANPHAM.Where(n=>n.MALSP == sp.MALSP).OrderByDescending(n=>n.DONGIA).Take(10);
            ViewBag.SPCungLoai = SPCungLoai;
            var NSX = db.NHASANXUAT.FirstOrDefault(p => p.MANSX == sp.MANSX);
            var maMau = db.CHITIETSP.Where(p => p.MASP == id).Select(p => p.MAMAU).ToList();
            var soLuongTon = db.CHITIETSP.Where(p => p.MASP == id).Select(p => p.SOLUONGTON).ToList();
            var tenMau = db.MAUSAC.Select(p => p.TENMAU).ToList();
            var demSanPham = soLuongTon.Sum(p => p.Value); // đếm list số lượng tồn của các size
            var tenLSP= db.LOAISANPHAM.FirstOrDefault(n=>n.TENLOAISP== sp.tenLSP);
            if (NSX != null)
            {
                sp.maNSX = NSX.MANSX;
                sp.tenNSX = NSX.TENNSX;
                sp.maMau = maMau;
                sp.soluongton = soLuongTon;
                sp.mauSP = tenMau;
            }
            if (demSanPham == 0) sp.tinhTrangSanPham = false;
            else sp.tinhTrangSanPham = true;
            if (sp == null)
            {
                return HttpNotFound();
            }
            return View(sp);
        }



    }
}
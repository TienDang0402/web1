using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using WebBanlaptop.Models;
using PagedList;
using PagedList.Mvc;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.IO;
using System.Web;

namespace WebBanlaptop.Controllers
{
    public class AdminController : Controller
    {

        public ActionResult index()
        {
            if (!checkLogin()) return RedirectToAction("Login"); // session null thì bắt đăng nhập
            return View();
        }
        private bool checkLogin() // check đã có người dung đăng nhập chưa nếu chưa thì return false
        {
            NHANVIEN admin = Session["Admin"] as NHANVIEN;
            if (admin == null)
            {
                Session["Admin"] = null;
                return false;
            }
            return true;
        }
        public static string MD5Hash(string input)
        {
            StringBuilder hash = new StringBuilder();
            MD5CryptoServiceProvider md5provider = new MD5CryptoServiceProvider();
            byte[] bytes = md5provider.ComputeHash(new UTF8Encoding().GetBytes(input));

            for (int i = 0; i < bytes.Length; i++)
            {
                hash.Append(bytes[i].ToString("x2"));
            }
            return hash.ToString();
        }

        #region đăng nhập, đăng xuất
        [HttpGet]
        public ActionResult Login()
        {
            if (checkLogin()) return RedirectToAction("index"); // nếu có lưu session đăng nhập thì vào luôn web
            return View();
        }
        [HttpPost]
        public ActionResult Login(FormCollection f)
        {
            QLBANLAPTOPEntities db = new QLBANLAPTOPEntities();
            var username = f["username"];
            var password = f["password"];
            var admin = db.NHANVIEN.SingleOrDefault(p => p.USERNAME == username);
            if (String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password))
            {
                ViewData["Error"] = "Vui Lòng Điền Đầy Đủ Nội Dung";
                return this.Login();
            }
            else if (admin == null)
            {
                ViewData["Error"] = "Sai Tài Khoản";
                return this.Login();
            }
            else if (!String.Equals(MD5Hash(password), admin.PASSWORD))
            {
                ViewData["Error"] = "Sai Mật Khẩu";
                return this.Login();
            }
            else if (!admin.TRANGTHAI)
            {
                ViewData["Error"] = "Tài khoản Admin này đang bị khóa. Vui lòng liên hệ chủ shop.";
                return this.Login();
            }
            else
            {
                Session["Admin"] = admin;
                return RedirectToAction("index", "Admin");
            }
        }
        public ActionResult LogOut()
        {
            Session["Admin"] = null;
            return RedirectToAction("Login");
        }
        #endregion

        #region phần admins
        public ActionResult ListAdmin(int? page)
        {
            QLBANLAPTOPEntities db = new QLBANLAPTOPEntities();
            if (!checkLogin()) return RedirectToAction("Login");
            List<NHANVIEN> listAdmin = db.NHANVIEN.ToList();
            ViewBag.lstnv = listAdmin;
            return View();
        }
        [HttpGet]
        public ActionResult addAdmin()
        {
            if (!checkLogin()) return RedirectToAction("Login");
            return View();
        }
        [HttpPost]
        public ActionResult addAdmin(NHANVIEN nv)
        {
            QLBANLAPTOPEntities db = new QLBANLAPTOPEntities();
            var checkNV = db.NHANVIEN.FirstOrDefault(n => n.USERNAME == nv.USERNAME);
            if (checkNV != null)
            {
                ViewData["Error"] = "Tài khoản admin đã tồn tại";
                return this.addAdmin();
            }
            else
            {
                try
                {
                    nv.PASSWORD = MD5Hash(nv.PASSWORD);
                    nv.TRANGTHAI = true;
                    nv.BIGBOSS = false;
                    db.NHANVIEN.Add(nv);
                    db.SaveChanges();
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                {
                    Exception raise = dbEx;
                    foreach (var validationErrors in dbEx.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            string message = string.Format("{0}:{1}",
                                validationErrors.Entry.Entity.ToString(),
                                validationError.ErrorMessage);
                            // raise a new exception nesting  
                            // the current instance as InnerException  
                            raise = new InvalidOperationException(message, raise);
                        }
                    }
                    throw raise;
                }

            }
            return RedirectToAction("ListAdmin", "Admin");
        }
        public JsonResult LockOrUnlockAdmin(string id)
        {
            bool lockOrUnlock = true; // khóa hay mở khóa tài khoản (true: khóa, false: mở khóa)
            QLBANLAPTOPEntities db = new QLBANLAPTOPEntities();
            var admin = db.NHANVIEN.Single(p => p.USERNAME.Equals(id));
            bool Status = false;
            string err = string.Empty;
            if (admin != null)
            {
                if ((bool)admin.BIGBOSS) // check chủ sỡ hữu
                    return Json(new
                    {
                        status = false,
                        exit = "Không thể khóa chủ sở hữu"
                    });
                if (admin.TRANGTHAI) admin.TRANGTHAI = false; // nếu đang hoạt động thì khóa
                else
                {
                    admin.TRANGTHAI = true;
                    lockOrUnlock = false; // mở khóa tài khoản
                }

                try
                {
                    db.SaveChanges();
                    Status = true;
                }
                catch (Exception ex)
                {
                    err = ex.Message;
                }
            }

            return Json(new
            {
                status = Status,
                errorMessage = err,
                lockStatus = lockOrUnlock
            });

        }
        [HttpGet]
        public ActionResult changePWAdmin(int id, bool type)
        {
            if (!checkLogin()) return RedirectToAction("Login");
            QLBANLAPTOPEntities db = new QLBANLAPTOPEntities();
            var checkNV = db.NHANVIEN.FirstOrDefault(n => n.MANV == id);
            if (type)
            {
                ViewBag.type = true;
                return View(checkNV);
            }
            else
            {
                ViewBag.type = false;
                return View(checkNV);
            }
        }
        [HttpPost]
        public ActionResult changePWAdmin(NHANVIEN nv, bool type, FormCollection f, int id)
        {
            QLBANLAPTOPEntities db = new QLBANLAPTOPEntities();
            var checkNV = db.NHANVIEN.FirstOrDefault(n => n.MANV == id);
            if (type)
            {
                try
                {
                    checkNV.PASSWORD = MD5Hash(nv.PASSWORD);
                    db.NHANVIEN.AddOrUpdate(checkNV);
                    db.SaveChanges();
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                {
                    Exception raise = dbEx;
                    foreach (var validationErrors in dbEx.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            string message = string.Format("{0}:{1}",
                                validationErrors.Entry.Entity.ToString(),
                                validationError.ErrorMessage);
                            // raise a new exception nesting  
                            // the current instance as InnerException  
                            raise = new InvalidOperationException(message, raise);
                        }
                    }
                    throw raise;
                }
            }
            else
            {
                var xnmk = f["XNPASSWORD"].ToString();
                if (!nv.PASSWORD.Equals(xnmk))
                {
                    ViewData["Error"] = "Tài khoản admin đã tồn tại";
                    return this.changePWAdmin(nv.MANV, type);
                }
                else
                {
                    try
                    {
                        checkNV.PASSWORD = MD5Hash(nv.PASSWORD);
                        db.NHANVIEN.AddOrUpdate(checkNV);
                        db.SaveChanges();
                    }
                    catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                    {
                        Exception raise = dbEx;
                        foreach (var validationErrors in dbEx.EntityValidationErrors)
                        {
                            foreach (var validationError in validationErrors.ValidationErrors)
                            {
                                string message = string.Format("{0}:{1}",
                                validationErrors.Entry.Entity.ToString(),
                                validationError.ErrorMessage);
                                // raise a new exception nesting  
                                // the current instance as InnerException  
                                raise = new InvalidOperationException(message, raise);
                            }
                        }
                        throw raise;
                    }
                }

            }

            return RedirectToAction("ListAdmin", "Admin");
        }
        #endregion

        #region phần users

        #endregion


        #region Quản lý tin tức
        //quản lý tin tức
        public ActionResult ListTinTuc()
        {
            QLBANLAPTOPEntities db = new QLBANLAPTOPEntities();
            if (!checkLogin()) return RedirectToAction("Login");
            List<TINTUC> listTinTuc = db.TINTUC.ToList();
            ViewBag.lstTT = listTinTuc;
            return View();
        }
        [HttpGet]
        public ActionResult ThemTinTuc()
        {
            if (!checkLogin()) return RedirectToAction("Login");
            return View();
        }
        [ValidateInput(false)]
        [HttpPost]
        public ActionResult ThemTinTuc(TINTUC tt, HttpPostedFileBase HINHTT)
        {
            QLBANLAPTOPEntities db = new QLBANLAPTOPEntities();
            //kiem tra hinh anh đa tôn tại trong csdl hay chưa
            if (HINHTT.ContentLength > 0)
            {
                //lấy hinh ảnh tư bên ngoài vào
                var filename = Path.GetFileName(HINHTT.FileName);
                //sau đo chuyển hinh ảnh đó vào thư mục trong solution
                var path = Path.Combine(Server.MapPath("~/Content/HinhTinTuc"), filename);
                //kiểm tra nếu hình anh đã có rồi thi xuất ra thông báo và ngược lại nêu chưa co thi thêm vào
                if (System.IO.File.Exists(path))
                {
                    ViewBag.upload = "Hình ảnh đã tồn tại rồi!!";
                    return View();
                }
                else
                {
                    HINHTT.SaveAs(path);
                    tt.HINHTT = filename;

                } 

            }
            db.TINTUC.Add(tt);
            db.SaveChanges();
            return RedirectToAction("ListTinTuc", "Admin");
        }

        public JsonResult LockOrUnlockTT(int? id)
        {
            bool lockOrUnlock = true; // khóa hay mở khóa tài khoản (true: khóa, false: mở khóa)
            QLBANLAPTOPEntities db = new QLBANLAPTOPEntities();
            var tt = db.TINTUC.SingleOrDefault(p => p.MATT == id);
            bool Status = false;
            string err = string.Empty;
            if (tt != null)
            {

                if (tt.TRANGTHAI) tt.TRANGTHAI = false; // nếu đang hoạt động thì khóa
                else
                {
                    tt.TRANGTHAI = true;
                    lockOrUnlock = false; // mở khóa tài khoản
                }

                try
                {
                    db.SaveChanges();
                    Status = true;
                }
                catch (Exception ex)
                {
                    err = ex.Message;
                }
            }

            return Json(new
            {
                status = Status,
                errorMessage = err,
                lockStatus = lockOrUnlock
            });



        }

        [HttpGet]
        public ActionResult SuaTT(int ?id)
        {
            if (!checkLogin()) return RedirectToAction("Login");
            QLBANLAPTOPEntities db = new QLBANLAPTOPEntities();
            var tt = db.TINTUC.SingleOrDefault(n => n.MATT == id);
            return View(tt);
        }
     
        #endregion



        #region Quản lý sản phẩm
        // quan ly san phaam

        public ActionResult ListSanPham()
        {
            QLBANLAPTOPEntities db = new QLBANLAPTOPEntities();
            if (!checkLogin()) return RedirectToAction("Login");
            List<SANPHAM> listSanPham = db.SANPHAM.ToList();
            foreach(var item in listSanPham)
            {
                var NSX = db.NHASANXUAT.FirstOrDefault(p => p.MANSX == item.MANSX);
                if (NSX != null)
                {
                    item.tenNSX = NSX.TENNSX;
                }
            }
            foreach (var item in listSanPham)
            {
                var NSX = db.LOAISANPHAM.FirstOrDefault(p => p.MALSP == item.MALSP);
                if (NSX != null)
                {
                    item.tenLSP = NSX.TENLOAISP;
                }
            }
            

            ViewBag.lstSP = listSanPham;
            return View();
        }
        public ActionResult ChiTietSanPham(int? id)
        {
            QLBANLAPTOPEntities db = new QLBANLAPTOPEntities();
            if (!checkLogin()) return RedirectToAction("Login");
            List<CHITIETSP> listChiTietSanPham = db.CHITIETSP.ToList();
            var sp = db.SANPHAM.FirstOrDefault(n => n.MASP == id);
            var ctsp = db.CHITIETSP.Where(n => n.MASP == sp.MASP);
            foreach(var item in ctsp)
            {
                var mausac = db.MAUSAC.FirstOrDefault(p => p.MAMAU == item.MAMAU);
                if (mausac != null)
                {
                    item.tenMau = mausac.TENMAU;
                }

            }
            foreach (var item in ctsp)
            {
                var tensp = db.SANPHAM.FirstOrDefault(p => p.MASP == item.MASP);
                if (tensp != null)
                {
                    item.tenSP = tensp.TENSP;
                }

            }


            ViewBag.lstCTSP = ctsp;
            return View();
        }
        //them moi san pham
        [HttpGet]
        public ActionResult ThemMoiSP()
        {
            QLBANLAPTOPEntities db = new QLBANLAPTOPEntities();
            ViewBag.MANSX = new SelectList(db.NHASANXUAT.OrderBy(n => n.MANSX), "MANSX", "TENNSX");
            ViewBag.MALSP = new SelectList(db.LOAISANPHAM.OrderBy(n => n.MALSP), "MALSP", "TENLOAISP");
            ViewBag.MAMAU = new SelectList(db.MAUSAC.OrderBy(n => n.MAMAU), "MAMAU", "TENMAU");
            return View();
        }
        [ValidateInput(false)]
        [HttpPost]
        public ActionResult ThemMoiSP(SANPHAM sp ,HttpPostedFileBase HINHANH, MAUSAC ms, FormCollection f)
        {
            QLBANLAPTOPEntities db = new QLBANLAPTOPEntities();
            ViewBag.MANSX = new SelectList(db.NHASANXUAT.OrderBy(n => n.MANSX), "MANSX", "TENNSX");
            ViewBag.MALSP = new SelectList(db.LOAISANPHAM.OrderBy(n => n.MALSP), "MALSP", "TENLOAISP");
            ViewBag.MAMAU = new SelectList(db.MAUSAC.OrderBy(n => n.MAMAU), "MAMAU", "TENMAU");
            //kiem tra hinh anh đa tôn tại trong csdl hay chưa
            if (HINHANH.ContentLength > 0)
            {
                //lấy hinh ảnh tư bên ngoài vào
                var filename = Path.GetFileName(HINHANH.FileName);
                //sau đo chuyển hinh ảnh đó vào thư mục trong solution
                var path = Path.Combine(Server.MapPath("~/Content/HinhAnhSP"), filename);
                //kiểm tra nếu hình anh đã có rồi thi xuất ra thông báo và ngược lại nêu chưa co thi thêm vào
                if (System.IO.File.Exists(path))
                {
                    ViewBag.upload = "Hình ảnh đã tồn tại rồi!!";
                    return View();
                }
                else
                {
                    HINHANH.SaveAs(path);
                    sp.HINHANH = filename;
                    
                }
                
            }
            db.SANPHAM.Add(sp);
            db.SaveChanges();
            
            CHITIETSP ctsp = new CHITIETSP();
            ctsp.MASP=sp.MASP;
            ctsp.MAMAU = ms.MAMAU;
            ctsp.SOLUONGTON = int.Parse(f["SOLUONGTON"].ToString());
            db.CHITIETSP.Add(ctsp);
            db.SaveChanges();
            return RedirectToAction("ListSanPham");
        }
        public JsonResult LockOrUnlockSP(int? id)
        {
            bool lockOrUnlock = true; // khóa hay mở khóa tài khoản (true: khóa, false: mở khóa)
            QLBANLAPTOPEntities db = new QLBANLAPTOPEntities();
            var sp = db.SANPHAM.Single(p => p.MASP==id);
            bool Status = false;
            string err = string.Empty;
            if (sp != null)
            {

                if (sp.TRANGTHAI) sp.TRANGTHAI = false; // nếu đang hoạt động thì khóa
                else
                {
                    sp.TRANGTHAI = true;
                    lockOrUnlock = false; // mở khóa tài khoản
                }

                try
                {
                    db.SaveChanges();
                    Status = true;
                }
                catch (Exception ex)
                {
                    err = ex.Message;
                }
            }

            return Json(new
            {
                status = Status,
                errorMessage = err,
                lockStatus = lockOrUnlock
            });



        }

        // sua san pham

        [HttpGet]
        public ActionResult SuaSP(int? id)
        {
            if (!checkLogin()) return RedirectToAction("Login");

            QLBANLAPTOPEntities db = new QLBANLAPTOPEntities();

            var sp = db.SANPHAM.FirstOrDefault(n => n.MASP == id);
            SANPHAM sptemp = new SANPHAM();
            ViewBag.MANSX = new SelectList(db.NHASANXUAT.OrderBy(n => n.MANSX), "MANSX", "TENNSX");
            ViewBag.MALSP = new SelectList(db.LOAISANPHAM.OrderBy(n => n.MALSP), "MALSP", "TENLOAISP");
            if (sp != null)
            {

                sptemp = sp;
            }
            return View(sptemp);





        }
        [HttpPost]
        public ActionResult SuaSP(SANPHAM sp, HttpPostedFileBase HINHANH,int? id)
        {
            QLBANLAPTOPEntities db = new QLBANLAPTOPEntities();

            ViewBag.MANSX = new SelectList(db.NHASANXUAT.OrderBy(n => n.MANSX), "MANSX", "TENNSX");
            ViewBag.MALSP = new SelectList(db.LOAISANPHAM.OrderBy(n => n.MALSP), "MALSP", "TENLOAISP");

            //kiem tra hinh anh đa tôn tại trong csdl hay chưa
            if (HINHANH.ContentLength > 0)
            {
                //lấy hinh ảnh tư bên ngoài vào
                var filename = Path.GetFileName(HINHANH.FileName);
                //sau đo chuyển hinh ảnh đó vào thư mục trong solution
                var path = Path.Combine(Server.MapPath("~/Content/HinhAnhSP"), filename);
                //kiểm tra nếu hình anh đã có rồi thi xuất ra thông báo và ngược lại nêu chưa co thi thêm vào
                if (System.IO.File.Exists(path))
                {
                    ViewBag.upload = "Hình ảnh đã tồn tại rồi!!";
                    return View();
                }
                else
                {
                    HINHANH.SaveAs(path);
                    sp.HINHANH = filename;

                }

            }
            db.SANPHAM.AddOrUpdate(sp);
            db.SaveChanges();
            return RedirectToAction("ListSanPham");
        }



        #endregion

        #region Quản lý đơn đặt hàng
        public ActionResult ListDonDatHang()
        {
            QLBANLAPTOPEntities db = new QLBANLAPTOPEntities();
            if (!checkLogin()) return RedirectToAction("Login");
            List<HOADON> listDonDatHang = db.HOADON.ToList();
            foreach (var item in listDonDatHang)
            {
                var KH = db.KHACHHANG.FirstOrDefault(p => p.MAKH == item.MAKH);
                if (KH != null)
                {
                    item.tenKH = KH.TENKH;
                }
            }
            ViewBag.lstDDH = listDonDatHang;
            return View();
        }

        public ActionResult ChiTietDonDatHang()
        {
            QLBANLAPTOPEntities db = new QLBANLAPTOPEntities();
            if (!checkLogin()) return RedirectToAction("Login");
            List<CHITIETHD> listChiTietHoaDon = db.CHITIETHD.ToList();
            foreach (var item in listChiTietHoaDon)
            {
                var sp = db.SANPHAM.FirstOrDefault(p => p.MASP == item.MASP);
                if (sp != null)
                {
                    item.tenSP = sp.TENSP;
                }
            }
            foreach (var item in listChiTietHoaDon)
            {
                var mau = db.MAUSAC.FirstOrDefault(p => p.MAMAU == item.MAMAU);
                if (mau != null)
                {
                    item.tenMau = mau.TENMAU;
                }
            }
            ViewBag.lstCTHD = listChiTietHoaDon;
            return View();
        }
        #endregion


    }
}







    



using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebBanlaptop.Models
{
    public class PostTT
    {
        QLBANLAPTOPEntities db = null;
        public PostTT()
        {
            db = new QLBANLAPTOPEntities();
        }
        public List<TINTUC> ListTinTuc(int top)
        {
            return db.TINTUC.OrderByDescending(x => x.MATT).Take(top).ToList();
        }
        public List<TINTUC> ListAllTinTuc()
        {
            return db.TINTUC.OrderByDescending(x => x.MATT).ToList();
        }
        public TINTUC ViewDetail(long id)
        {
            return db.TINTUC.Find(id);
        }
    }
}
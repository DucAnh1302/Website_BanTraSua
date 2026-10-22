using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QL_WebBanTraSua.Models;

namespace QL_WebBanTraSua.Controllers
{
    public class AdminController : Controller
    {
        db_trasuaEntities db = new db_trasuaEntities();
        // GET: Admin
        public ActionResult Index()
        {
            return View();
        }

        // quản lý sản phẩm
        public ActionResult QuanLySanPham()
        {
            List<product> dssp = db.products.ToList();
            return View(dssp);
        }
    }
}
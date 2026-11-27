using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using QL_WebBanTraSua.Models;

namespace QL_WebBanTraSua.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RoleAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string[] _allowedRoles;

        public RoleAuthorizeAttribute(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles ?? new string[0];
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var httpContext = filterContext.HttpContext;
            var session = httpContext.Session;

            if (session == null || session["UserId"] == null)
            {
                string returnUrl = httpContext.Request != null ? httpContext.Request.RawUrl : "";
                filterContext.Controller.TempData["Error"] = "Vui lòng đăng nhập để truy cập khu vực này.";
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new { controller = "Account", action = "DangNhap", returnUrl = returnUrl })
                );
                return;
            }

            long userId;
            if (!long.TryParse(session["UserId"].ToString(), out userId))
            {
                session.Clear();
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new { controller = "Account", action = "DangNhap" })
                );
                return;
            }

            using (var db = new db_trasuaEntities())
            {
                var currentUser = db.users.Find(userId);
                if (currentUser == null || !currentUser.status)
                {
                    session.Clear();
                    filterContext.Controller.TempData["Error"] = "Tài khoản của bạn đã bị khóa hoặc không còn hiệu lực.";
                    filterContext.Result = new RedirectToRouteResult(
                        new RouteValueDictionary(new { controller = "Account", action = "DangNhap" })
                    );
                    return;
                }

                // Dong bo lai session tu DB
                session["UserRole"] = currentUser.role;
                session["UserName"] = currentUser.name;
                session["UserEmail"] = currentUser.email;

                if (_allowedRoles.Length > 0)
                {
                    string userRole = (currentUser.role ?? "").Trim().ToLower();
                    bool hasAccess = _allowedRoles.Any(r => string.Equals(r, userRole, StringComparison.OrdinalIgnoreCase));
                    if (!hasAccess)
                    {
                        filterContext.Controller.TempData["Error"] = "Bạn không có quyền truy cập vào chức năng này.";
                        filterContext.Result = new RedirectToRouteResult(
                            new RouteValueDictionary(new { controller = "Account", action = "KhongCoQuyen" })
                        );
                        return;
                    }
                }
            }

            base.OnActionExecuting(filterContext);
        }
    }
}

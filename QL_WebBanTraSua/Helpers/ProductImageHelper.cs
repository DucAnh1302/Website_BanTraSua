using System;
using System.IO;
using System.Web;
using System.Web.Mvc;

namespace QL_WebBanTraSua.Helpers
{
    public static class ProductImageHelper
    {
        public static string Resolve(HttpContextBase httpContext, string image, string categoryName = null)
        {
            if (string.IsNullOrWhiteSpace(image))
            {
                return GetFallback(categoryName);
            }

            string trimmed = image.Trim();
            if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            string clean = trimmed.TrimStart('~', '/');
            var urlHelper = new UrlHelper(HttpContext.Current.Request.RequestContext);

            if (httpContext != null && httpContext.Server != null)
            {
                string contentCandidate = "~/Content/images/" + clean;
                if (File.Exists(httpContext.Server.MapPath(contentCandidate)))
                {
                    return urlHelper.Content(contentCandidate);
                }

                string assetCandidate = "~/assets/img/" + clean;
                if (File.Exists(httpContext.Server.MapPath(assetCandidate)))
                {
                    return urlHelper.Content(assetCandidate);
                }

                string bareName = Path.GetFileName(clean);
                if (!string.IsNullOrEmpty(bareName))
                {
                    string directProdCandidate = "~/Content/images/products/" + bareName;
                    if (File.Exists(httpContext.Server.MapPath(directProdCandidate)))
                    {
                        return urlHelper.Content(directProdCandidate);
                    }
                }
            }

            return GetFallback(categoryName);
        }

        private static string GetFallback(string categoryName)
        {
            if (!string.IsNullOrEmpty(categoryName))
            {
                if (categoryName.Contains("Cà Phê"))
                    return "https://images.unsplash.com/photo-1517701604599-bb29b565090c?q=80&w=400&auto=format&fit=crop";
                if (categoryName.Contains("Matcha"))
                    return "https://images.unsplash.com/photo-1515823662972-da6a2e4d3002?q=80&w=400&auto=format&fit=crop";
                if (categoryName.Contains("Trái Cây"))
                    return "https://images.unsplash.com/photo-1556679343-c7306c1976bc?q=80&w=400&auto=format&fit=crop";
                if (categoryName.Contains("Đá Xay"))
                    return "https://images.unsplash.com/photo-1572490122747-3968b75cc699?q=80&w=400&auto=format&fit=crop";
            }
            return "https://images.unsplash.com/photo-1541658016709-82535e94bc69?q=80&w=400&auto=format&fit=crop";
        }
    }
}

using System;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using QL_WebBanTraSua.Models;

namespace QL_WebBanTraSua.Helpers
{
    public static class InvoicePdfHelper
    {
        public static byte[] BuildOrderInvoicePdf(order ord)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4, 36f, 36f, 36f, 36f);
                PdfWriter.GetInstance(document, ms);
                document.Open();

                BaseFont bf;
                try
                {
                    string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                    if (!File.Exists(fontPath))
                    {
                        fontPath = @"C:\Windows\Fonts\arial.ttf";
                    }
                    bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                }
                catch
                {
                    bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.WINANSI, BaseFont.NOT_EMBEDDED);
                }

                Font titleFont = new Font(bf, 20, Font.BOLD, new BaseColor(24, 24, 27));
                Font subTitleFont = new Font(bf, 12, Font.ITALIC, new BaseColor(113, 113, 122));
                Font sectionTitleFont = new Font(bf, 13, Font.BOLD, new BaseColor(225, 29, 72));
                Font headerTableFont = new Font(bf, 10, Font.BOLD, BaseColor.WHITE);
                Font boldFont = new Font(bf, 10, Font.BOLD, new BaseColor(39, 39, 42));
                Font normalFont = new Font(bf, 10, Font.NORMAL, new BaseColor(63, 63, 70));
                Font smallFont = new Font(bf, 9, Font.NORMAL, new BaseColor(113, 113, 122));

                Paragraph title = new Paragraph("TIỆM TRÀ SỮA TEABLOOM", titleFont) { Alignment = Element.ALIGN_CENTER };
                document.Add(title);

                Paragraph slogan = new Paragraph("Đậm vị trà, Thơm vị sữa - 140 Lê Trọng Tấn, Q. Tân Phú, TP. HCM", subTitleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 15f
                };
                document.Add(slogan);

                string channel = ord.order_type == "in_store" ? "BÁN TẠI QUẦY (POS)" : "ĐẶT HÀNG TRỰC TUYẾN";
                Paragraph invoiceHeader = new Paragraph($"HÓA ĐƠN BÁN HÀNG #{ord.id} ({channel})", sectionTitleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20f
                };
                document.Add(invoiceHeader);

                PdfPTable infoTable = new PdfPTable(2) { WidthPercentage = 100, SpacingAfter = 15f };
                infoTable.SetWidths(new float[] { 50f, 50f });

                PdfPCell c1 = new PdfPCell { Border = Rectangle.NO_BORDER };
                c1.AddElement(new Paragraph($"Khách hàng: {ord.shipping_name ?? (ord.user != null ? ord.user.name : "Khách vãng lai")}", normalFont));
                c1.AddElement(new Paragraph($"Số điện thoại: {ord.shipping_phone ?? "N/A"}", normalFont));
                c1.AddElement(new Paragraph($"Địa chỉ / Kênh: {(string.IsNullOrWhiteSpace(ord.shipping_address) ? "Mua tại quầy" : ord.shipping_address)}", normalFont));
                infoTable.AddCell(c1);

                PdfPCell c2 = new PdfPCell { Border = Rectangle.NO_BORDER };
                c2.AddElement(new Paragraph($"Mã đơn: #{ord.id}", normalFont));
                c2.AddElement(new Paragraph($"Thời gian: {(ord.created_at.HasValue ? ord.created_at.Value.ToString("dd/MM/yyyy HH:mm") : DateTime.Now.ToString("dd/MM/yyyy HH:mm"))}", normalFont));
                string ptThanhToan = ord.payment_method == "transfer" ? "Chuyển khoản (VietQR)" : "Tiền mặt";
                string trangThaiTt = ord.payment_status == "paid" ? "Đã thanh toán" : "Chưa thanh toán";
                c2.AddElement(new Paragraph($"Thanh toán: {ptThanhToan} ({trangThaiTt})", normalFont));
                infoTable.AddCell(c2);

                document.Add(infoTable);

                PdfPTable itemTable = new PdfPTable(6) { WidthPercentage = 100, SpacingAfter = 15f };
                itemTable.SetWidths(new float[] { 8f, 35f, 12f, 15f, 12f, 18f });

                string[] headers = { "STT", "Tên thức uống", "Size", "Tùy chọn", "SL", "Thành tiền" };
                foreach (var h in headers)
                {
                    PdfPCell hCell = new PdfPCell(new Phrase(h, headerTableFont))
                    {
                        BackgroundColor = new BaseColor(30, 41, 59),
                        Padding = 6f,
                        HorizontalAlignment = Element.ALIGN_CENTER
                    };
                    itemTable.AddCell(hCell);
                }

                int stt = 1;
                foreach (var oi in ord.order_items)
                {
                    var variant = oi.product_variants;
                    string productName = variant != null && variant.product != null ? variant.product.name : "Thức uống";
                    string size = variant != null ? variant.size : "M";

                    string toppingsStr = "";
                    if (oi.order_item_toppings != null && oi.order_item_toppings.Count > 0)
                    {
                        toppingsStr = " + " + string.Join(", ", oi.order_item_toppings.Select(t => t.topping_name));
                    }
                    string optionsStr = $"Đường {oi.sugar_level}%, Đá {oi.ice_option}{toppingsStr}";

                    decimal itemToppingsTotal = oi.order_item_toppings != null ? oi.order_item_toppings.Sum(t => t.price) : 0;
                    decimal lineTotal = (oi.unit_price + itemToppingsTotal) * oi.quantity;

                    itemTable.AddCell(new PdfPCell(new Phrase(stt.ToString(), normalFont)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 6f });
                    itemTable.AddCell(new PdfPCell(new Phrase(productName, normalFont)) { Padding = 6f });
                    itemTable.AddCell(new PdfPCell(new Phrase(size, normalFont)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 6f });
                    itemTable.AddCell(new PdfPCell(new Phrase(optionsStr, smallFont)) { Padding = 6f });
                    itemTable.AddCell(new PdfPCell(new Phrase(oi.quantity.ToString(), normalFont)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 6f });
                    itemTable.AddCell(new PdfPCell(new Phrase($"{lineTotal:N0} đ", normalFont)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 6f });

                    stt++;
                }

                document.Add(itemTable);

                PdfPTable summaryTable = new PdfPTable(2)
                {
                    WidthPercentage = 45,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    SpacingAfter = 20f
                };
                summaryTable.SetWidths(new float[] { 50f, 50f });

                summaryTable.AddCell(new PdfPCell(new Phrase("Tạm tính:", normalFont)) { Border = Rectangle.NO_BORDER, Padding = 4f });
                summaryTable.AddCell(new PdfPCell(new Phrase($"{ord.subtotal:N0} đ", normalFont)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 4f });

                summaryTable.AddCell(new PdfPCell(new Phrase("Phí vận chuyển:", normalFont)) { Border = Rectangle.NO_BORDER, Padding = 4f });
                summaryTable.AddCell(new PdfPCell(new Phrase($"{ord.shipping_fee:N0} đ", normalFont)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 4f });

                if (ord.discount_amount > 0)
                {
                    summaryTable.AddCell(new PdfPCell(new Phrase("Giảm giá:", normalFont)) { Border = Rectangle.NO_BORDER, Padding = 4f });
                    summaryTable.AddCell(new PdfPCell(new Phrase($"-{ord.discount_amount:N0} đ", normalFont)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 4f });
                }

                summaryTable.AddCell(new PdfPCell(new Phrase("TỔNG THANH TOÁN:", boldFont)) { Border = Rectangle.TOP_BORDER, PaddingTop = 8f });
                summaryTable.AddCell(new PdfPCell(new Phrase($"{ord.final_total:N0} đ", new Font(bf, 13, Font.BOLD, new BaseColor(225, 29, 72)))) { Border = Rectangle.TOP_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT, PaddingTop = 8f });

                document.Add(summaryTable);

                Paragraph thankYou = new Paragraph("Cảm ơn quý khách đã tin dùng và ủng hộ TeaBloom Milk Tea!", normalFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 20f
                };
                document.Add(thankYou);

                Paragraph footerNote = new Paragraph("Hotline hỗ trợ & Khiếu nại: 1900 1234 - cskh@trasua.vn", smallFont)
                {
                    Alignment = Element.ALIGN_CENTER
                };
                document.Add(footerNote);

                document.Close();
                return ms.ToArray();
            }
        }
    }
}

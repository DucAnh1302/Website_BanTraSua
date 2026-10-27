using System;
using System.Collections.Generic;
using System.Linq;

namespace QL_WebBanTraSua.Models
{
    public class ToppingItem
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    public class CartItem
    {
        public string CartItemId { get; set; }
        public long ProductId { get; set; }
        public long VariantId { get; set; }
        public string ProductName { get; set; }
        public string Image { get; set; }
        public string Size { get; set; }
        public decimal VariantPrice { get; set; }
        public string SugarLevel { get; set; }
        public string IceOption { get; set; }
        public List<ToppingItem> Toppings { get; set; }
        public string Note { get; set; }
        public int Quantity { get; set; }

        public CartItem()
        {
            Toppings = new List<ToppingItem>();
            Quantity = 1;
            CartItemId = Guid.NewGuid().ToString("N");
        }

        public decimal UnitPrice
        {
            get
            {
                decimal total = VariantPrice;
                if (Toppings != null && Toppings.Count > 0)
                {
                    total += Toppings.Sum(t => t.Price);
                }
                return total;
            }
        }

        public decimal TotalPrice
        {
            get
            {
                return UnitPrice * Quantity;
            }
        }
    }
}

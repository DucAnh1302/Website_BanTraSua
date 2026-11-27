using System;

namespace QL_WebBanTraSua.Models
{
    public class PosCheckoutState
    {
        public string CustomerPhone { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public long? MatchedCustomerId { get; set; }
        public string MatchedCustomerTier { get; set; }
        public int MatchedCustomerPoints { get; set; }
        public bool IsNewCustomerWillCreate { get; set; }
        public string PaymentMethod { get; set; } = "cash";
        public decimal? AmountGiven { get; set; }
    }
}

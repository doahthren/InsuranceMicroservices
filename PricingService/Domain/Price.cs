#nullable disable
using System.Collections.Generic;
using System.Linq;

namespace PricingService.Domain
{
    public class CoverPrice
    {
        public CoverPrice(string coverCode, decimal price)
        {
            CoverCode = coverCode;
            Price = price;
        }

        public string CoverCode { get; }
        public decimal Price { get; }
    }

    public class Price
    {
        public Price(IDictionary<string, CoverPrice> covers)
        {
            Covers = covers ?? new Dictionary<string, CoverPrice>();
        }

        public IDictionary<string, CoverPrice> Covers { get; }

        public decimal TotalPremium
        {
            get { return Covers.Values.Sum(c => c.Price); }
        }
    }
}
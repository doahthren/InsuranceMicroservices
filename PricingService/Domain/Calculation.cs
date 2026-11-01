#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace PricingService.Domain
{
    public class Calculation
    {
        public Calculation(string productCode, DateTimeOffset policyFrom, DateTimeOffset policyTo,
            IEnumerable<string> selectedCovers, IDictionary<string, object> subject)
        {
            ProductCode = productCode;
            PolicyFrom = policyFrom;
            PolicyTo = policyTo;
            SelectedCovers = selectedCovers == null ? new List<string>() : selectedCovers.ToList();
            Subject = subject ?? new Dictionary<string, object>();
        }

        public string ProductCode { get; }
        public DateTimeOffset PolicyFrom { get; }
        public DateTimeOffset PolicyTo { get; }
        public IList<string> SelectedCovers { get; }
        public IDictionary<string, object> Subject { get; }
    }
}
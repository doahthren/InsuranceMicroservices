#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PricingService.Domain;

namespace PricingService.DataAccess
{
    public class InMemoryTariffRepository : ITariffRepository
    {
        private readonly Dictionary<string, Tariff> tariffs;

        public InMemoryTariffRepository(IEnumerable<Tariff> items)
        {
            tariffs = items.ToDictionary(t => t.Code);
        }

        public Task<Tariff> this[string productCode]
        {
            get
            {
                Tariff tariff;
                if (productCode == null || !tariffs.TryGetValue(productCode, out tariff))
                    throw new InvalidOperationException("Tariff for product '" + productCode + "' was not found.");
                return Task.FromResult(tariff);
            }
        }
    }

    public class InMemoryDataStore : IDataStore
    {
        public InMemoryDataStore()
        {
            Tariffs = new InMemoryTariffRepository(DemoTariffs.All());
        }

        public ITariffRepository Tariffs { get; }
    }

    public static class DemoTariffs
    {
        public static IEnumerable<Tariff> All()
        {
            return new[] { Travel(), House(), Farm(), Car() };
        }

        public static Tariff Travel()
        {
            var travel = new Tariff("TRI");
            travel.BasePremiumRules.AddBasePriceRule("C1", null,
                "(NUM_OF_ADULTS) * (DESTINATION == \"EUR\" ? 26.00M : 34.00M)");
            travel.BasePremiumRules.AddBasePriceRule("C2", null, "(NUM_OF_ADULTS + NUM_OF_CHILDREN) * 26.00M");
            travel.BasePremiumRules.AddBasePriceRule("C3", null, "(NUM_OF_ADULTS + NUM_OF_CHILDREN) * 10.00M");

            travel.DiscountMarkupRules.AddPercentMarkup("DESTINATION == \"WORLD\"", 1.5M);

            return travel;
        }

        public static Tariff House()
        {
            var house = new Tariff("HSI");

            house.BasePremiumRules.AddBasePriceRule("C1", "TYP == \"APT\"", "AREA * 1.25M");
            house.BasePremiumRules.AddBasePriceRule("C1", "TYP == \"HOUSE\"", "AREA * 1.50M");

            house.BasePremiumRules.AddBasePriceRule("C2", "TYP == \"APT\"", "AREA * 0.25M");
            house.BasePremiumRules.AddBasePriceRule("C2", "TYP == \"HOUSE\"", "AREA * 0.45M");

            house.BasePremiumRules.AddBasePriceRule("C3", null, "30M");
            house.BasePremiumRules.AddBasePriceRule("C4", null, "50M");

            house.DiscountMarkupRules.AddPercentMarkup("FLOOD == \"YES\"", 1.50M);
            house.DiscountMarkupRules.AddPercentMarkup("NUM_OF_CLAIM > 1", 1.25M);

            return house;
        }

        public static Tariff Farm()
        {
            var farm = new Tariff("FAI");

            farm.BasePremiumRules.AddBasePriceRule("C1", null, "10M");
            farm.BasePremiumRules.AddBasePriceRule("C2", null, "20M");
            farm.BasePremiumRules.AddBasePriceRule("C3", null, "30M");
            farm.BasePremiumRules.AddBasePriceRule("C4", null, "40M");

            farm.DiscountMarkupRules.AddPercentMarkup("NUM_OF_CLAIM > 2", 2.00M);

            return farm;
        }

        public static Tariff Car()
        {
            var car = new Tariff("CAR");

            car.BasePremiumRules.AddBasePriceRule("C1", null, "100M");
            car.DiscountMarkupRules.AddPercentMarkup("NUM_OF_CLAIM > 2", 1.50M);

            return car;
        }
    }
}

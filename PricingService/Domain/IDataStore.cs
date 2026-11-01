#nullable disable
using System.Threading.Tasks;

namespace PricingService.Domain
{
    public interface ITariffRepository
    {
        Task<Tariff> this[string productCode] { get; }
    }

    public interface IDataStore
    {
        ITariffRepository Tariffs { get; }
    }
}
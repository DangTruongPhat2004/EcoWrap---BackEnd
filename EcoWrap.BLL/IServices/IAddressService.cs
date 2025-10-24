using EcoWrap.Models.DTO;
using System.Threading.Tasks;

namespace EcoWrap.BLL.IServices
{
    public interface IAddressService
    {
        Task<AddressParseResult> ParseAddressAsync(string rawAddress);
    }
}
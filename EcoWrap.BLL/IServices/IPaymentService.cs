using System.Threading.Tasks;
using EcoWrap.Models.Payment;
using Net.payOS.Types;
namespace EcoWrap.BLL.IServices
{
    public interface IPaymentService
    {
        Task<CreatePaymentResponse> CreatePaymentLinkAsync(
        int orderId,
        long amount,
        string description,
        List<ItemData> items, // Hoặc List<CartItem> rồi xử lý trong PayosService
        string returnUrl,
        string cancelUrl
    );

        Task<WebhookProcessingResult> ProcessWebhookAsync(string webhookData, string signature);

    }   
}
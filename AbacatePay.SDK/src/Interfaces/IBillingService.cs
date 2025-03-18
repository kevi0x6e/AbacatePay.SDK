using AbacatePay.SDK.Models.Response.Billing;

namespace AbacatePay.SDK.Interfaces;

public interface IBillingService
{
    Task<BillingResponse> CreateBillingAsync(BillingRequest request, CancellationToken cancellationToken = default);
    Task<List<BillingResponse>> GetBillingsAsync(CancellationToken cancellationToken = default);
}
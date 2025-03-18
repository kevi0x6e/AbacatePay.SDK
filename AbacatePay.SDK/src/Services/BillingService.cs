using AbacatePay.SDK.Exceptions;
using AbacatePay.SDK.Interfaces;
using AbacatePay.SDK.Models.Response.Billing;

namespace AbacatePay.SDK.Services
{
    public class BillingService(HttpClient httpClient) : BaseService(httpClient), IBillingService
    {
        public async Task<BillingResponse> CreateBillingAsync(BillingRequest request, CancellationToken cancellationToken = default)
        {
            var (success, data, error) = await PostAsync<BillingResponse>("billing/create", request, cancellationToken);
            
            if (!success || error != null) throw new AbacatePayException(error?.Message ?? "Unknown error occurred");
            
            if (data == null) throw new AbacatePayException("API returned null data");
            
            return data;
        }

        public async Task<List<BillingResponse>> GetBillingsAsync(CancellationToken cancellationToken = default)
        {
            var (success, data, error) = await GetAsync<List<BillingResponse>>("billing/list", cancellationToken);
            
            if (!success || error != null) throw new AbacatePayException(error?.Message ?? "Unknown error occurred");

            return data ?? new List<BillingResponse>();
        }
    }
}
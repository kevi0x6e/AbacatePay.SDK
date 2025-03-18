using AbacatePay.SDK.Exceptions;
using AbacatePay.SDK.Interfaces;
using AbacatePay.SDK.Models.Request;
using AbacatePay.SDK.Models.Response.Customer;

namespace AbacatePay.SDK.Services;

public class CustomerService(HttpClient httpClient) : BaseService(httpClient), ICustomerService
{
    public async Task<CustomerResponse> CreateClientAsync(CustomerRequest request, CancellationToken cancellationToken = default)
    {
        var (success, data, error) = await PostAsync<CustomerResponse>("customer/create", request, cancellationToken);

        if (!success || error != null) throw new AbacatePayException(error?.Message ?? "Unknown error occurred");
        
        if (data == null) throw new AbacatePayException("API returned null data");

        return data;
    }
    
    public async Task<List<CustomerResponse>> GetClientsAsync(CancellationToken cancellationToken = default)
    {
        var (success, data, error) = await GetAsync<List<CustomerResponse>>("customer/list", cancellationToken);
    
        if (!success || error != null) throw new AbacatePayException(error?.Message ?? "Unknown error occurred");
    
        return data ?? new List<CustomerResponse>();
    }
}
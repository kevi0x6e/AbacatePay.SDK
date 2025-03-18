using AbacatePay.SDK.Models.Request;
using AbacatePay.SDK.Models.Response.Customer;

namespace AbacatePay.SDK.Interfaces;

public interface ICustomerService
{
    Task<CustomerResponse> CreateClientAsync(CustomerRequest request, CancellationToken cancellationToken = default);
    Task<List<CustomerResponse>> GetClientsAsync(CancellationToken cancellationToken = default);
}
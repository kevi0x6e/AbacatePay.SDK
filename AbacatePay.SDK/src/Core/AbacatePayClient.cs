using System.Net.Http.Headers;
using AbacatePay.SDK.Interfaces;
using AbacatePay.SDK.Services;

namespace AbacatePay.SDK.Core;

public class AbacatePayClient : IAbacatePayClient
{
    private readonly HttpClient _httpClient;
        
    public ICustomerService Customers { get; }
    public IBillingService Billings { get; }

    public AbacatePayClient(string apiKey)
    {
        _httpClient = new HttpClient
        {
            DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Bearer", apiKey) }
        };
            
        Customers = new CustomerService(_httpClient);
        Billings = new BillingService(_httpClient);
    }
}
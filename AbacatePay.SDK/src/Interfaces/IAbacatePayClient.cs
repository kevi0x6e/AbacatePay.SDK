namespace AbacatePay.SDK.Interfaces;

public class IAbacatePayClient
{
    ICustomerService Customers { get; }
    IBillingService Billings { get; }
}
using AbacatePay.SDK.Models.Request;

public record BillingRequest(List<string> Methods, List<ProductRequest> Products, Uri ReturnUrl, Uri CompletionUrl, string CustomerId, CustomerRequest Customer, string Frequency);
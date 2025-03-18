namespace AbacatePay.SDK.Models.Response.Billing;

public record BillingResponse(MetadataResponse Metadata, List<ProductResponse> Product, decimal Amount, string Status, bool DevMode, List<string> Methods, string Frequency, bool AllowCoupons, List<string> Coupons, Uri Url, BillingCustomerResponse Customer, string Id);
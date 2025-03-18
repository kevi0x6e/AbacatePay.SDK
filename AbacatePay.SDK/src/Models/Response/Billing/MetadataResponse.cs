namespace AbacatePay.SDK.Models.Response.Billing;

public record MetadataResponse(decimal Fee, Uri ReturnUrl, Uri CompletionUrl);
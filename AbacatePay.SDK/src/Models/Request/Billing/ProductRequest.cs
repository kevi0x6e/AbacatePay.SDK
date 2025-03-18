namespace AbacatePay.SDK.Models.Request;

public record ProductRequest(string ExternalId, string Name, string Description, int Quantity, int Price);
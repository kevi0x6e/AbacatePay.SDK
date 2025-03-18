namespace AbacatePay.SDK.Models.Response.AbacatePayApi;

public class ErrorModelResponse(string message)
{
    public string Message { get; set; } = message;
}
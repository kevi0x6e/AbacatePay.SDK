namespace AbacatePay.SDK.Exceptions;

public class AbacatePayException: Exception
{
    public AbacatePayException(string message) : base(message) { }
    public AbacatePayException(string message, Exception innerException) : base(message, innerException) { }
}
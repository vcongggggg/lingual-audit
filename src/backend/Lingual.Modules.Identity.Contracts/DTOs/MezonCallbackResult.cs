namespace Lingual.Modules.Identity.Contracts.DTOs;

public class MezonCallbackResult
{
    public int StatusCode { get; init; }
    public object Payload { get; init; } = default!;

    public static MezonCallbackResult Success(object payload)
    {
        return new MezonCallbackResult
        {
            StatusCode = 200,
            Payload = payload
        };
    }

    public static MezonCallbackResult Failure(int statusCode, object payload)
    {
        return new MezonCallbackResult
        {
            StatusCode = statusCode,
            Payload = payload
        };
    }
}
using static api_mmo.Rest.RestClient;

namespace api_mmo.Rest;

public delegate Task<RequestResult> RestDelegate(RequestInfo info);

public interface IRestClientMiddleware
{
    Task<RequestResult> InvokeAsync(RequestInfo client);
}
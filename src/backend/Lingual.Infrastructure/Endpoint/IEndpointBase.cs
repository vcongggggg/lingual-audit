using Microsoft.AspNetCore.Routing;

namespace Lingual.Infrastructure.Endpoint;

public interface IEndpointBase
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
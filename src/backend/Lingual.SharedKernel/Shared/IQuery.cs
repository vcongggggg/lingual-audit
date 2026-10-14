using MediatR;

namespace Lingual.SharedKernel.Shared;

public interface IQuery<out TResponse> : IRequest<TResponse>;
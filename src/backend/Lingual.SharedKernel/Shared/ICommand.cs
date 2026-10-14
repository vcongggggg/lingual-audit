using MediatR;

namespace Lingual.SharedKernel.Shared;

public interface ICommand<out TResponse> : IRequest<TResponse>, ITxRequest;
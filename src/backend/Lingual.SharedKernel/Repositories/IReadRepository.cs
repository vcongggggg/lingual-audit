using Ardalis.Specification;
using Lingual.SharedKernel.Entities;

namespace Lingual.SharedKernel.Repositories;

public interface IReadRepository<T> : IReadRepositoryBase<T> where T : class, IAggregateRoot;
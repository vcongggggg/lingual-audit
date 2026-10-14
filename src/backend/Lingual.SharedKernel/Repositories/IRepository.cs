using Ardalis.Specification;
using Lingual.SharedKernel.Entities;

namespace Lingual.SharedKernel.Repositories;

public interface IRepository<T> : IRepositoryBase<T> where T : class, IAggregateRoot;
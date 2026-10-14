using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Lingual.Persistence;

public interface IDatabaseFacade
{
    public DatabaseFacade Database { get; }
}
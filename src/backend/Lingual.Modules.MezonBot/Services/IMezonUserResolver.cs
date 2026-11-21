namespace Lingual.Modules.MezonBot.Services;

public interface IMezonUserResolver
{
    Task<Guid> ResolveUserIdAsync(string mezonUserId, string? displayName = null, CancellationToken cancellationToken = default);
}


using System.Threading;
using System.Threading.Tasks;

namespace Lingual.Modules.MezonBot.Services;

public record SrsReminderScanResult(
    int TotalUsersScanned,
    int EligibleUsersCount,
    int RemindersSentCount
);

public interface ISrsReminderService
{
    Task<SrsReminderScanResult> ScanAndSendRemindersAsync(CancellationToken cancellationToken = default);
}


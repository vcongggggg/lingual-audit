using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mezon.Net.Sdk;
using Mezon.Net.Client;
using Mezon.Net.Models;
using SdkClient = Mezon.Net.Sdk.MezonClient;

namespace Lingual.Modules.MezonBot;

public class MezonBotWorker : BackgroundService
{
    private readonly ILogger<MezonBotWorker> _logger;
    private readonly MezonBotOptions _options;
    private SdkClient? _client;

    public MezonBotWorker(ILogger<MezonBotWorker> logger, IOptions<MezonBotOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var botId = _options.GetEffectiveBotId();
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.Token) || botId <= 0)
        {
            _logger.LogWarning("⚠️ [MezonBot] Chưa cấu hình Mezon:Token hoặc BotId/AppId hợp lệ. Bot worker sẽ ở chế độ chờ và không kết nối gateway.");
            return;
        }

        try
        {
            _logger.LogInformation("🚀 [MezonBot] Đang khởi tạo kết nối Mezon.Net.Sdk 1.7.0 cho BotId: {BotId}...", botId);

            var clientOptions = new MezonClientOptions(botId, _options.Token);
            _client = new SdkClient(clientOptions);

            _client.ChannelMessageReceived += OnChannelMessageReceivedAsync;

            await _client.LoginAsync();
            _logger.LogInformation("✅ [MezonBot] Đã đăng nhập thành công vào Gateway Mezon!");

            // Chờ đến khi service dừng
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("🛑 [MezonBot] Nhận tín hiệu dừng service.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi kết nối hoặc xử lý sự kiện Mezon Bot");
        }
        finally
        {
            if (_client != null)
            {
                await _client.DisposeAsync();
                _logger.LogInformation("🔌 [MezonBot] Đã giải phóng kết nối MezonClient.");
            }
        }
    }

    private async Task OnChannelMessageReceivedAsync(ChannelMessageEventData eventData)
    {
        try
        {
            ChannelMessageResponse data = (ChannelMessageResponse)eventData;
            var rawContent = data.Content;
            if (string.IsNullOrWhiteSpace(rawContent))
            {
                return;
            }

            var parsed = MessageContent.Parse(rawContent);
            var text = parsed.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            _logger.LogInformation("📩 [MezonBot] Nhận tin nhắn từ Channel {ChannelId}: '{Text}'", data.ChannelId, text);

            if (text.Equals("/ping", StringComparison.OrdinalIgnoreCase))
            {
                await ReplyToChannelAsync(data.ChannelId, "🏓 **Pong!** Bot LINGUAL hoạt động bình thường trên nền tảng Mezon Campus Studio 2026. (SDK: Mezon.Net.Sdk 1.7.0)");
            }
            else if (text.Equals("/help", StringComparison.OrdinalIgnoreCase))
            {
                var helpMsg = "🤖 **LINGUAL Bot — Trợ lý Học Ngôn ngữ Mezon**\n\n" +
                              "Các câu lệnh khả dụng:\n" +
                              "• `/ping`: Kiểm tra trạng thái hoạt động của Bot\n" +
                              "• `/learn`: Mở mini-app học từ vựng thẻ 3D (Spaced Repetition SM-2)\n" +
                              "• `/help`: Hiển thị danh sách câu lệnh hỗ trợ";
                await ReplyToChannelAsync(data.ChannelId, helpMsg);
            }
            else if (text.Equals("/learn", StringComparison.OrdinalIgnoreCase))
            {
                var learnMsg = "📚 **Học Từ Vựng Tiếng Anh Cùng LINGUAL**\n\n" +
                               "👉 Bắt đầu phiên ôn tập thẻ ghi nhớ Flashcard 3D và thuật toán lặp lại ngắt quãng SM-2 tại Mini-App Clan!\n" +
                               "💡 Gõ `/quiz` để tham gia đấu vui từ vựng nhanh trong kênh Clan.";
                await ReplyToChannelAsync(data.ChannelId, learnMsg);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi xử lý tin nhắn sự kiện");
        }
    }

    private async Task ReplyToChannelAsync(long channelId, string responseText)
    {
        if (_client == null || channelId <= 0)
        {
            return;
        }

        try
        {
            var channel = await _client.GetChannelAsync(channelId);
            if (channel != null)
            {
                await channel.SendTextAsync(responseText);
                _logger.LogInformation("📤 [MezonBot] Đã gửi phản hồi đến Channel {ChannelId}: '{Response}'", channelId, responseText);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Không thể gửi tin nhắn phản hồi tới channel {ChannelId}", channelId);
        }
    }
}

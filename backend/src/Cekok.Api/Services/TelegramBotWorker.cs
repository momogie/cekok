using System.Text.Json;
using System.Text.Json.Serialization;
using Cekok.Api.Data;
using Cekok.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Cekok.Api.Services;

public class TelegramBotWorker(
    IServiceProvider serviceProvider,
    ILogger<TelegramBotWorker> logger
) : BackgroundService
{
    private int _offset = 0;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Telegram Bot Worker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CekokDbContext>();
                var enc = scope.ServiceProvider.GetRequiredService<EncryptionService>();

                var tokenSetting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "telegram_bot_token", stoppingToken);
                if (tokenSetting == null || string.IsNullOrEmpty(tokenSetting.Value))
                {
                    await Task.Delay(10000, stoppingToken);
                    continue;
                }

                var token = tokenSetting.IsSecure ? enc.Decrypt(tokenSetting.Value) : tokenSetting.Value;
                await PollUpdatesAsync(token, db, stoppingToken);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException) break;
                logger.LogError(ex, "Error in Telegram Bot Worker loop");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task PollUpdatesAsync(string token, CekokDbContext db, CancellationToken ct)
    {
        using var http = new HttpClient();
        // Use a shorter timeout if we're in a loop to avoid blocking too long on shutdown
        var url = $"https://api.telegram.org/bot{token}/getUpdates?offset={_offset}&timeout=20";
        
        try
        {
            var response = await http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errContent = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Telegram getUpdates failed: {Status} {Error}", response.StatusCode, errContent);
                await Task.Delay(5000, ct);
                return;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var updates = JsonSerializer.Deserialize<TelegramUpdateResponse>(json);

            if (updates?.Result == null || updates.Result.Count == 0) return;

            foreach (var update in updates.Result)
            {
                _offset = update.UpdateId + 1;
                if (update.Message != null)
                {
                    await HandleMessageAsync(token, update.Message, db, ct);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to poll Telegram updates");
            await Task.Delay(2000, ct);
        }
    }

    private async Task HandleMessageAsync(string token, TelegramMessage message, CekokDbContext db, CancellationToken ct)
    {
        var chatId = message.Chat.Id.ToString();
        var text = message.Text?.Trim() ?? "";

        if (text.StartsWith("/"))
        {
            var cmd = text.Split(' ')[0].ToLower();
            
            if (cmd == "/start")
            {
                await SendReplyAsync(token, chatId, "👋 Welcome to Cekok Bot!\n\nI can notify you about deployments.\n\nCommands:\n/subscribe - Get all deployment notifications\n/unsubscribe - Stop notifications\n/id - Show your Telegram Chat ID", ct);
            }
            else if (cmd == "/id")
            {
                await SendReplyAsync(token, chatId, $"Your Chat ID: `{chatId}`", ct);
            }
            else if (cmd == "/subscribe")
            {
                var sub = await db.TelegramSubscribers.FirstOrDefaultAsync(s => s.ChatId == chatId, ct);
                if (sub == null)
                {
                    sub = new TelegramSubscriber
                    {
                        ChatId = chatId,
                        Username = message.From?.Username,
                        DisplayName = $"{message.From?.FirstName} {message.From?.LastName}".Trim(),
                        IsActive = true
                    };
                    db.TelegramSubscribers.Add(sub);
                }
                else
                {
                    sub.IsActive = true;
                }

                await db.SaveChangesAsync(ct);
                await SendReplyAsync(token, chatId, "✅ You have successfully subscribed to all deployment notifications!", ct);
                logger.LogInformation("New telegram subscription: {ChatId} ({Username})", chatId, message.From?.Username);
            }
            else if (cmd == "/unsubscribe")
            {
                var sub = await db.TelegramSubscribers.FirstOrDefaultAsync(s => s.ChatId == chatId, ct);
                if (sub != null && sub.IsActive)
                {
                    sub.IsActive = false;
                    await db.SaveChangesAsync(ct);
                    await SendReplyAsync(token, chatId, "❌ You have unsubscribed from notifications.", ct);
                }
                else
                {
                    await SendReplyAsync(token, chatId, "You are not currently subscribed.", ct);
                }
            }
        }
    }

    private async Task SendReplyAsync(string token, string chatId, string text, CancellationToken ct)
    {
        try
        {
            using var http = new HttpClient();
            var url = $"https://api.telegram.org/bot{token}/sendMessage";
            await http.PostAsJsonAsync(url, new { 
                chat_id = chatId, 
                text = text,
                parse_mode = "Markdown"
            }, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send reply to {ChatId}", chatId);
        }
    }
}

public class TelegramUpdateResponse {
    [JsonPropertyName("result")] public List<TelegramUpdate>? Result { get; set; }
}
public class TelegramUpdate {
    [JsonPropertyName("update_id")] public int UpdateId { get; set; }
    [JsonPropertyName("message")] public TelegramMessage? Message { get; set; }
}
public class TelegramMessage {
    [JsonPropertyName("message_id")] public int MessageId { get; set; }
    [JsonPropertyName("from")] public TelegramUser? From { get; set; }
    [JsonPropertyName("chat")] public TelegramChat Chat { get; set; } = null!;
    [JsonPropertyName("text")] public string? Text { get; set; }
}
public class TelegramUser {
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("username")] public string? Username { get; set; }
    [JsonPropertyName("first_name")] public string? FirstName { get; set; }
    [JsonPropertyName("last_name")] public string? LastName { get; set; }
}
public class TelegramChat {
    [JsonPropertyName("id")] public long Id { get; set; }
}

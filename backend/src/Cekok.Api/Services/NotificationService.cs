using System.Net;
using System.Net.Mail;
using Cekok.Api.Data;
using Cekok.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Cekok.Api.Services;

public class NotificationService(CekokDbContext db, EncryptionService enc, ILogger<NotificationService> logger)
{
    public async Task SendDeploymentNotificationAsync(Application app, DeployJob job)
    {
        if (app.NotifyEmail && !string.IsNullOrEmpty(app.NotifyEmailAddress))
        {
            await SendEmailAsync(app, job);
        }

        if (app.NotifyTelegram)
        {
            await SendTelegramAsync(app, job);
        }
    }

    public async Task SendTestNotificationAsync(Application app, string type)
    {
        var mockJob = new DeployJob
        {
            Status = "success",
            TriggeredBy = "manual",
            TriggeredByUser = "test-user",
            StartedAt = DateTime.UtcNow.ToString("O"),
            FinishedAt = DateTime.UtcNow.ToString("O"),
            CommitHash = "test12345678"
        };

        if (type == "email")
        {
            await SendEmailAsync(app, mockJob);
        }
        else if (type == "telegram")
        {
            await SendTelegramAsync(app, mockJob);
        }
    }

    private async Task SendEmailAsync(Application app, DeployJob job)
    {
        try
        {
            var host = await GetSetting("smtp_host");
            var portStr = await GetSetting("smtp_port");
            var user = await GetSetting("smtp_user");
            var pass = await GetSetting("smtp_pass", true);
            var from = await GetSetting("smtp_from");

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(from))
            {
                logger.LogWarning("SMTP host or from address is not configured. Skipping email.");
                return;
            }

            int.TryParse(portStr ?? "587", out var port);

            using var smtp = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(user, pass),
                EnableSsl = true
            };

            var statusEmoji = job.Status.ToLower() == "success" ? "✅" : "❌";
            var subject = $"{statusEmoji} [{job.Status.ToUpper()}] Deployment: {app.Name}";
            var body = $"""
                Deployment for {app.Name} has finished.
                
                Status: {job.Status.ToUpper()}
                Triggered By: {job.TriggeredBy} ({(job.TriggeredByUser ?? "system")})
                Started At: {job.StartedAt}
                Finished At: {job.FinishedAt}
                Commit Hash: {job.CommitHash ?? "N/A"}
                
                ---
                Sent by Cekok Dashboard
                """;

            var mail = new MailMessage(from, app.NotifyEmailAddress!, subject, body);
            await smtp.SendMailAsync(mail);
            logger.LogInformation("Email notification sent for app {AppName} to {Email}", app.Name, app.NotifyEmailAddress);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email notification for app {AppName}", app.Name);
        }
    }

    private async Task SendTelegramAsync(Application app, DeployJob job)
    {
        try
        {
            var token = await GetSetting("telegram_bot_token", true);
            var defaultChatId = await GetSetting("telegram_chat_id");
            
            var chatId = !string.IsNullOrEmpty(app.NotifyTelegramChatId) ? app.NotifyTelegramChatId : defaultChatId;
            
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(chatId))
            {
                logger.LogWarning("Telegram token or chat ID is not configured. Skipping Telegram.");
                return;
            }

            var statusEmoji = job.Status.ToLower() == "success" ? "✅" : "❌";
            var message = $"""
                {statusEmoji} *Deployment {job.Status.ToUpper()}*
                *App:* {app.Name}
                *Trigger:* {job.TriggeredBy}
                *Commit:* {job.CommitHash?.Substring(0, Math.Min(7, job.CommitHash?.Length ?? 0)) ?? "N/A"}
                """;

            using var http = new HttpClient();
            var url = $"https://api.telegram.org/bot{token}/sendMessage";
            var resp = await http.PostAsJsonAsync(url, new {
                chat_id = chatId,
                text = message,
                parse_mode = "Markdown"
            });
            
            if (!resp.IsSuccessStatusCode)
            {
                var error = await resp.Content.ReadAsStringAsync();
                logger.LogWarning("Telegram API Error: {Error}", error);
            }
            else
            {
                logger.LogInformation("Telegram notification sent for app {AppName} to {ChatId}", app.Name, chatId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send telegram notification for app {AppName}", app.Name);
        }
    }

    private async Task<string?> GetSetting(string key, bool isSecure = false)
    {
        var setting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null || string.IsNullOrEmpty(setting.Value)) return null;
        return isSecure ? enc.Decrypt(setting.Value) : setting.Value;
    }
}

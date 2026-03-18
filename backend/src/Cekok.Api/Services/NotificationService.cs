using System.Net;
using System.Net.Mail;
using Cekok.Api.Data;
using Cekok.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Cekok.Api.Services;

public class NotificationService(CekokDbContext db, EncryptionService enc, ILogger<NotificationService> logger)
{
    public async Task SendDeploymentNotificationAsync(Application app, DeployJob job, Func<string, string, Task>? log = null)
    {
        if (log != null) await log("info", $"Checking notifications for {app.Name} (Email: {app.NotifyEmail}, Telegram: {app.NotifyTelegram})...");

        bool sentAny = false;
        if (app.NotifyEmail && !string.IsNullOrEmpty(app.NotifyEmailAddress))
        {
            if (log != null) await log("info", $"Sending email notification to {app.NotifyEmailAddress}...");
            await SendEmailAsync(app, job, log);
            sentAny = true;
        }

        if (app.NotifyTelegram)
        {
            if (log != null) await log("info", "Sending telegram notification...");
            await SendTelegramAsync(app, job, log);
            sentAny = true;
        }

        if (!sentAny && log != null)
        {
            await log("info", "No notifications were enabled or configured for this app.");
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

    private async Task SendEmailAsync(Application app, DeployJob job, Func<string, string, Task>? log = null)
    {
        try
        {
            var host = await GetSetting("smtp_host");
            var portStr = await GetSetting("smtp_port");
            var user = await GetSetting("smtp_username");
            var pass = await GetSetting("smtp_password", true);
            var from = await GetSetting("smtp_from_email");

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(from))
            {
                logger.LogWarning("SMTP host or from address is not configured. Skipping email.");
                if (log != null) await log("warn", "SMTP not configured (host or from missing), skipping email.");
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
            if (log != null) await log("success", "✓ Email notification sent");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email notification for app {AppName}", app.Name);
            if (log != null) await log("error", $"✗ Failed to send email: {ex.Message}");
        }
    }

    private async Task SendTelegramAsync(Application app, DeployJob job, Func<string, string, Task>? log = null)
    {
        try
        {
            var token = await GetSetting("telegram_bot_token", true);
            var defaultChatId = await GetSetting("telegram_admin_chat_id");
            
            var chatId = !string.IsNullOrEmpty(app.NotifyTelegramChatId) ? app.NotifyTelegramChatId : defaultChatId;
            
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(chatId))
            {
                logger.LogWarning("Telegram token or chat ID is not configured. Skipping Telegram.");
                if (log != null) await log("warn", "Telegram not configured (token or chat id missing), skipping.");
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
                if (log != null) await log("error", $"✗ Telegram API Error: {resp.StatusCode}");
            }
            else
            {
                logger.LogInformation("Telegram notification sent for app {AppName} to {ChatId}", app.Name, chatId);
                if (log != null) await log("success", "✓ Telegram notification sent");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send telegram notification for app {AppName}", app.Name);
            if (log != null) await log("error", $"✗ Failed to send telegram: {ex.Message}");
        }
    }

    private async Task<string?> GetSetting(string key, bool isSecure = false)
    {
        var setting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null || string.IsNullOrEmpty(setting.Value)) return null;
        return isSecure ? enc.Decrypt(setting.Value) : setting.Value;
    }
}

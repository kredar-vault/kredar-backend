using System.Net.Http.Headers;
using System.Net.Http.Json;
using Kredar.API.Config;
using Microsoft.Extensions.Options;

namespace Kredar.API.Auth;

/// <summary>
/// Transactional email via Resend (https://resend.com), matching Xental's approach.
/// Sends are best-effort: failures are logged, never thrown (so they don't leak whether
/// an account exists or block the auth flow). Users can always request another email.
/// </summary>
public class EmailService(
    IHttpClientFactory httpFactory,
    IOptions<ResendSettings> resendOptions,
    IConfiguration config,
    ILogger<EmailService> logger)
{
    private readonly ResendSettings _resend = resendOptions.Value;
    private readonly string _baseUrl = config["AppSettings:BaseUrl"] ?? "http://localhost:8080";

    public Task SendVerificationEmailAsync(string toEmail, string token)
    {
        var verificationLink = $"{_baseUrl}/api/auth/verify-email?token={token}";
        var html = $"""
            <h2>Welcome to Kredar</h2>
            <p>Click the button below to verify your email address.</p>
            <a href="{verificationLink}"
               style="background:#000;color:#fff;padding:12px 24px;text-decoration:none;border-radius:6px;display:inline-block;">
               Verify Email
            </a>
            <p>This link expires in 24 hours.</p>
            <p>If you did not create a Kredar account, ignore this email.</p>
            """;
        return SendAsync(toEmail, "Verify your Kredar email", html);
    }

    public Task SendLoginOtpEmailAsync(string toEmail, string otp)
    {
        var html = $"""
            <h2>Your login code</h2>
            <p>Use the code below to complete your sign in.</p>
            <div style="font-size:36px;font-weight:bold;letter-spacing:8px;padding:20px 0;">{otp}</div>
            <p>This code expires in <strong>10 minutes</strong>.</p>
            <p>If you did not try to log in, please ignore this email.</p>
            """;
        return SendAsync(toEmail, "Your Kredar login code", html);
    }

    private async Task SendAsync(string toEmail, string subject, string html)
    {
        if (!_resend.IsConfigured)
        {
            logger.LogWarning("Resend is not configured; skipping email '{Subject}' to {To}.", subject, toEmail);
            return;
        }

        try
        {
            var client = httpFactory.CreateClient("resend");
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _resend.ApiKey);
            request.Content = JsonContent.Create(new
            {
                from = string.IsNullOrWhiteSpace(_resend.FromName)
                    ? _resend.FromEmail
                    : $"{_resend.FromName} <{_resend.FromEmail}>",
                to = new[] { toEmail },
                subject,
                html,
            });

            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                logger.LogError("Resend send failed ({Status}) for '{Subject}' to {To}: {Body}",
                    (int)response.StatusCode, subject, toEmail, body);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Resend send threw for '{Subject}' to {To}.", subject, toEmail);
        }
    }
}

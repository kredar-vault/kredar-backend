using Kredar.API.Config;
using Microsoft.Extensions.Options;
using Resend;

namespace Kredar.API.Auth;

public class EmailService(IResend resend, IOptions<EmailSettings> emailOptions, IConfiguration config)
{
    private readonly EmailSettings _email = emailOptions.Value;
    private readonly string _baseUrl = config["AppSettings:BaseUrl"] ?? "http://localhost:8080";

    public async Task SendVerificationEmailAsync(string toEmail, string token)
    {
        var verificationLink = $"{_baseUrl}/api/auth/verify-email?token={token}";

        var message = new EmailMessage
        {
            From = $"{_email.FromName} <{_email.FromEmail}>",
            To = [toEmail],
            Subject = "Verify your Kredar email",
            HtmlBody = $"""
                <h2>Welcome to Kredar</h2>
                <p>Click the button below to verify your email address.</p>
                <a href="{verificationLink}"
                   style="background:#000;color:#fff;padding:12px 24px;text-decoration:none;border-radius:6px;display:inline-block;">
                   Verify Email
                </a>
                <p>This link expires in 24 hours.</p>
                <p>If you did not create a Kredar account, ignore this email.</p>
            """
        };

        await resend.EmailSendAsync(message);
    }

    public async Task SendLoginOtpEmailAsync(string toEmail, string otp)
    {
        var message = new EmailMessage
        {
            From = $"{_email.FromName} <{_email.FromEmail}>",
            To = [toEmail],
            Subject = "Your Kredar login code",
            HtmlBody = $"""
                <h2>Your login code</h2>
                <p>Use the code below to complete your sign in.</p>
                <div style="font-size:36px;font-weight:bold;letter-spacing:8px;padding:20px 0;">{otp}</div>
                <p>This code expires in <strong>10 minutes</strong>.</p>
                <p>If you did not try to log in, please ignore this email.</p>
            """
        };

        await resend.EmailSendAsync(message);
    }
}

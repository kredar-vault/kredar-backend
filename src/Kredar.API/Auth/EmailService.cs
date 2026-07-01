using Kredar.API.Config;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Kredar.API.Auth;

public class EmailService(IOptions<EmailSettings> emailOptions, IConfiguration config)
{
    private readonly EmailSettings _email = emailOptions.Value;
    private readonly string _baseUrl = config["AppSettings:BaseUrl"] ?? "http://localhost:8080";

    public async Task SendVerificationEmailAsync(string toEmail, string token)
    {
        var verificationLink = $"{_baseUrl}/api/auth/verify-email?token={token}";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_email.FromName, _email.FromEmail));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = "Verify your Kredar email";

        message.Body = new TextPart("html")
        {
            Text = $"""
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

        using var client = new SmtpClient();
        await client.ConnectAsync(_email.Host, _email.Port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_email.Username, _email.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    public async Task SendLoginOtpEmailAsync(string toEmail, string otp)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_email.FromName, _email.FromEmail));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = "Your Kredar login code";

        message.Body = new TextPart("html")
        {
            Text = $"""
                <h2>Your login code</h2>
                <p>Use the code below to complete your sign in.</p>
                <div style="font-size:36px;font-weight:bold;letter-spacing:8px;padding:20px 0;">{otp}</div>
                <p>This code expires in <strong>10 minutes</strong>.</p>
                <p>If you did not try to log in, please ignore this email.</p>
            """
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(_email.Host, _email.Port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_email.Username, _email.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}

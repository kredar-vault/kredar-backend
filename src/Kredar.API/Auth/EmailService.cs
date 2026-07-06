using Kredar.API.Config;
using Microsoft.Extensions.Options;
using Resend;

namespace Kredar.API.Auth;

public class EmailService(IResend resend, IOptions<EmailSettings> emailOptions, IConfiguration config)
{
    private readonly EmailSettings _email = emailOptions.Value;
    private readonly string _baseUrl = config["AppSettings:BaseUrl"] ?? "http://localhost:8080";
    private readonly string _frontendUrl = config["AppSettings:FrontendUrl"] ?? config["AppSettings:BaseUrl"] ?? "http://localhost:3000";

    public async Task SendVerificationEmailAsync(string toEmail, string token)
    {
        var verificationLink = $"{_baseUrl}/api/v1/auth/verify-email?token={token}";

        var message = new EmailMessage
        {
            From = $"{_email.FromName} <{_email.FromEmail}>",
            To = [toEmail],
            Subject = "Confirm your Kredar email address",
            HtmlBody = $"""
                <!DOCTYPE html>
                <html>
                <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
                <body style="margin:0;padding:0;background-color:#f4f4f4;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
                  <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#f4f4f4;padding:40px 0;">
                    <tr><td align="center">
                      <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;">

                        <!-- Header -->
                        <tr>
                          <td align="center" style="padding:0 0 24px 0;">
                            <span style="font-size:24px;font-weight:700;color:#011B33;letter-spacing:-0.5px;">Kredar</span>
                          </td>
                        </tr>

                        <!-- Card -->
                        <tr>
                          <td style="background:#ffffff;border-radius:8px;padding:48px 48px 40px;box-shadow:0 1px 3px rgba(0,0,0,0.08);">

                            <p style="margin:0 0 24px;font-size:16px;line-height:1.6;color:#374151;">
                              Thanks for signing up with Kredar! Before you get started accepting payments with Kredar,
                              we need you to confirm your email address. Please click the button below to complete your signup.
                            </p>

                            <table cellpadding="0" cellspacing="0" style="margin:32px 0;">
                              <tr>
                                <td align="center" style="border-radius:6px;background:#011B33;">
                                  <a href="{verificationLink}"
                                     style="display:inline-block;padding:14px 32px;font-size:15px;font-weight:600;color:#ffffff;text-decoration:none;border-radius:6px;letter-spacing:0.2px;">
                                    Confirm email address
                                  </a>
                                </td>
                              </tr>
                            </table>

                            <hr style="border:none;border-top:1px solid #E5E7EB;margin:32px 0;">

                            <p style="margin:0;font-size:13px;color:#9CA3AF;line-height:1.6;">
                              This link expires in <strong>24 hours</strong>. If you did not create a Kredar account, you can safely ignore this email.
                            </p>
                          </td>
                        </tr>

                        <!-- Footer -->
                        <tr>
                          <td align="center" style="padding:24px 0 0;">
                            <p style="margin:0;font-size:12px;color:#9CA3AF;">
                              &copy; 2026 Kredar. All rights reserved.
                            </p>
                          </td>
                        </tr>

                      </table>
                    </td></tr>
                  </table>
                </body>
                </html>
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

    public async Task SendPasswordResetEmailAsync(string toEmail, string token)
    {
        var resetLink = $"{_frontendUrl}/auth/reset-password?token={token}";

        var message = new EmailMessage
        {
            From = $"{_email.FromName} <{_email.FromEmail}>",
            To = [toEmail],
            Subject = "Reset your Kredar password",
            HtmlBody = $"""
                <h2>Reset your password</h2>
                <p>Click the button below to set a new password. This link expires in 1 hour.</p>
                <a href="{resetLink}"
                   style="background:#000;color:#fff;padding:12px 24px;text-decoration:none;border-radius:6px;display:inline-block;">
                   Reset Password
                </a>
                <p>If you did not request a password reset, ignore this email.</p>
            """
        };

        await resend.EmailSendAsync(message);
    }
}

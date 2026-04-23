using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace GLMS.Web.Services
{
    public class EmailSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = "GLMS System";
        public bool EnableSsl { get; set; } = true;
    }

    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
        Task SendContractStatusChangedAsync(string toEmail, string clientName, int contractId, string newStatus);
        Task SendServiceRequestCreatedAsync(string toEmail, string clientName, int requestId, decimal costUsd, decimal costZar);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(EmailSettings settings, ILogger<EmailService> logger)
        {
            _settings = settings;
            _logger = logger;
        }

        /// <summary>
        /// Sends a general HTML email via SMTP using MailKit.
        /// Logs a warning and continues gracefully if SMTP is not configured.
        /// </summary>
        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            // If no SMTP host is configured, log and skip (allows app to run without email setup)
            if (string.IsNullOrWhiteSpace(_settings.Host))
            {
                _logger.LogWarning("Email not sent — SMTP host is not configured. To: {Email} | Subject: {Subject}", toEmail, subject);
                return;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_settings.Host, _settings.Port,
                    _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

                if (!string.IsNullOrEmpty(_settings.Username))
                    await client.AuthenticateAsync(_settings.Username, _settings.Password);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent to {Email} — Subject: {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}.", toEmail);
            }
        }

        /// <summary>
        /// Notification sent to the client contact when a contract status changes.
        /// </summary>
        public async Task SendContractStatusChangedAsync(
            string toEmail, string clientName, int contractId, string newStatus)
        {
            var subject = $"[GLMS] Contract #{contractId} Status Update";

            var html = $@"
                <html><body style='font-family:Arial,sans-serif;color:#333;'>
                <h2 style='color:#1a3a5c;'>Contract Status Update</h2>
                <p>Dear <strong>{clientName}</strong>,</p>
                <p>We are writing to let you know that the status of your contract has been updated.</p>
                <table style='border-collapse:collapse;width:100%;max-width:400px;'>
                    <tr>
                        <td style='padding:8px;background:#f5f5f5;font-weight:bold;'>Contract ID</td>
                        <td style='padding:8px;'>#{contractId}</td>
                    </tr>
                    <tr>
                        <td style='padding:8px;background:#f5f5f5;font-weight:bold;'>New Status</td>
                        <td style='padding:8px;color:#1a7a4a;'><strong>{newStatus}</strong></td>
                    </tr>
                </table>
                <p style='margin-top:20px;'>Please log in to the GLMS portal for further details.</p>
                <p style='color:#888;font-size:12px;'>This is an automated notification from TechMove GLMS.</p>
                </body></html>";

            await SendEmailAsync(toEmail, subject, html);
        }

        /// <summary>
        /// Notification sent when a new service request is created under a contract.
        /// </summary>
        public async Task SendServiceRequestCreatedAsync(
            string toEmail, string clientName, int requestId, decimal costUsd, decimal costZar)
        {
            var subject = $"[GLMS] New Service Request #{requestId} Created";

            var html = $@"
                <html><body style='font-family:Arial,sans-serif;color:#333;'>
                <h2 style='color:#1a3a5c;'>New Service Request</h2>
                <p>Dear <strong>{clientName}</strong>,</p>
                <p>A new service request has been raised against your contract.</p>
                <table style='border-collapse:collapse;width:100%;max-width:400px;'>
                    <tr>
                        <td style='padding:8px;background:#f5f5f5;font-weight:bold;'>Request ID</td>
                        <td style='padding:8px;'>#{requestId}</td>
                    </tr>
                    <tr>
                        <td style='padding:8px;background:#f5f5f5;font-weight:bold;'>Cost (USD)</td>
                        <td style='padding:8px;'>$ {costUsd:N2}</td>
                    </tr>
                    <tr>
                        <td style='padding:8px;background:#f5f5f5;font-weight:bold;'>Cost (ZAR)</td>
                        <td style='padding:8px;'>R {costZar:N2}</td>
                    </tr>
                </table>
                <p style='margin-top:20px;'>Log in to the GLMS portal to review or approve this request.</p>
                <p style='color:#888;font-size:12px;'>This is an automated notification from TechMove GLMS.</p>
                </body></html>";

            await SendEmailAsync(toEmail, subject, html);
        }
    }
}

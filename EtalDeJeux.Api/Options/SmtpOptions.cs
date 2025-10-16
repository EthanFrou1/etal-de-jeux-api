namespace EtalDeJeux.Api.Options;

public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public string? FromName { get; set; }
    public bool UseSsl { get; set; } = false;
    public bool UseStartTls { get; set; } = true;
}

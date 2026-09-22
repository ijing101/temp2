namespace SHT85.Services;

public sealed class LogService
{
    public event EventHandler<string>? MessageLogged;

    public void Info(string message) => MessageLogged?.Invoke(this, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
}

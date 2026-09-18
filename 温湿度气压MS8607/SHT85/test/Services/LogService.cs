namespace test.Services;

public sealed class LogService
{
    public event EventHandler<string>? MessageLogged;

    public void Info(string message) => MessageLogged?.Invoke(this, $"[{DateTime.Now:HH:mm:ss}] {message}");
}

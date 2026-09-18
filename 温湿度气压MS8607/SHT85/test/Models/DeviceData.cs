namespace test.Models;

public sealed class DeviceData
{
    public double Temperature { get; init; }
    public double Humidity { get; init; }
    public double Pressure { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
}

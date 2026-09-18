namespace test.Models;

public sealed class DeviceConfig
{
    public string PortName { get; set; } = "COM3";
    public int BaudRate { get; set; } = 9600;
    public string Parity { get; set; } = "None";
    public int DataBits { get; set; } = 8;
    public int StopBits { get; set; } = 1;
    /// <summary>当前设备单播地址；硬件固件接受 1–247，0 为广播地址。</summary>
    public byte ConnectionAddress { get; set; } = 1;
    public byte SlaveAddress { get; set; } = 1;
    public int DeviceBaudRate { get; set; } = 9600;
    public int TemperatureCalibration { get; set; }
    public int HumidityCalibration { get; set; }
    public int PressureCalibration { get; set; }
}

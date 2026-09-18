namespace test.Models;

/// <summary>寄存器地址与编码来自项目根目录的《寄存器说明.txt》。</summary>
public static class ModbusRegisterMap
{
    public const ushort Temperature = 0;
    public const ushort Humidity = 1;
    public const ushort Pressure = 2;
    public const ushort EnterBootloader = 17;
    public const ushort AppVersion = 15;
    public const ushort TemperatureCalibration = 20;
    public const ushort HumidityCalibration = 21;
    public const ushort PressureCalibration = 22;
    public const ushort DeviceAddress = 23;
    public const ushort DeviceBaudRate = 24;

    public static ushort ToBaudCode(int baudRate) => baudRate switch
    {
        9600 => 0,
        19200 => 1,
        115200 => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(baudRate), "设备波特率只支持 9600、19200 或 115200。")
    };

    public static int FromBaudCode(ushort code) => code switch
    {
        0 => 9600,
        1 => 19200,
        2 => 115200,
        _ => throw new ArgumentOutOfRangeException(nameof(code), $"设备返回了未知的波特率编码：{code}。")
    };

    public static ushort ToSignedRegister(int value, string parameterName)
    {
        if (value is < -999 or > 999)
        {
            throw new ArgumentOutOfRangeException(parameterName, "校准寄存器值必须在 -999 到 999 之间（界面输入范围为 -99.9 到 99.9）。");
        }

        return unchecked((ushort)(short)value);
    }

    public static int FromSignedRegister(ushort value) => unchecked((short)value);
}

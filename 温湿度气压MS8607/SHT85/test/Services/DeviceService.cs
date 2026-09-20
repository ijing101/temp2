using test.Communication;
using test.Models;

namespace test.Services;

/// <summary>实际设备服务：把设备寄存器映射为界面使用的配置和测量值。</summary>
public sealed class DeviceService : IDisposable
{
    private readonly SerialPortService _serialPort = new();
    private DeviceConfig? _currentConfig;
    private byte? _connectedAddress;
    private DeviceMode _deviceMode = DeviceMode.Disconnected;
    private int _configuredPortBaudRate = 9600;

    public bool IsConnected => IsPortOpen
        && _deviceMode is DeviceMode.Application or DeviceMode.Bootloader;
    public bool IsPortOpen => _serialPort.IsOpen;
    public DeviceMode DeviceMode => _deviceMode;
    public ushort? AppVersion { get; private set; }
    public int BaudRate => _serialPort.BaudRate;
    public Stream BaseStream => _serialPort.BaseStream;

    public static string[] GetPortNames() => SerialPortService.GetPortNames();

    public void OpenPort(DeviceConfig config)
    {
        _serialPort.Open(config);
        _currentConfig = config;
        _configuredPortBaudRate = config.BaudRate;
        _connectedAddress = null;
        _deviceMode = DeviceMode.Disconnected;
        AppVersion = null;
    }

    public DeviceConfig ConnectDevice(byte address)
    {
        if (!IsPortOpen) throw new InvalidOperationException("请先打开串口。");
        if (address is < 1 or > 247) throw new ArgumentOutOfRangeException(nameof(address), "设备连接地址必须是 1 到 247。");

        _connectedAddress = null;
        _deviceMode = DeviceMode.Disconnected;
        AppVersion = null;
        try
        {
            var registers = _serialPort.ReadHoldingRegisters(address, ModbusRegisterMap.TemperatureCalibration, 5);
            var config = DecodeConfig(registers, address);
            var appVersion = _serialPort.ReadHoldingRegisters(address, ModbusRegisterMap.AppVersion, 1)[0];
            _currentConfig = config;
            AppVersion = appVersion;
            _connectedAddress = address;
            _deviceMode = DeviceMode.Application;
            return config;
        }
        catch
        {
            AppVersion = null;
            throw;
        }
    }

    public bool TryDetectBootloader(TimeSpan timeout, CancellationToken cancellationToken)
        => _serialPort.TryDetectBootloaderSignal(timeout, cancellationToken);

    public Task UpgradeWithYmodemAsync(string firmwarePath, IProgress<int>? progress, CancellationToken cancellationToken)
        => _serialPort.SendYmodemAsync(firmwarePath, progress, cancellationToken);

    public void SetPortBaudRate(int baudRate)
    {
        if (baudRate is not (9600 or 19200 or 115200))
        {
            throw new ArgumentOutOfRangeException(nameof(baudRate), "串口波特率只支持 9600、19200 或 115200。");
        }

        _serialPort.SetBaudRate(baudRate);
    }

    public void MarkBootloaderConnected()
    {
        if (!IsPortOpen) throw new InvalidOperationException("请先打开串口。");

        _connectedAddress = null;
        _deviceMode = DeviceMode.Bootloader;
        AppVersion = null;
    }

    public void DisconnectDevice()
    {
        _connectedAddress = null;
        _deviceMode = DeviceMode.Disconnected;
        AppVersion = null;
    }

    public void ClosePort()
    {
        DisconnectDevice();
        _serialPort.Close();
        _currentConfig = null;
    }

    public DeviceConfig ReadConfig()
    {
        var address = EnsureConnectedAddress();
        var registers = _serialPort.ReadHoldingRegisters(address, ModbusRegisterMap.TemperatureCalibration, 5);
        _currentConfig = DecodeConfig(registers, address);
        return _currentConfig;
    }

    public ushort ReadAppVersion()
    {
        if (_deviceMode != DeviceMode.Application)
        {
            throw new InvalidOperationException("只有 APP 模式可以读取固件版本。");
        }

        var address = EnsureConnectedAddress();
        AppVersion = _serialPort.ReadHoldingRegisters(address, ModbusRegisterMap.AppVersion, 1)[0];
        return AppVersion.Value;
    }

    public DeviceData ReadData()
    {
        var address = EnsureConnectedAddress();
        var registers = _serialPort.ReadHoldingRegisters(address, ModbusRegisterMap.Temperature, 3);

        return new DeviceData
        {
            Temperature = unchecked((short)registers[0]) / 10.0,
            Humidity = registers[1] / 10.0,
            // 硬件 APP 将 MS8607.pressure（hPa）乘 10 写入 Reg[2]。
            Pressure = registers[2] / 10.0,
            Timestamp = DateTime.Now
        };
    }

    public byte[] WriteSingleRegister(ushort registerAddress, ushort registerValue)
    {
        var currentAddress = EnsureConnectedAddress();
        ValidateWritableRegister(registerAddress, registerValue);

        // 等待设备回显确认后再切换主机地址/波特率，保证写命令仍使用变更前的链路参数。
        var frame = _serialPort.WriteSingleRegister(currentAddress, registerAddress, registerValue);
        var config = _currentConfig ?? throw new InvalidOperationException("设备配置尚未初始化。");

        switch (registerAddress)
        {
            case ModbusRegisterMap.TemperatureCalibration:
                config.TemperatureCalibration = ModbusRegisterMap.FromSignedRegister(registerValue);
                break;
            case ModbusRegisterMap.HumidityCalibration:
                config.HumidityCalibration = ModbusRegisterMap.FromSignedRegister(registerValue);
                break;
            case ModbusRegisterMap.PressureCalibration:
                config.PressureCalibration = ModbusRegisterMap.FromSignedRegister(registerValue);
                break;
            case ModbusRegisterMap.DeviceAddress:
                config.SlaveAddress = checked((byte)registerValue);
                config.ConnectionAddress = checked((byte)registerValue);
                _connectedAddress = checked((byte)registerValue);
                break;
            case ModbusRegisterMap.DeviceBaudRate:
                var baudRate = ModbusRegisterMap.FromBaudCode(registerValue);
                _serialPort.SetBaudRate(baudRate);
                config.BaudRate = baudRate;
                config.DeviceBaudRate = baudRate;
                break;
            case ModbusRegisterMap.EnterBootloader:
                _deviceMode = DeviceMode.Bootloader;
                break;
        }

        return frame;
    }

    public bool EnterBootMode()
    {
        if (!IsConnected) return false;
        WriteSingleRegister(ModbusRegisterMap.EnterBootloader, 0x1234);
        // BOOT 程序的 RS-485 固定使用 9600 / 8N1，不跟随 APP 的 Flash 配置。
        _serialPort.SetBaudRate(9600);
        _serialPort.DiscardBuffers();
        if (_currentConfig is not null) _currentConfig.BaudRate = 9600;
        return _deviceMode == DeviceMode.Bootloader;
    }

    public async Task<DeviceConfig?> CompleteFirmwareUpgradeAsync()
    {
        if (!IsPortOpen) throw new InvalidOperationException("串口已关闭，无法确认固件升级结果。");

        // 未经过 APP 连接而直接检测到 BOOT 时，没有可恢复的 APP 地址/波特率。
        // 发送完成后保留串口打开，由上位机自动扫描并连接 APP。
        if (_connectedAddress is null || _currentConfig is null)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            _serialPort.SetBaudRate(_configuredPortBaudRate);
            _serialPort.DiscardBuffers();
            _connectedAddress = null;
            _deviceMode = DeviceMode.Disconnected;
            AppVersion = null;
            return null;
        }

        var config = _currentConfig;
        var address = _connectedAddress.Value;

        // BOOT 在终止包后校验并复位；首次启动还会延时校验，再由 APP 初始化串口。
        await Task.Delay(TimeSpan.FromSeconds(5));
        _serialPort.SetBaudRate(config.DeviceBaudRate);
        _serialPort.DiscardBuffers();

        Exception? lastError = null;
        for (var attempt = 0; attempt < 8; attempt++)
        {
            try
            {
                var registers = await Task.Run(() =>
                    _serialPort.ReadHoldingRegisters(address, ModbusRegisterMap.TemperatureCalibration, 5));
                var appVersion = await Task.Run(() =>
                    _serialPort.ReadHoldingRegisters(address, ModbusRegisterMap.AppVersion, 1));
                _currentConfig = DecodeConfig(registers, address);
                AppVersion = appVersion[0];
                _deviceMode = DeviceMode.Application;
                return _currentConfig;
            }
            catch (Exception ex) when (ex is IOException or TimeoutException)
            {
                lastError = ex;
                if (attempt < 7) await Task.Delay(TimeSpan.FromMilliseconds(750));
            }
        }

        throw new IOException("固件数据已发送，但设备未能在重启后恢复 Modbus 通信。请检查 .bin 文件并重新连接设备。", lastError);
    }

    public void Dispose() => ClosePort();

    private byte EnsureConnectedAddress()
    {
        if (!IsConnected || _connectedAddress is null) throw new InvalidOperationException("请先连接设备。");
        return _connectedAddress.Value;
    }

    private DeviceConfig DecodeConfig(ushort[] registers, byte connectionAddress)
    {
        var addressValue = registers[3];
        if (addressValue is < 1 or > 247) throw new IOException($"设备地址寄存器返回非法值 {addressValue}（硬件固件有效范围 1–247）。");

        var temperatureCalibration = ModbusRegisterMap.FromSignedRegister(registers[0]);
        var humidityCalibration = ModbusRegisterMap.FromSignedRegister(registers[1]);
        var pressureCalibration = ModbusRegisterMap.FromSignedRegister(registers[2]);
        ValidateReadCalibration(temperatureCalibration, "温度");
        ValidateReadCalibration(humidityCalibration, "湿度");
        ValidateReadCalibration(pressureCalibration, "气压");

        var baudRate = ModbusRegisterMap.FromBaudCode(registers[4]);
        var config = _currentConfig ?? new DeviceConfig();
        config.ConnectionAddress = connectionAddress;
        config.SlaveAddress = (byte)addressValue;
        config.DeviceBaudRate = baudRate;
        config.BaudRate = _serialPort.BaudRate;
        config.TemperatureCalibration = temperatureCalibration;
        config.HumidityCalibration = humidityCalibration;
        config.PressureCalibration = pressureCalibration;
        return config;
    }

    private static void ValidateReadCalibration(int value, string name)
    {
        if (value is < -999 or > 999) throw new IOException($"设备{name}校准寄存器返回超出范围的值 {value}。");
    }

    private static void ValidateWritableRegister(ushort registerAddress, ushort registerValue)
    {
        switch (registerAddress)
        {
            case ModbusRegisterMap.EnterBootloader:
                if (registerValue != 0x1234) throw new ArgumentOutOfRangeException(nameof(registerValue), "进入升级模式必须写入 0x1234。");
                break;
            case ModbusRegisterMap.TemperatureCalibration:
            case ModbusRegisterMap.HumidityCalibration:
            case ModbusRegisterMap.PressureCalibration:
                var calibration = ModbusRegisterMap.FromSignedRegister(registerValue);
                ValidateReadCalibration(calibration, "校准");
                break;
            case ModbusRegisterMap.DeviceAddress when registerValue is >= 1 and <= 247:
                break;
            case ModbusRegisterMap.DeviceBaudRate when registerValue <= 2:
                break;
            case ModbusRegisterMap.DeviceAddress:
                throw new ArgumentOutOfRangeException(nameof(registerValue), "设备地址必须在 1 到 247 之间。");
            case ModbusRegisterMap.DeviceBaudRate:
                throw new ArgumentOutOfRangeException(nameof(registerValue), "设备波特率编码只支持 0、1 或 2。");
            default:
                throw new ArgumentOutOfRangeException(nameof(registerAddress), $"寄存器 0x{registerAddress:X4} 不是点表定义的可写寄存器。");
        }
    }
}

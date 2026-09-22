using System.IO.Ports;
using System.Diagnostics;
using SHT85.Models;

namespace SHT85.Communication;

/// <summary>真实 RS-485 串口与 MODBUS RTU 传输层。</summary>
public sealed class SerialPortService : IDisposable
{
    private readonly object _syncRoot = new();
    private readonly YmodemSender _ymodemSender = new();
    private SerialPort? _serialPort;

    public bool IsOpen => _serialPort?.IsOpen == true;
    public int BaudRate => _serialPort?.BaudRate ?? 9600;

    public Stream BaseStream
    {
        get
        {
            if (_serialPort?.IsOpen != true) throw new InvalidOperationException("串口尚未打开。");
            return _serialPort.BaseStream;
        }
    }

    public static string[] GetPortNames() => SerialPort.GetPortNames()
        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public void Open(DeviceConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        Close();

        var parity = Enum.Parse<Parity>(config.Parity, ignoreCase: true);
        var stopBits = config.StopBits switch
        {
            1 => StopBits.One,
            2 => StopBits.Two,
            _ => throw new ArgumentOutOfRangeException(nameof(config.StopBits), "停止位只支持 1 或 2。")
        };

        var port = new SerialPort(config.PortName, config.BaudRate, parity, config.DataBits, stopBits)
        {
            Handshake = Handshake.None,
            ReadTimeout = 1500,
            WriteTimeout = 1500
        };

        try
        {
            port.Open();
            port.DiscardInBuffer();
            port.DiscardOutBuffer();
            _serialPort = port;
        }
        catch
        {
            port.Dispose();
            throw;
        }
    }

    public ushort[] ReadHoldingRegisters(byte slaveAddress, ushort startAddress, ushort quantity)
    {
        var request = ModbusRtuProtocol.BuildReadHoldingRegistersRequest(slaveAddress, startAddress, quantity);
        lock (_syncRoot)
        {
            var port = GetOpenPort();
            port.DiscardInBuffer();
            port.Write(request, 0, request.Length);

            var header = ReadResponseHeader(port, slaveAddress, ModbusRtuProtocol.ReadHoldingRegistersFunction);
            var responseLength = header[1] == (ModbusRtuProtocol.ReadHoldingRegistersFunction | 0x80)
                ? 5
                : quantity * 2 + 5;
            var response = new byte[responseLength];
            Array.Copy(header, response, header.Length);
            Array.Copy(ReadExactly(port, responseLength - header.Length), 0, response, header.Length, responseLength - header.Length);
            return ModbusRtuProtocol.ParseReadHoldingRegistersResponse(response, slaveAddress, quantity);
        }
    }

    public byte[] WriteSingleRegister(byte slaveAddress, ushort registerAddress, ushort registerValue)
    {
        var request = ModbusRtuProtocol.BuildWriteSingleRegisterRequest(slaveAddress, registerAddress, registerValue);
        lock (_syncRoot)
        {
            var port = GetOpenPort();
            port.DiscardInBuffer();
            port.Write(request, 0, request.Length);

            var header = ReadResponseHeader(port, slaveAddress, ModbusRtuProtocol.WriteSingleRegisterFunction);
            var responseLength = header[1] == (ModbusRtuProtocol.WriteSingleRegisterFunction | 0x80) ? 5 : 8;
            var response = new byte[responseLength];
            Array.Copy(header, response, header.Length);
            Array.Copy(ReadExactly(port, responseLength - header.Length), 0, response, header.Length, responseLength - header.Length);
            ModbusRtuProtocol.ValidateWriteSingleRegisterResponse(response, request);
            return request;
        }
    }

    /// <summary>
    /// 在 BOOT 固定的 9600/8N1 链路上监听升级等待信号 'C'。
    /// 未检测到时恢复调用前的波特率，避免影响 APP 扫描。
    /// </summary>
    public bool TryDetectBootloaderSignal(TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));

        lock (_syncRoot)
        {
            var port = GetOpenPort();
            var originalBaudRate = port.BaudRate;
            var originalReadTimeout = port.ReadTimeout;
            var detected = false;

            try
            {
                if (port.BaudRate != 9600) port.BaudRate = 9600;
                port.ReadTimeout = 100;
                port.DiscardInBuffer();

                var stopwatch = Stopwatch.StartNew();
                while (stopwatch.Elapsed < timeout)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        if (port.ReadByte() == 'C')
                        {
                            detected = true;
                            return true;
                        }
                    }
                    catch (TimeoutException)
                    {
                        // 轮询超时属于正常情况，继续等待 BOOT 的下一次 'C'。
                    }
                }

                return false;
            }
            finally
            {
                port.ReadTimeout = originalReadTimeout;
                if (!detected && port.IsOpen && port.BaudRate != originalBaudRate)
                {
                    port.BaudRate = originalBaudRate;
                }
            }
        }
    }

    public void SetBaudRate(int baudRate)
    {
        lock (_syncRoot)
        {
            GetOpenPort().BaudRate = baudRate;
        }
    }

    public void DiscardBuffers()
    {
        lock (_syncRoot)
        {
            var port = GetOpenPort();
            port.DiscardInBuffer();
            port.DiscardOutBuffer();
        }
    }

    /// <summary>
    /// 独占串口执行 YMODEM。使用 SerialPort 的有限 ReadTimeout，而不是可能在
    /// 通信线断开时长期挂起的 BaseStream.ReadAsync。
    /// </summary>
    public Task SendYmodemAsync(string firmwarePath, IProgress<int>? progress, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            lock (_syncRoot)
            {
                var port = GetOpenPort();
                var originalReadTimeout = port.ReadTimeout;
                var originalWriteTimeout = port.WriteTimeout;
                try
                {
                    // Every read wakes at most after 200 ms to observe cancellation.
                    port.ReadTimeout = 200;
                    port.WriteTimeout = 1500;
                    port.DiscardInBuffer();
                    port.DiscardOutBuffer();
                    _ymodemSender.Send(port, firmwarePath, progress, cancellationToken);
                }
                finally
                {
                    if (port.IsOpen)
                    {
                        port.ReadTimeout = originalReadTimeout;
                        port.WriteTimeout = originalWriteTimeout;
                    }
                }
            }
        });
    }

    public void Close()
    {
        lock (_syncRoot)
        {
            if (_serialPort is null) return;
            try
            {
                if (_serialPort.IsOpen) _serialPort.Close();
            }
            finally
            {
                _serialPort.Dispose();
                _serialPort = null;
            }
        }
    }

    public void Dispose() => Close();

    private SerialPort GetOpenPort()
    {
        if (_serialPort?.IsOpen != true) throw new InvalidOperationException("串口尚未打开。");
        return _serialPort;
    }

    private static byte[] ReadExactly(SerialPort port, int count)
    {
        var buffer = new byte[count];
        var offset = 0;
        while (offset < count)
        {
            var read = port.Read(buffer, offset, count - offset);
            if (read == 0) throw new IOException("串口已关闭，未收到完整的 MODBUS 响应。");
            offset += read;
        }

        return buffer;
    }

    private static byte[] ReadResponseHeader(SerialPort port, byte slaveAddress, byte function)
    {
        // APP 启动时会在 RS-485 总线上打印地址/波特率文本。扫描到从站+功能码，
        // 再按已知响应格式读完帧头，避免把这些启动文本当成 Modbus 报文。
        var timeout = Stopwatch.StartNew();
        var first = -1;
        while (timeout.Elapsed < TimeSpan.FromSeconds(5))
        {
            if (first < 0 || first != slaveAddress)
            {
                first = port.ReadByte();
                continue;
            }

            var second = port.ReadByte();
            if (second == function || second == (function | 0x80))
            {
                return new[] { (byte)first, (byte)second, (byte)port.ReadByte() };
            }

            first = second;
        }

        throw new IOException("等待 MODBUS 响应超时。");
    }
}

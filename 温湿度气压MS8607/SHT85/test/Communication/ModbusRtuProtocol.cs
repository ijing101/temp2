namespace test.Communication;

/// <summary>MODBUS RTU 功能码 0x03、0x06 报文构造和校验。</summary>
public static class ModbusRtuProtocol
{
    public const byte ReadHoldingRegistersFunction = 0x03;
    public const byte WriteSingleRegisterFunction = 0x06;

    public static byte[] BuildReadHoldingRegistersRequest(byte slaveAddress, ushort startAddress, ushort quantity)
    {
        if (slaveAddress == 0) throw new ArgumentOutOfRangeException(nameof(slaveAddress), "不能对广播地址执行读操作。");
        if (quantity is < 1 or > 125) throw new ArgumentOutOfRangeException(nameof(quantity), "一次最多读取 125 个保持寄存器。");

        var frame = new byte[8];
        frame[0] = slaveAddress;
        frame[1] = ReadHoldingRegistersFunction;
        frame[2] = (byte)(startAddress >> 8);
        frame[3] = (byte)startAddress;
        frame[4] = (byte)(quantity >> 8);
        frame[5] = (byte)quantity;
        AppendCrc(frame);
        return frame;
    }

    public static byte[] BuildWriteSingleRegisterRequest(byte slaveAddress, ushort registerAddress, ushort registerValue)
    {
        if (slaveAddress == 0) throw new ArgumentOutOfRangeException(nameof(slaveAddress), "不能对广播地址执行写操作。");

        var frame = new byte[8];
        frame[0] = slaveAddress;
        frame[1] = WriteSingleRegisterFunction;
        frame[2] = (byte)(registerAddress >> 8);
        frame[3] = (byte)registerAddress;
        frame[4] = (byte)(registerValue >> 8);
        frame[5] = (byte)registerValue;
        AppendCrc(frame);
        return frame;
    }

    public static ushort[] ParseReadHoldingRegistersResponse(ReadOnlySpan<byte> frame, byte slaveAddress, ushort quantity)
    {
        ValidateFrame(frame, slaveAddress, ReadHoldingRegistersFunction);

        if (frame[1] == (ReadHoldingRegistersFunction | 0x80))
        {
            ThrowDeviceException(frame[2]);
        }

        var expectedByteCount = quantity * 2;
        if (frame.Length != expectedByteCount + 5 || frame[2] != expectedByteCount)
        {
            throw new IOException($"MODBUS 返回长度错误：期望 {expectedByteCount} 个数据字节，收到 {frame[2]} 个。");
        }

        var values = new ushort[quantity];
        for (var i = 0; i < quantity; i++)
        {
            values[i] = (ushort)((frame[3 + i * 2] << 8) | frame[4 + i * 2]);
        }

        return values;
    }

    public static void ValidateWriteSingleRegisterResponse(ReadOnlySpan<byte> response, ReadOnlySpan<byte> request)
    {
        if (request.Length != 8) throw new ArgumentException("写单寄存器请求长度必须为 8 字节。", nameof(request));
        ValidateFrame(response, request[0], WriteSingleRegisterFunction);

        if (response[1] == (WriteSingleRegisterFunction | 0x80))
        {
            ThrowDeviceException(response[2]);
        }

        // 比较功能数据字段；CRC 字节序由 ValidateFrame 同时兼容两种格式。
        if (response.Length != 8 || !response[..6].SequenceEqual(request[..6]))
        {
            throw new IOException("MODBUS 写单寄存器响应与请求回显不一致。");
        }
    }

    public static ushort CalculateCrc16(ReadOnlySpan<byte> data)
    {
        ushort crc = 0xFFFF;
        foreach (var value in data)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (ushort)((crc & 1) != 0 ? (crc >> 1) ^ 0xA001 : crc >> 1);
            }
        }

        return crc;
    }

    private static void AppendCrc(Span<byte> frame)
    {
        var crc = CalculateCrc16(frame[..^2]);
        // 标准 RTU 在线序为 CRC 低字节优先。硬件源码的 CRC helper 返回值已交换字节，
        // 配合其收帧拼接方式后，线上仍与此顺序一致。
        frame[^2] = (byte)crc;
        frame[^1] = (byte)(crc >> 8);
    }

    private static void ValidateFrame(ReadOnlySpan<byte> frame, byte slaveAddress, byte function)
    {
        if (frame.Length < 5) throw new IOException("MODBUS 响应长度不足。");

        var actualCrcLittleEndian = (ushort)(frame[^2] | (frame[^1] << 8));
        var actualCrcBigEndian = (ushort)((frame[^2] << 8) | frame[^1]);
        var expectedCrc = CalculateCrc16(frame[..^2]);
        if (actualCrcLittleEndian != expectedCrc && actualCrcBigEndian != expectedCrc)
        {
            throw new IOException($"MODBUS 响应 CRC 错误：收到 0x{actualCrcLittleEndian:X4}（低字节在前）/0x{actualCrcBigEndian:X4}（高字节在前），期望 0x{expectedCrc:X4}。");
        }

        if (frame[0] != slaveAddress) throw new IOException($"MODBUS 从站地址不匹配：收到 {frame[0]}，期望 {slaveAddress}。");
        if (frame[1] != function && frame[1] != (function | 0x80))
        {
            throw new IOException($"MODBUS 功能码不匹配：收到 0x{frame[1]:X2}，期望 0x{function:X2}。");
        }

        if ((frame[1] & 0x80) != 0 && frame.Length != 5) throw new IOException("MODBUS 异常响应长度错误。");
    }

    private static void ThrowDeviceException(byte exceptionCode)
    {
        var description = exceptionCode switch
        {
            0x01 => "不支持的功能码",
            0x02 => "寄存器地址无效",
            0x03 => "寄存器数据无效",
            0x04 => "设备执行失败",
            _ => $"异常码 0x{exceptionCode:X2}"
        };

        throw new IOException($"设备返回 MODBUS 异常：{description}。");
    }
}

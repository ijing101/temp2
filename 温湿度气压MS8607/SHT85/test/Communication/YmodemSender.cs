using System.Diagnostics;
using System.IO.Ports;

namespace SHT85.Communication;

/// <summary>
/// YMODEM 发送端：CRC16、Block 0 文件信息、1024 字节数据包、EOT 结束握手。
/// 所有读取均使用 SerialPort.ReadByte 的有限超时，避免通信线断开时异步读取长期挂起。
/// </summary>
public sealed class YmodemSender
{
    private const byte Soh = 0x01;
    private const byte Stx = 0x02;
    private const byte Eot = 0x04;
    private const byte Ack = 0x06;
    private const byte Nak = 0x15;
    private const byte Can = 0x18;
    private const byte CrcRequest = 0x43;
    private const int BlockSize = 1024;
    private const int HeaderBlockSize = 128;
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(3);
    private const int PacketRetryCount = 3;

    // BOOT reserves one 21 KiB active slot and one 21 KiB rollback slot.
    public const long MaximumFirmwareSizeBytes = 0x5400 - 4;

    public void Send(SerialPort transport, string firmwarePath, IProgress<int>? progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentException.ThrowIfNullOrWhiteSpace(firmwarePath);
        cancellationToken.ThrowIfCancellationRequested();

        using var firmware = File.OpenRead(firmwarePath);
        var totalBytes = firmware.Length;
        if (totalBytes == 0 || totalBytes > MaximumFirmwareSizeBytes)
        {
            throw new IOException($"固件大小必须为 1–{MaximumFirmwareSizeBytes:N0} 字节；BOOT 使用 21 KiB APP 回滚分区，CRC 保存在升级元数据中。");
        }

        try
        {
            WaitFor(transport, CrcRequest, cancellationToken);
            SendPacketWithRetry(transport, 0, BuildHeader(FileName(firmwarePath), totalBytes), cancellationToken);
            WaitFor(transport, CrcRequest, cancellationToken);

            var blockNumber = (byte)1;
            var buffer = new byte[BlockSize];
            long sentBytes = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var read = firmware.Read(buffer, 0, BlockSize);
                if (read == 0) break;

                if (read < BlockSize)
                {
                    Array.Fill(buffer, (byte)0x1A, read, BlockSize - read);
                }

                SendPacketWithRetry(transport, blockNumber, buffer, cancellationToken);
                sentBytes += read;
                progress?.Report((int)(sentBytes * 100 / totalBytes));
                // 固件接收器使用 1..255 的包号并跳过 0，保持与现有协议兼容。
                blockNumber = blockNumber == byte.MaxValue ? (byte)1 : (byte)(blockNumber + 1);
            }

            EndTransfer(transport, cancellationToken);
            progress?.Report(100);
        }
        catch (OperationCanceledException)
        {
            SendCancel(transport);
            throw;
        }
        catch (IOException ex)
        {
            throw new IOException("YMODEM 通信已中断或设备无响应。请检查 RS-485 通信线和设备供电，然后重新进入 BOOT 升级。", ex);
        }
    }

    private static void EndTransfer(SerialPort transport, CancellationToken cancellationToken)
    {
        WriteControlAndExpect(transport, Eot, Nak, cancellationToken);
        WriteControlAndExpect(transport, Eot, Ack, cancellationToken);
        WaitFor(transport, CrcRequest, cancellationToken);
        SendPacketWithRetry(transport, 0, new byte[HeaderBlockSize], cancellationToken);
    }

    private static void SendPacketWithRetry(SerialPort transport, byte blockNumber, byte[] data, CancellationToken cancellationToken)
    {
        var packet = BuildPacket(blockNumber, data);
        for (var attempt = 0; attempt < PacketRetryCount; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Write(transport, packet);

            var response = ReadStageResponse(transport, Ack, cancellationToken);
            if (response == Ack) return;
            // NAK: resend the same packet. Any other response is ignored by ReadStageResponse.
        }

        throw new IOException("YMODEM 数据包连续 3 次被设备拒绝。");
    }

    private static void WriteControlAndExpect(SerialPort transport, byte control, byte expected, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Write(transport, new[] { control });
        var response = ReadStageResponse(transport, expected, cancellationToken);
        if (response != expected) throw new IOException($"YMODEM 结束握手失败：期望 0x{expected:X2}，收到 0x{response:X2}。");
    }

    private static void WaitFor(SerialPort transport, byte expected, CancellationToken cancellationToken)
    {
        var response = ReadStageResponse(transport, expected, cancellationToken);
        if (response != expected) throw new IOException($"YMODEM 握手失败：期望 0x{expected:X2}，收到 0x{response:X2}。");
    }

    private static byte ReadStageResponse(SerialPort transport, byte expected, CancellationToken cancellationToken)
    {
        var timeout = Stopwatch.StartNew();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int value;
            try
            {
                value = transport.ReadByte();
            }
            catch (TimeoutException)
            {
                if (timeout.Elapsed >= ResponseTimeout)
                {
                    throw new IOException("YMODEM 等待设备响应超时（3 秒）。通信线可能已断开。");
                }
                continue;
            }

            if (value < 0) throw new IOException("YMODEM 串口通信流已关闭。");
            if (value == Can) throw new IOException("接收端取消了 YMODEM 传输。");
            if (value == Ack || value == Nak)
            {
                if (value == expected) return (byte)value;
                if (expected == Ack && value == Nak) return (byte)value;
                continue;
            }

            // BOOT 的启动文本不是 YMODEM 响应；仅接受当前阶段需要的 C。
            if (value == CrcRequest && expected == CrcRequest) return (byte)value;
        }
    }

    private static void SendCancel(SerialPort transport)
    {
        try
        {
            // BOOT recognizes CAN during a transfer and immediately resumes its 'C' advertisement.
            Write(transport, new[] { Can, Can });
        }
        catch
        {
            // A disconnected link cannot receive CAN; BOOT's inactivity timeout will recover it.
        }
    }

    private static void Write(SerialPort transport, byte[] data)
    {
        transport.Write(data, 0, data.Length);
    }

    private static byte[] BuildHeader(string fileName, long length)
    {
        var data = new byte[HeaderBlockSize];
        var metadata = System.Text.Encoding.ASCII.GetBytes($"{fileName}\0{length}\0");
        Array.Copy(metadata, data, Math.Min(metadata.Length, data.Length));
        return data;
    }

    private static byte[] BuildPacket(byte blockNumber, byte[] data)
    {
        var header = data.Length switch
        {
            HeaderBlockSize => Soh,
            BlockSize => Stx,
            _ => throw new ArgumentException("YMODEM 数据块必须为 128 或 1024 字节。", nameof(data))
        };
        var packet = new byte[data.Length + 5];
        packet[0] = header;
        packet[1] = blockNumber;
        packet[2] = (byte)~blockNumber;
        Array.Copy(data, 0, packet, 3, data.Length);
        var crc = CalculateCrc16(data);
        packet[^2] = (byte)(crc >> 8);
        packet[^1] = (byte)crc;
        return packet;
    }

    private static ushort CalculateCrc16(byte[] data)
    {
        ushort crc = 0;
        foreach (var value in data)
        {
            crc ^= (ushort)(value << 8);
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1);
            }
        }

        return crc;
    }

    private static string FileName(string path) => Path.GetFileName(path);
}

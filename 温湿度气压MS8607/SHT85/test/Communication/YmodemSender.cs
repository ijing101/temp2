namespace test.Communication;

/// <summary>
/// YMODEM 发送端：CRC16、Block 0 文件信息、1024 字节数据包、EOT 结束握手。
/// 传入真实串口的 BaseStream 后即可用于 BOOT 程序升级。
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
    // BOOT reserves one 21 KiB active slot and one 21 KiB rollback slot.
    public const long MaximumFirmwareSizeBytes = 0x5400 - 4;

    public async Task SendAsync(Stream transport, string firmwarePath, IProgress<int>? progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentException.ThrowIfNullOrWhiteSpace(firmwarePath);

        await using var firmware = File.OpenRead(firmwarePath);
        var totalBytes = firmware.Length;
        if (totalBytes == 0 || totalBytes > MaximumFirmwareSizeBytes)
        {
            throw new IOException($"固件大小必须为 1–{MaximumFirmwareSizeBytes:N0} 字节；BOOT 使用 21 KiB APP 回滚分区，CRC 保存在升级元数据中。");
        }

        try
        {
            await WaitForAsync(transport, CrcRequest, cancellationToken);
            await SendPacketWithRetryAsync(transport, 0, BuildHeader(FileName(firmwarePath), totalBytes), cancellationToken);
            await WaitForAsync(transport, CrcRequest, cancellationToken);

            var blockNumber = (byte)1;
            var buffer = new byte[BlockSize];
            long sentBytes = 0;

            while (true)
            {
                var read = await firmware.ReadAsync(buffer.AsMemory(0, BlockSize), cancellationToken);
                if (read == 0) break;

                if (read < BlockSize)
                {
                    Array.Fill(buffer, (byte)0x1A, read, BlockSize - read);
                }

                await SendPacketWithRetryAsync(transport, blockNumber, buffer, cancellationToken);
                sentBytes += read;
                progress?.Report(totalBytes == 0 ? 100 : (int)(sentBytes * 100 / totalBytes));
                // 固件接收器使用 1..255 的包号并跳过 0，保持与现有协议兼容。
                blockNumber = blockNumber == byte.MaxValue ? (byte)1 : (byte)(blockNumber + 1);
            }

            await EndTransferAsync(transport, cancellationToken);
            progress?.Report(100);
        }
        catch (OperationCanceledException)
        {
            await SendCancelAsync(transport);
            throw;
        }
    }

    private static async Task EndTransferAsync(Stream transport, CancellationToken cancellationToken)
    {
        await WriteControlAndExpectAsync(transport, Eot, Nak, cancellationToken);
        await WriteControlAndExpectAsync(transport, Eot, Ack, cancellationToken);
        await WaitForAsync(transport, CrcRequest, cancellationToken);
        await SendPacketWithRetryAsync(transport, 0, new byte[HeaderBlockSize], cancellationToken);
    }

    private static async Task SendPacketWithRetryAsync(Stream transport, byte blockNumber, byte[] data, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var packet = BuildPacket(blockNumber, data);
            await transport.WriteAsync(packet, cancellationToken);
            await transport.FlushAsync(cancellationToken);

            var response = await ReadStageResponseAsync(transport, Ack, cancellationToken);
            if (response == Ack) return;
            if (response == Nak) continue;
        }

        throw new IOException("YMODEM 数据包重试次数已用尽。");
    }

    private static async Task WriteControlAndExpectAsync(Stream transport, byte control, byte expected, CancellationToken cancellationToken)
    {
        await transport.WriteAsync(new[] { control }, cancellationToken);
        await transport.FlushAsync(cancellationToken);
        var response = await ReadStageResponseAsync(transport, expected, cancellationToken);
        if (response != expected) throw new IOException($"YMODEM 结束握手失败：期望 0x{expected:X2}，收到 0x{response:X2}。");
    }

    private static async Task WaitForAsync(Stream transport, byte expected, CancellationToken cancellationToken)
    {
        var response = await ReadStageResponseAsync(transport, expected, cancellationToken);
        if (response != expected) throw new IOException($"YMODEM 握手失败：期望 0x{expected:X2}，收到 0x{response:X2}。");
    }

    private static async Task<byte> ReadStageResponseAsync(Stream transport, byte expected, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        var buffer = new byte[1];
        try
        {
            while (true)
            {
                var read = await transport.ReadAsync(buffer.AsMemory(0, 1), timeout.Token);
                if (read == 0) throw new IOException("YMODEM 通信流已关闭。");

                var value = buffer[0];
                if (value == Can) throw new IOException("接收端取消了 YMODEM 传输。");
                if (value == Ack || value == Nak)
                {
                    if (value == expected) return value;
                    if (expected == Ack && value == Nak) return value;
                    continue;
                }

                // BOOT 固件会在 RS-485 上输出 ASCII 调试文本，并每 500 ms 重发 'C'。
                // 文本不是协议响应；只接受本阶段需要的 C，其余字节继续扫描。
                if (value == CrcRequest && expected == CrcRequest) return value;
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new IOException("YMODEM 等待设备响应超时。");
        }
    }

    private static async Task SendCancelAsync(Stream transport)
    {
        try
        {
            await transport.WriteAsync(new[] { Can, Can }, CancellationToken.None);
            await transport.FlushAsync(CancellationToken.None);
        }
        catch
        {
            // 取消时尽力向设备发送 CAN，不覆盖原始取消异常。
        }
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

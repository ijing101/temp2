using test.Communication;

namespace test.Services;

public sealed class FirmwareUpgradeService
{
    public Task UpgradeWithYmodemAsync(DeviceService device, string firmwarePath, IProgress<int>? progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(device);
        return device.UpgradeWithYmodemAsync(firmwarePath, progress, cancellationToken);
    }
}

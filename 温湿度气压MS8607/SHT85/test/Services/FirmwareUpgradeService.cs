using test.Communication;

namespace test.Services;

public sealed class FirmwareUpgradeService
{
    private readonly YmodemSender _ymodemSender = new();

    public Task UpgradeWithYmodemAsync(Stream serialStream, string firmwarePath, IProgress<int>? progress, CancellationToken cancellationToken)
        => _ymodemSender.SendAsync(serialStream, firmwarePath, progress, cancellationToken);
}

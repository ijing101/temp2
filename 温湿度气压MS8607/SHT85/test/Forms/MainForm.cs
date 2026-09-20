using System.Globalization;
using test.Communication;
using test.Models;
using test.Services;

namespace test;

public partial class MainForm : Form
{
    private readonly DeviceService _deviceService = new();
    private readonly FirmwareUpgradeService _firmwareUpgradeService = new();
    private readonly LogService _logService = new();
    private CancellationTokenSource? _firmwareUpgradeCancellation;
    private CancellationTokenSource? _deviceConnectionScanCancellation;
    private Task? _deviceConnectionScanTask;
    private bool _firmwareTransferComplete;
    private bool _closeAfterFirmwareCancel;
    private bool _isClosing;

    public MainForm()
    {
        InitializeComponent();
        Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath)
            ?? System.Drawing.SystemIcons.Application;
        Load += MainForm_Load;
        FormClosing += MainForm_FormClosing;
        btnOpenPort.Click += btnOpenPort_Click;
        btnClosePort.Click += btnClosePort_Click;
        btnRefreshPorts.Click += btnRefreshPorts_Click;
        btnDeviceConnect.Click += btnDeviceConnect_Click;
        btnDeviceDisconnect.Click += btnDeviceDisconnect_Click;
        btnReadSlaveAddress.Click += btnReadSlaveAddress_Click;
        btnWriteSlaveAddress.Click += btnWriteSlaveAddress_Click;
        btnReadDeviceBaudRate.Click += btnReadDeviceBaudRate_Click;
        btnWriteDeviceBaudRate.Click += btnWriteDeviceBaudRate_Click;
        btnReadTemperatureCalibration.Click += btnReadTemperatureCalibration_Click;
        btnWriteTemperatureCalibration.Click += btnWriteTemperatureCalibration_Click;
        btnReadHumidityCalibration.Click += btnReadHumidityCalibration_Click;
        btnWriteHumidityCalibration.Click += btnWriteHumidityCalibration_Click;
        btnReadPressureCalibration.Click += btnReadPressureCalibration_Click;
        btnWritePressureCalibration.Click += btnWritePressureCalibration_Click;
        btnPollOnce.Click += btnPollOnce_Click;
        btnClearLog.Click += btnClearLog_Click;
        btnReadFirmwareVersion.Click += btnReadFirmwareVersion_Click;
        btnEnterBoot.Click += btnEnterBoot_Click;
        btnSelectFirmware.Click += btnSelectFirmware_Click;
        btnStartFirmware.Click += btnStartFirmware_Click;
        btnCancelFirmware.Click += btnCancelFirmware_Click;
        chkAutoRefresh.CheckedChanged += chkAutoRefresh_CheckedChanged;
        numRefreshInterval.ValueChanged += numRefreshInterval_ValueChanged;
        uiTimer.Tick += uiTimer_Tick;
        _logService.MessageLogged += LogService_MessageLogged;
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        RefreshPortList(logResult: false);
        cmbBaudRate.Items.AddRange(new object[] { 9600, 19200, 115200 });
        cmbDeviceBaudRate.Items.AddRange(new object[] { 9600, 19200, 115200 });
        cmbParity.Items.Add("None");
        cmbDataBits.Items.Add(8);
        cmbStopBits.Items.Add(1);

        cmbBaudRate.SelectedItem = 9600;
        cmbDeviceBaudRate.SelectedItem = 9600;
        cmbParity.SelectedItem = "None";
        cmbDataBits.SelectedItem = 8;
        cmbStopBits.SelectedItem = 1;
        _logService.Info("程序已启动。");
        UpdateConnectionState(false);
    }

    private void btnRefreshPorts_Click(object? sender, EventArgs e)
    {
        if (_deviceService.IsPortOpen) return;
        RefreshPortList(logResult: true);
    }

    private void RefreshPortList(bool logResult)
    {
        if (_deviceService.IsPortOpen) return;

        var previousPort = cmbPort.SelectedItem?.ToString();
        var ports = DeviceService.GetPortNames();
        var items = ports.Length > 0 ? ports : new[] { "COM3" };

        cmbPort.BeginUpdate();
        try
        {
            cmbPort.Items.Clear();
            cmbPort.Items.AddRange(items);
        }
        finally
        {
            cmbPort.EndUpdate();
        }

        cmbPort.SelectedItem = previousPort is not null && items.Contains(previousPort, StringComparer.OrdinalIgnoreCase)
            ? items.First(port => string.Equals(port, previousPort, StringComparison.OrdinalIgnoreCase))
            : items.Contains("COM3", StringComparer.OrdinalIgnoreCase)
                ? items.First(port => string.Equals(port, "COM3", StringComparison.OrdinalIgnoreCase))
                : items[0];

        if (logResult)
        {
            _logService.Info("端口列表已更新。");
        }
    }

    private void btnOpenPort_Click(object? sender, EventArgs e)
    {
        if (!TryReadCommunicationConfig(out var config)) return;

        try
        {
            _deviceService.OpenPort(config);
            UpdateConnectionState(false);
            _logService.Info("串口已打开。");
        }
        catch (Exception ex)
        {
            ReportCommunicationError("打开串口", ex);
        }
    }

    private void btnClosePort_Click(object? sender, EventArgs e)
    {
        if (_deviceConnectionScanTask is not null)
        {
            ShowInputError("正在扫描设备，请先点击“断开”停止扫描；串口仍保持打开。");
            return;
        }

        try
        {
            _deviceService.ClosePort();
            UpdateConnectionState(false);
            _logService.Info("串口已关闭。");
        }
        catch (Exception ex)
        {
            ReportCommunicationError("关闭串口", ex);
        }
    }

    private async void btnDeviceConnect_Click(object? sender, EventArgs e)
    {
        if (!_deviceService.IsPortOpen)
        {
            ShowInputError("请先打开串口。");
            return;
        }

        if (!TryReadConnectionAddress(out var address)) return;

        await StartDeviceConnectionScanAsync(address);
    }

    private async Task StartDeviceConnectionScanAsync(byte address)
    {
        if (_deviceConnectionScanTask is not null) return;

        var cancellation = new CancellationTokenSource();
        _deviceConnectionScanCancellation = cancellation;
        _deviceConnectionScanTask = ScanForDeviceAsync(address, cancellation.Token);
        UpdateConnectionState(false);

        try
        {
            await _deviceConnectionScanTask;
        }
        finally
        {
            if (ReferenceEquals(_deviceConnectionScanCancellation, cancellation))
            {
                _deviceConnectionScanCancellation = null;
                _deviceConnectionScanTask = null;
                cancellation.Dispose();
            }

            if (!_isClosing && _deviceService.IsPortOpen)
            {
                UpdateConnectionState(_deviceService.IsConnected);
            }
        }
    }

    private async void btnDeviceDisconnect_Click(object? sender, EventArgs e)
    {
        var wasScanning = _deviceConnectionScanTask is not null;
        try
        {
            await StopDeviceConnectionScanAsync();
            _deviceService.DisconnectDevice();
            UpdateConnectionState(false);
            _logService.Info(wasScanning
                ? "扫描已停止。"
                : "设备已断开。");
        }
        catch (Exception ex)
        {
            ReportCommunicationError("停止设备扫描", ex);
        }
    }

    private void btnReadSlaveAddress_Click(object? sender, EventArgs e)
    {
        if (!EnsureConnected()) return;
        try
        {
            var config = _deviceService.ReadConfig();
            DisplayDeviceConfig(config);
            _logService.Info("设备地址已读取。");
        }
        catch (Exception ex) { ReportCommunicationError("读取设备地址", ex); }
    }

    private void btnWriteSlaveAddress_Click(object? sender, EventArgs e)
    {
        if (!EnsureConnected()) return;
        if (!byte.TryParse(txtSlaveAddress.Text, out var address) || address is < 1 or > 247)
        {
            ShowInputError("硬件固件接受的设备地址范围是 1 到 247。");
            txtSlaveAddress.Focus();
            return;
        }

        try
        {
            _deviceService.WriteSingleRegister(ModbusRegisterMap.DeviceAddress, address);
            txtCurrentSlaveAddress.Text = address.ToString();
            txtConnectionAddress.Text = address.ToString();
            UpdateConnectionState(_deviceService.IsConnected);
            LogSingleRegisterWrite("设备地址");
        }
        catch (Exception ex) { ReportCommunicationError("写入设备地址", ex); }
    }

    private void btnReadDeviceBaudRate_Click(object? sender, EventArgs e)
    {
        if (!EnsureConnected()) return;
        try
        {
            var config = _deviceService.ReadConfig();
            DisplayDeviceConfig(config);
            _logService.Info("设备波特率已读取。");
        }
        catch (Exception ex) { ReportCommunicationError("读取设备波特率", ex); }
    }

    private void btnWriteDeviceBaudRate_Click(object? sender, EventArgs e)
    {
        if (!EnsureConnected()) return;
        try
        {
            var baudRate = Convert.ToInt32(cmbDeviceBaudRate.SelectedItem ?? 9600);
            var baudCode = ModbusRegisterMap.ToBaudCode(baudRate);
            _deviceService.WriteSingleRegister(ModbusRegisterMap.DeviceBaudRate, baudCode);
            txtCurrentDeviceBaudRate.Text = baudRate.ToString();
            cmbBaudRate.SelectedItem = baudRate;
            LogSingleRegisterWrite("设备波特率");
            _logService.Info("设备波特率已更新。");
        }
        catch (Exception ex) { ReportCommunicationError("写入设备波特率", ex); }
    }

    private void btnReadTemperatureCalibration_Click(object? sender, EventArgs e)
        => ReadCalibration(txtCurrentTemperatureCalibration, "温度", config => config.TemperatureCalibration);

    private void btnReadHumidityCalibration_Click(object? sender, EventArgs e)
        => ReadCalibration(txtCurrentHumidityCalibration, "湿度", config => config.HumidityCalibration);

    private void btnReadPressureCalibration_Click(object? sender, EventArgs e)
        => ReadCalibration(txtCurrentPressureCalibration, "气压", config => config.PressureCalibration);

    private void btnWriteTemperatureCalibration_Click(object? sender, EventArgs e)
        => WriteCalibration(txtTemperatureCalibration, txtCurrentTemperatureCalibration, "温度", ModbusRegisterMap.TemperatureCalibration);

    private void btnWriteHumidityCalibration_Click(object? sender, EventArgs e)
        => WriteCalibration(txtHumidityCalibration, txtCurrentHumidityCalibration, "湿度", ModbusRegisterMap.HumidityCalibration);

    private void btnWritePressureCalibration_Click(object? sender, EventArgs e)
        => WriteCalibration(txtPressureCalibration, txtCurrentPressureCalibration, "气压", ModbusRegisterMap.PressureCalibration);

    private void ReadCalibration(TextBox currentValueTextBox, string metricName, Func<DeviceConfig, int> valueSelector)
    {
        if (!EnsureConnected()) return;
        try
        {
            currentValueTextBox.Text = FormatCalibration(valueSelector(_deviceService.ReadConfig()));
            _logService.Info($"{metricName}校准已读取。");
        }
        catch (Exception ex) { ReportCommunicationError($"读取{metricName}校准值", ex); }
    }

    private void WriteCalibration(TextBox writeValueTextBox, TextBox currentValueTextBox, string metricName, ushort registerAddress)
    {
        if (!EnsureConnected() || !TryReadCalibration(writeValueTextBox, metricName, out var calibration)) return;

        try
        {
            var registerValue = ModbusRegisterMap.ToSignedRegister(calibration, nameof(calibration));
            _deviceService.WriteSingleRegister(registerAddress, registerValue);
            currentValueTextBox.Text = FormatCalibration(calibration);
            LogSingleRegisterWrite($"{metricName}校准");
        }
        catch (Exception ex) { ReportCommunicationError($"写入{metricName}校准值", ex); }
    }

    private void LogSingleRegisterWrite(string itemName)
    {
        _logService.Info($"{itemName}已写入。");
    }

    private void btnPollOnce_Click(object? sender, EventArgs e)
    {
        if (EnsureConnected()) PollDeviceData(showSuccessLog: true);
    }

    private void btnReadFirmwareVersion_Click(object? sender, EventArgs e)
    {
        if (!EnsureConnected()) return;

        try
        {
            var version = _deviceService.ReadAppVersion();
            lblFirmwareVersionValue.Text = version.ToString();
            _logService.Info("固件版本已读取。");
        }
        catch (Exception ex) { ReportCommunicationError("读取 APP 程序版本", ex); }
    }

    private void btnClearLog_Click(object? sender, EventArgs e)
    {
        txtRuntimeLog.Clear();
        _logService.Info("日志已清空。");
    }

    private void chkAutoRefresh_CheckedChanged(object? sender, EventArgs e) => UpdateRefreshTimer();

    private void numRefreshInterval_ValueChanged(object? sender, EventArgs e) => UpdateRefreshTimer();

    private void UpdateRefreshTimer()
    {
        uiTimer.Interval = checked((int)numRefreshInterval.Value * 1000);
        if (chkAutoRefresh.Checked && _deviceService.IsConnected && _deviceService.DeviceMode == DeviceMode.Application && _firmwareUpgradeCancellation is null) uiTimer.Start();
        else uiTimer.Stop();
    }

    private void uiTimer_Tick(object? sender, EventArgs e)
    {
        if (_deviceService.IsConnected && _deviceService.DeviceMode == DeviceMode.Application && chkAutoRefresh.Checked) PollDeviceData();
        lblStatusTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _isClosing = true;
        _deviceConnectionScanCancellation?.Cancel();

        if (_firmwareUpgradeCancellation is not null)
        {
            e.Cancel = true;
            if (_firmwareTransferComplete)
            {
                _logService.Info("设备正在重启，请稍候。");
            }
            else
            {
                _closeAfterFirmwareCancel = true;
                _firmwareUpgradeCancellation.Cancel();
            }
            return;
        }

        uiTimer.Stop();
        _deviceService.Dispose();
    }

    private void btnEnterBoot_Click(object? sender, EventArgs e)
    {
        if (!EnsureConnected()) return;
        if (string.IsNullOrWhiteSpace(txtFirmwarePath.Text) || !File.Exists(txtFirmwarePath.Text))
        {
            ShowInputError("请先选择有效的 .bin 固件，再切换到 BOOT，避免设备等待升级超时。");
            return;
        }

        try
        {
            if (!_deviceService.EnterBootMode())
            {
                ShowInputError("无法切换到 BOOT 模式，请检查设备是否已连接。");
                return;
            }

            cmbBaudRate.SelectedItem = 9600;
            UpdateRefreshTimer();
            UpdateConnectionState(_deviceService.IsConnected);
            _logService.Info("设备已进入升级模式。");
            UpdateFirmwareUpgradeUi();
        }
        catch (Exception ex) { ReportCommunicationError("切换 BOOT 模式", ex); }
    }

    private void btnSelectFirmware_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "选择 APP 固件",
            Filter = "BOOT 支持的固件 (*.bin)|*.bin|所有文件 (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var firmwareSize = new FileInfo(dialog.FileName).Length;
        if (firmwareSize == 0 || firmwareSize > YmodemSender.MaximumFirmwareSizeBytes)
        {
            ShowInputError($"BOOT 固件大小必须为 1–{YmodemSender.MaximumFirmwareSizeBytes:N0} 字节（21 KiB APP 回滚分区，CRC 保存在升级元数据中）。");
            return;
        }

        txtFirmwarePath.Text = dialog.FileName;
        progressFirmware.Value = 0;
        _logService.Info("已选择固件。");
        UpdateFirmwareUpgradeUi();
    }

    private async void btnStartFirmware_Click(object? sender, EventArgs e)
    {
        if (_deviceService.DeviceMode != DeviceMode.Bootloader)
        {
            ShowInputError("只有 BOOT 模式可以执行 APP 固件升级。");
            return;
        }

        if (string.IsNullOrWhiteSpace(txtFirmwarePath.Text) || !File.Exists(txtFirmwarePath.Text))
        {
            ShowInputError("请先选择有效的 APP 固件文件。");
            return;
        }

        _firmwareTransferComplete = false;
        _firmwareUpgradeCancellation = new CancellationTokenSource();
        var progress = new Progress<int>(value =>
        {
            progressFirmware.Value = Math.Clamp(value, 0, 100);
            lblFirmwareProgress.Text = $"升级进度 {progressFirmware.Value}%";
        });

        SetFirmwareUpgradeRunning(true);
        _logService.Info("正在升级固件。");
        var closePortAfterFailure = false;
        Exception? upgradeFailure = null;

        try
        {
            await _firmwareUpgradeService.UpgradeWithYmodemAsync(
                _deviceService,
                txtFirmwarePath.Text,
                progress,
                _firmwareUpgradeCancellation.Token);
            _firmwareTransferComplete = true;
            UpdateFirmwareUpgradeUi();
            _logService.Info("固件发送完成，正在等待设备重启。");
            var config = await _deviceService.CompleteFirmwareUpgradeAsync();
            if (config is not null)
            {
                DisplayDeviceConfig(config);
                txtConnectionAddress.Text = config.ConnectionAddress.ToString();
                cmbBaudRate.SelectedItem = config.DeviceBaudRate;
                UpdateConnectionState(_deviceService.IsConnected);
                _logService.Info("固件升级完成。");
            }
            else
            {
                UpdateConnectionState(false);
                _logService.Info("固件升级完成，正在自动连接 APP。");
                if (TryReadConnectionAddress(out var address))
                {
                    await StartDeviceConnectionScanAsync(address);
                }
            }
        }
        catch (OperationCanceledException)
        {
            closePortAfterFailure = true;
            _logService.Info("固件升级已取消。");
        }
        catch (Exception ex)
        {
            closePortAfterFailure = true;
            upgradeFailure = ex;
            _logService.Info("固件升级失败。");
        }
        finally
        {
            if (closePortAfterFailure && _deviceService.IsPortOpen)
            {
                try
                {
                    _deviceService.ClosePort();
                    UpdateConnectionState(false);
                    _logService.Info("升级已中止，串口已关闭。请检查通信线后重新打开串口。");
                }
                catch (Exception ex)
                {
                    _logService.Info($"升级中止后关闭串口失败：{ex.Message}");
                }
            }

            _firmwareUpgradeCancellation.Dispose();
            _firmwareUpgradeCancellation = null;
            _firmwareTransferComplete = false;
            SetFirmwareUpgradeRunning(false);
            UpdateFirmwareUpgradeUi();
            UpdateRefreshTimer();
            if (upgradeFailure is not null)
            {
                MessageBox.Show(upgradeFailure.Message, "固件升级失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (_closeAfterFirmwareCancel)
            {
                _closeAfterFirmwareCancel = false;
                Close();
            }
        }
    }

    private void btnCancelFirmware_Click(object? sender, EventArgs e)
    {
        if (_firmwareUpgradeCancellation is null || _firmwareTransferComplete) return;

        btnCancelFirmware.Enabled = false;
        _logService.Info("正在取消固件升级并等待串口操作结束。请勿断电。");
        _firmwareUpgradeCancellation.Cancel();
    }

    private void PollDeviceData(bool showSuccessLog = false)
    {
        try
        {
            var data = _deviceService.ReadData();
            cardTemperature.SetValue(data.Temperature);
            cardHumidity.SetValue(data.Humidity);
            cardPressure.SetValue(data.Pressure, "0.0");
            lblStatusTime.Text = data.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
            if (showSuccessLog) AppendRuntimeLog("数据采集成功。");
        }
        catch (Exception)
        {
            AppendRuntimeLog("数据采集失败。");
        }
    }

    private bool TryReadCommunicationConfig(out DeviceConfig config)
    {
        config = new DeviceConfig();
        if (!TryReadConnectionAddress(out var connectionAddress)) return false;

        config = new DeviceConfig
        {
            PortName = cmbPort.SelectedItem?.ToString() ?? "COM3",
            BaudRate = Convert.ToInt32(cmbBaudRate.SelectedItem ?? 9600),
            Parity = "None",
            DataBits = 8,
            StopBits = 1,
            ConnectionAddress = connectionAddress,
            SlaveAddress = connectionAddress,
            DeviceBaudRate = 9600
        };
        return true;
    }

    private bool TryReadConnectionAddress(out byte address)
    {
        if (byte.TryParse(txtConnectionAddress.Text, out address) && address is >= 1 and <= 247) return true;

        ShowInputError("硬件固件的连接地址范围是 1 到 247；0 是广播地址，不能用于连接。");
        txtConnectionAddress.Focus();
        return false;
    }

    private static bool TryReadCalibration(TextBox textBox, string metricName, out int calibration)
    {
        calibration = 0;
        var text = textBox.Text.Trim();
        var numberStyles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
        var parsed = decimal.TryParse(text, numberStyles, CultureInfo.InvariantCulture, out var value)
            || decimal.TryParse(text, numberStyles, CultureInfo.CurrentCulture, out value);
        var scaledValue = value * 10m;

        if (parsed
            && value is >= -99.9m and <= 99.9m
            && scaledValue == decimal.Truncate(scaledValue))
        {
            calibration = checked((int)scaledValue);
            return true;
        }

        MessageBox.Show($"{metricName}校准值必须在 -99.9 到 99.9 之间，且最多保留 1 位小数。", "输入检查", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        textBox.Focus();
        return false;
    }

    private static string FormatCalibration(int value)
        => (value / 10m).ToString("0.0", CultureInfo.InvariantCulture);

    private bool EnsureConnected()
    {
        if (_deviceService.IsConnected && _deviceService.DeviceMode == DeviceMode.Application) return true;
        ShowInputError("请先连接处于 APP 模式的设备。");
        return false;
    }

    private void DisplayDeviceConfig(DeviceConfig config)
    {
        txtCurrentSlaveAddress.Text = config.SlaveAddress.ToString();
        txtCurrentDeviceBaudRate.Text = config.DeviceBaudRate.ToString();
        cmbDeviceBaudRate.SelectedItem = config.DeviceBaudRate;
        txtCurrentTemperatureCalibration.Text = FormatCalibration(config.TemperatureCalibration);
        txtCurrentHumidityCalibration.Text = FormatCalibration(config.HumidityCalibration);
        txtCurrentPressureCalibration.Text = FormatCalibration(config.PressureCalibration);
    }

    private void UpdateConnectionState(bool connected)
    {
        var portOpen = _deviceService.IsPortOpen;
        var applicationConnected = connected && _deviceService.DeviceMode == DeviceMode.Application;
        var bootloaderConnected = connected && _deviceService.DeviceMode == DeviceMode.Bootloader;
        var isScanning = _deviceConnectionScanCancellation is not null;

        btnOpenPort.Enabled = !portOpen;
        btnClosePort.Enabled = portOpen && !isScanning;
        btnRefreshPorts.Enabled = !portOpen;
        btnDeviceConnect.Enabled = portOpen && !connected && !isScanning;
        btnDeviceDisconnect.Enabled = connected || isScanning;
        txtConnectionAddress.Enabled = !connected;
        cmbPort.Enabled = !portOpen;
        cmbBaudRate.Enabled = !portOpen;
        cmbParity.Enabled = !portOpen;
        cmbDataBits.Enabled = !portOpen;
        cmbStopBits.Enabled = !portOpen;
        btnReadSlaveAddress.Enabled = applicationConnected;
        btnWriteSlaveAddress.Enabled = applicationConnected;
        btnReadDeviceBaudRate.Enabled = applicationConnected;
        btnWriteDeviceBaudRate.Enabled = applicationConnected;
        btnReadTemperatureCalibration.Enabled = applicationConnected;
        btnWriteTemperatureCalibration.Enabled = applicationConnected;
        btnReadHumidityCalibration.Enabled = applicationConnected;
        btnWriteHumidityCalibration.Enabled = applicationConnected;
        btnReadPressureCalibration.Enabled = applicationConnected;
        btnWritePressureCalibration.Enabled = applicationConnected;
        btnPollOnce.Enabled = applicationConnected;
        lblConnectionState.Text = connected
            ? bootloaderConnected ? "● BOOT 已连接" : $"● 设备已连接（地址 {txtConnectionAddress.Text}）"
            : portOpen
                ? isScanning ? "● 正在扫描设备" : "● 串口已打开，等待连接设备"
                : "● 串口未打开";
        lblConnectionState.ForeColor = connected ? Color.FromArgb(134, 239, 172) : Color.FromArgb(253, 186, 116);
        lblStatus.Text = connected
            ? bootloaderConnected ? "状态：BOOT 已连接，等待升级" : $"状态：设备已连接，地址 {txtConnectionAddress.Text}"
            : portOpen
                ? isScanning ? "状态：正在扫描设备" : "状态：串口已打开，设备未连接"
                : "状态：串口未打开";

        UpdateRefreshTimer();
        UpdateFirmwareUpgradeUi();
    }

    private async Task ScanForDeviceAsync(byte address, CancellationToken cancellationToken)
    {
        var selectedBaudRate = Convert.ToInt32(cmbBaudRate.SelectedItem ?? 9600);
        var scanBaudRates = new[] { selectedBaudRate, 9600, 19200, 115200 }.Distinct();

        while (!cancellationToken.IsCancellationRequested && _deviceService.IsPortOpen && !_deviceService.IsConnected)
        {
            foreach (var baudRate in scanBaudRates)
            {
                if (cancellationToken.IsCancellationRequested || !_deviceService.IsPortOpen || _deviceService.IsConnected) return;

                try
                {
                    _deviceService.SetPortBaudRate(baudRate);

                    // BOOT 固定以 9600 波特率周期发送 'C'，先检测它，避免被 APP 的
                    // Modbus 读取逻辑消耗掉，从而支持未连接 APP 时直接升级。
                    var bootloaderDetected = await Task.Run(
                        () => _deviceService.TryDetectBootloader(TimeSpan.FromMilliseconds(800), cancellationToken),
                        cancellationToken);
                    if (bootloaderDetected)
                    {
                        _deviceService.MarkBootloaderConnected();
                        if (cancellationToken.IsCancellationRequested || !_deviceService.IsPortOpen) return;

                        UpdateConnectionState(true);
                        _logService.Info("BOOT 已连接。");
                        return;
                    }

                    // ConnectDevice 包含同步串口读操作，放到后台线程避免扫描期间卡住界面。
                    var config = await Task.Run(() => _deviceService.ConnectDevice(address), cancellationToken);
                    if (cancellationToken.IsCancellationRequested || !_deviceService.IsPortOpen) return;

                    cmbBaudRate.SelectedItem = _deviceService.BaudRate;
                    DisplayDeviceConfig(config);
                    UpdateConnectionState(true);
                    _logService.Info("设备已连接。");
                    PollDeviceData(showSuccessLog: true);
                    return;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception)
                {
                    if (cancellationToken.IsCancellationRequested || !_deviceService.IsPortOpen) return;
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private async Task StopDeviceConnectionScanAsync()
    {
        var cancellation = _deviceConnectionScanCancellation;
        var scanTask = _deviceConnectionScanTask;
        if (cancellation is null || scanTask is null) return;

        cancellation.Cancel();
        await scanTask;
    }

    private void UpdateFirmwareUpgradeUi()
    {
        var isUpgrading = _firmwareUpgradeCancellation is not null;
        var isConnected = _deviceService.IsConnected;
        var isBootMode = _deviceService.DeviceMode == DeviceMode.Bootloader;
        var isApplicationMode = _deviceService.DeviceMode == DeviceMode.Application;
        var hasFirmware = !string.IsNullOrWhiteSpace(txtFirmwarePath.Text) && File.Exists(txtFirmwarePath.Text);

        lblFirmwareVersionValue.Text = _deviceService.AppVersion?.ToString() ?? "未读取";
        var isScanning = _deviceConnectionScanTask is not null;
        lblFirmwareModeValue.Text = isApplicationMode
            ? "APP 模式"
            : _firmwareTransferComplete && isScanning
                ? "正在连接 APP"
                : _firmwareTransferComplete
                    ? "BOOT 已完成，等待 APP"
                    : _deviceService.DeviceMode switch
        {
            DeviceMode.Bootloader => "BOOT 模式",
            DeviceMode.Application => "APP 模式",
            _ => "未连接"
        };
        lblFirmwareModeValue.ForeColor = isBootMode
            ? Color.FromArgb(134, 239, 172)
            : Color.FromArgb(253, 186, 116);

        btnReadFirmwareVersion.Enabled = isConnected && isApplicationMode && !isUpgrading;
        btnEnterBoot.Enabled = isConnected && hasFirmware && !isBootMode && !isUpgrading;
        // 固件可以在未连接设备时提前选择，便于 BOOT 设备直接升级。
        btnSelectFirmware.Enabled = !isUpgrading;
        btnStartFirmware.Enabled = isConnected && isBootMode && hasFirmware && !isUpgrading;
        btnCancelFirmware.Enabled = isUpgrading && !_firmwareTransferComplete;
        txtFirmwarePath.Enabled = !isUpgrading;
    }

    private void SetFirmwareUpgradeRunning(bool isUpgrading)
    {
        if (isUpgrading) uiTimer.Stop();
        var applicationConnected = _deviceService.IsConnected && _deviceService.DeviceMode == DeviceMode.Application;
        btnOpenPort.Enabled = !isUpgrading && !_deviceService.IsPortOpen;
        btnClosePort.Enabled = !isUpgrading && _deviceService.IsPortOpen && _deviceConnectionScanCancellation is null;
        btnRefreshPorts.Enabled = !isUpgrading && !_deviceService.IsPortOpen;
        btnDeviceConnect.Enabled = !isUpgrading && _deviceService.IsPortOpen && !_deviceService.IsConnected;
        btnDeviceDisconnect.Enabled = !isUpgrading && (_deviceService.IsConnected || _deviceConnectionScanCancellation is not null);
        btnReadSlaveAddress.Enabled = !isUpgrading && applicationConnected;
        btnWriteSlaveAddress.Enabled = !isUpgrading && applicationConnected;
        btnReadDeviceBaudRate.Enabled = !isUpgrading && applicationConnected;
        btnWriteDeviceBaudRate.Enabled = !isUpgrading && applicationConnected;
        btnReadTemperatureCalibration.Enabled = !isUpgrading && applicationConnected;
        btnWriteTemperatureCalibration.Enabled = !isUpgrading && applicationConnected;
        btnReadHumidityCalibration.Enabled = !isUpgrading && applicationConnected;
        btnWriteHumidityCalibration.Enabled = !isUpgrading && applicationConnected;
        btnReadPressureCalibration.Enabled = !isUpgrading && applicationConnected;
        btnWritePressureCalibration.Enabled = !isUpgrading && applicationConnected;
        btnPollOnce.Enabled = !isUpgrading && applicationConnected;
        chkAutoRefresh.Enabled = !isUpgrading;
        btnClearLog.Enabled = !isUpgrading;
        UpdateFirmwareUpgradeUi();
    }

    private void LogService_MessageLogged(object? sender, string message) => AppendRuntimeLog(message);

    private void AppendRuntimeLog(string message)
    {
        txtRuntimeLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        txtRuntimeLog.ScrollToCaret();
    }

    private void ReportCommunicationError(string operation, Exception exception)
    {
        _logService.Info($"{operation}失败。");
        MessageBox.Show(exception.Message, operation + "失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private static void ShowInputError(string message)
    {
        MessageBox.Show(message, "输入检查", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}

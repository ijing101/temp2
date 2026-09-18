using test.Controls;

namespace test;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;

    // ==================== 窗体和布局容器 ====================
    #region 窗体和布局容器
    private Panel panelHeader = null!;
    private Label lblTitle = null!;
    private Label lblSubtitle = null!;
    private Label lblConnectionState = null!;
    private Panel panelMain = null!;
    private TableLayoutPanel tblWorkspace = null!;
    private Panel panelLeft = null!;
    private Panel panelCenter = null!;
    private Panel panelRight = null!;
    #endregion

    // ==================== 通讯参数区域 ====================
    #region 通讯参数区域
    private GroupBox grpCommunication = null!;
    private TableLayoutPanel tblCommunication = null!;
    private Label lblConnectionAddress = null!;
    private Label lblPort = null!;
    private Label lblBaudRate = null!;
    private Label lblParity = null!;
    private Label lblDataBits = null!;
    private Label lblStopBits = null!;
    private TextBox txtConnectionAddress = null!;
    private ComboBox cmbPort = null!;
    private ComboBox cmbBaudRate = null!;
    private ComboBox cmbParity = null!;
    private ComboBox cmbDataBits = null!;
    private ComboBox cmbStopBits = null!;
    private TableLayoutPanel tblCommunicationActions = null!;
    private Button btnRefreshPorts = null!;
    private Button btnOpenPort = null!;
    private Button btnClosePort = null!;
    #endregion

    // ==================== 设备参数和数据校准区域 ====================
    #region 设备参数和数据校准区域
    private GroupBox grpDeviceParameters = null!;
    private TableLayoutPanel tblDeviceParameters = null!;
    private Label lblSlaveAddress = null!;
    private Label lblDeviceBaudRate = null!;
    private TextBox txtSlaveAddress = null!;
    private TextBox txtCurrentSlaveAddress = null!;
    private ComboBox cmbDeviceBaudRate = null!;
    private TextBox txtCurrentDeviceBaudRate = null!;
    private Button btnReadSlaveAddress = null!;
    private Button btnWriteSlaveAddress = null!;
    private Button btnReadDeviceBaudRate = null!;
    private Button btnWriteDeviceBaudRate = null!;
    private FlowLayoutPanel flowDeviceConnectionActions = null!;
    private Button btnDeviceConnect = null!;
    private Button btnDeviceDisconnect = null!;
    private GroupBox grpDataCalibration = null!;
    private TableLayoutPanel tblDataCalibration = null!;
    private Label lblTemperatureCalibration = null!;
    private Label lblHumidityCalibration = null!;
    private Label lblPressureCalibration = null!;
    private TextBox txtTemperatureCalibration = null!;
    private TextBox txtHumidityCalibration = null!;
    private TextBox txtPressureCalibration = null!;
    private TextBox txtCurrentTemperatureCalibration = null!;
    private TextBox txtCurrentHumidityCalibration = null!;
    private TextBox txtCurrentPressureCalibration = null!;
    private Button btnReadTemperatureCalibration = null!;
    private Button btnWriteTemperatureCalibration = null!;
    private Button btnReadHumidityCalibration = null!;
    private Button btnWriteHumidityCalibration = null!;
    private Button btnReadPressureCalibration = null!;
    private Button btnWritePressureCalibration = null!;
    #endregion

    // ==================== 固件升级区域 ====================
    #region 固件升级区域
    private GroupBox grpFirmwareUpgrade = null!;
    private Panel panelFirmwareUpgrade = null!;
    private TableLayoutPanel tblFirmwareUpgrade = null!;
    private Label lblFirmwareTarget = null!;
    private Label lblFirmwareVersionValue = null!;
    private Panel panelFirmwareVersion = null!;
    private Button btnReadFirmwareVersion = null!;
    private Label lblFirmwareMode = null!;
    private Label lblFirmwareModeValue = null!;
    private Label lblFirmwareFile = null!;
    private Panel panelFirmwareFile = null!;
    private TextBox txtFirmwarePath = null!;
    private Button btnSelectFirmware = null!;
    private Label lblFirmwareProgress = null!;
    private ProgressBar progressFirmware = null!;
    private TableLayoutPanel flowFirmwareActions = null!;
    private Button btnEnterBoot = null!;
    private Button btnStartFirmware = null!;
    #endregion

    // ==================== 实时监控、采集控制和日志区域 ====================
    #region 实时监控、采集控制和日志区域
    private GroupBox grpDataMonitoring = null!;
    private TableLayoutPanel tblMetrics = null!;
    private MetricCard cardTemperature = null!;
    private MetricCard cardHumidity = null!;
    private MetricCard cardPressure = null!;
    private GroupBox grpOperations = null!;
    private FlowLayoutPanel flowOperations = null!;
    private Button btnPollOnce = null!;
    private CheckBox chkAutoRefresh = null!;
    private NumericUpDown numRefreshInterval = null!;
    private Label lblRefreshIntervalUnit = null!;
    private Button btnClearLog = null!;
    private GroupBox grpRuntimeLog = null!;
    private RichTextBox txtRuntimeLog = null!;
    #endregion

    // ==================== 状态栏和定时器 ====================
    #region 状态栏和定时器
    private StatusStrip statusStripMain = null!;
    private ToolStripStatusLabel lblStatus = null!;
    private ToolStripStatusLabel lblStatusTime = null!;
    private System.Windows.Forms.Timer uiTimer = null!;
    #endregion

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        // 注意：这是 WinForms 设计器文件。
        // 学习时重点看“创建控件 → 设置属性 → 加入父容器”的顺序。
        // 业务逻辑请写在 MainForm.cs，避免设计器重新生成时被覆盖。
        components = new System.ComponentModel.Container();

        // ---------- 1. 创建控件实例 ----------
        // 先 new 控件，后面才能设置属性、添加子控件和安排布局。
        panelHeader = new Panel();
        lblTitle = new Label();
        lblSubtitle = new Label();
        lblConnectionState = new Label();
        panelMain = new Panel();
        tblWorkspace = new TableLayoutPanel();
        panelLeft = new Panel();
        panelCenter = new Panel();
        panelRight = new Panel();
        grpCommunication = new GroupBox();
        tblCommunication = new TableLayoutPanel();
        lblConnectionAddress = new Label();
        lblPort = new Label();
        lblBaudRate = new Label();
        lblParity = new Label();
        lblDataBits = new Label();
        lblStopBits = new Label();
        txtConnectionAddress = new TextBox();
        cmbPort = new ComboBox();
        cmbBaudRate = new ComboBox();
        cmbParity = new ComboBox();
        cmbDataBits = new ComboBox();
        cmbStopBits = new ComboBox();
        tblCommunicationActions = new TableLayoutPanel();
        btnRefreshPorts = new Button();
        btnOpenPort = new Button();
        btnClosePort = new Button();
        grpDeviceParameters = new GroupBox();
        tblDeviceParameters = new TableLayoutPanel();
        lblSlaveAddress = new Label();
        lblDeviceBaudRate = new Label();
        txtSlaveAddress = new TextBox();
        txtCurrentSlaveAddress = new TextBox();
        cmbDeviceBaudRate = new ComboBox();
        txtCurrentDeviceBaudRate = new TextBox();
        btnReadSlaveAddress = new Button();
        btnWriteSlaveAddress = new Button();
        btnReadDeviceBaudRate = new Button();
        btnWriteDeviceBaudRate = new Button();
        flowDeviceConnectionActions = new FlowLayoutPanel();
        btnDeviceConnect = new Button();
        btnDeviceDisconnect = new Button();
        grpDataCalibration = new GroupBox();
        tblDataCalibration = new TableLayoutPanel();
        lblTemperatureCalibration = new Label();
        lblHumidityCalibration = new Label();
        lblPressureCalibration = new Label();
        txtTemperatureCalibration = new TextBox();
        txtHumidityCalibration = new TextBox();
        txtPressureCalibration = new TextBox();
        txtCurrentTemperatureCalibration = new TextBox();
        txtCurrentHumidityCalibration = new TextBox();
        txtCurrentPressureCalibration = new TextBox();
        btnReadTemperatureCalibration = new Button();
        btnWriteTemperatureCalibration = new Button();
        btnReadHumidityCalibration = new Button();
        btnWriteHumidityCalibration = new Button();
        btnReadPressureCalibration = new Button();
        btnWritePressureCalibration = new Button();
        grpFirmwareUpgrade = new GroupBox();
        panelFirmwareUpgrade = new Panel();
        tblFirmwareUpgrade = new TableLayoutPanel();
        lblFirmwareTarget = new Label();
        lblFirmwareVersionValue = new Label();
        panelFirmwareVersion = new Panel();
        btnReadFirmwareVersion = new Button();
        lblFirmwareMode = new Label();
        lblFirmwareModeValue = new Label();
        lblFirmwareFile = new Label();
        panelFirmwareFile = new Panel();
        txtFirmwarePath = new TextBox();
        btnSelectFirmware = new Button();
        lblFirmwareProgress = new Label();
        progressFirmware = new ProgressBar();
        flowFirmwareActions = new TableLayoutPanel();
        btnEnterBoot = new Button();
        btnStartFirmware = new Button();
        grpDataMonitoring = new GroupBox();
        tblMetrics = new TableLayoutPanel();
        cardTemperature = new MetricCard("温度", "℃", Color.FromArgb(220, 38, 38));
        cardHumidity = new MetricCard("湿度", "%RH", Color.FromArgb(2, 132, 199));
        cardPressure = new MetricCard("气压", "hPa", Color.FromArgb(124, 58, 237));
        grpOperations = new GroupBox();
        flowOperations = new FlowLayoutPanel();
        btnPollOnce = new Button();
        chkAutoRefresh = new CheckBox();
        numRefreshInterval = new NumericUpDown();
        lblRefreshIntervalUnit = new Label();
        btnClearLog = new Button();
        grpRuntimeLog = new GroupBox();
        txtRuntimeLog = new RichTextBox();
        statusStripMain = new StatusStrip();
        lblStatus = new ToolStripStatusLabel();
        lblStatusTime = new ToolStripStatusLabel();
        uiTimer = new System.Windows.Forms.Timer(components);

        panelHeader.SuspendLayout();
        panelMain.SuspendLayout();
        tblWorkspace.SuspendLayout();
        panelLeft.SuspendLayout();
        panelCenter.SuspendLayout();
        panelRight.SuspendLayout();
        grpCommunication.SuspendLayout();
        tblCommunication.SuspendLayout();
        tblCommunicationActions.SuspendLayout();
        grpDeviceParameters.SuspendLayout();
        tblDeviceParameters.SuspendLayout();
        flowDeviceConnectionActions.SuspendLayout();
        grpDataCalibration.SuspendLayout();
        tblDataCalibration.SuspendLayout();
        grpFirmwareUpgrade.SuspendLayout();
        panelFirmwareUpgrade.SuspendLayout();
        tblFirmwareUpgrade.SuspendLayout();
        panelFirmwareFile.SuspendLayout();
        flowFirmwareActions.SuspendLayout();
        grpDataMonitoring.SuspendLayout();
        tblMetrics.SuspendLayout();
        grpOperations.SuspendLayout();
        flowOperations.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numRefreshInterval).BeginInit();
        grpRuntimeLog.SuspendLayout();
        statusStripMain.SuspendLayout();
        SuspendLayout();

        // ==================== 2. 窗体级布局 ====================
        #region 窗体级布局
        // Header: Dock Top 固定高度，窗口变宽时只扩展宽度，不影响主工作区的高度。
        panelHeader.BackColor = Color.FromArgb(30, 41, 59);
        panelHeader.Controls.Add(lblConnectionState);
        //panelHeader.Controls.Add(lblSubtitle);
        panelHeader.Controls.Add(lblTitle);
        panelHeader.Dock = DockStyle.Top;
        panelHeader.Padding = new Padding(16, 10, 16, 8);
        panelHeader.Size = new Size(1184, 80);
        panelHeader.Name = "panelHeader";

        lblTitle.Dock = DockStyle.Top;
        lblTitle.AutoSize = false;
        lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        lblTitle.ForeColor = Color.White;
        lblTitle.Height = 34;
        lblTitle.Name = "lblTitle";
        lblTitle.Text = "温湿度配置工具";
        lblTitle.TextAlign = ContentAlignment.MiddleLeft;

        //lblSubtitle.Dock = DockStyle.Top;
        //lblSubtitle.AutoSize = false;
        //lblSubtitle.Font = new Font("Segoe UI", 9F);
        //lblSubtitle.ForeColor = Color.FromArgb(203, 213, 225);
        //lblSubtitle.Height = 24;
        //lblSubtitle.Name = "lblSubtitle";
        //lblSubtitle.Text = "Industrial Device Debugger  ·  MODBUS RTU 实时监测与配置";
        //lblSubtitle.TextAlign = ContentAlignment.MiddleLeft;

        lblConnectionState.Dock = DockStyle.Right;
        lblConnectionState.AutoSize = false;
        lblConnectionState.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblConnectionState.ForeColor = Color.FromArgb(134, 239, 172);
        lblConnectionState.Name = "lblConnectionState";
        lblConnectionState.Size = new Size(220, 62);
        lblConnectionState.Text = "● 未连接";
        lblConnectionState.TextAlign = ContentAlignment.MiddleRight;

        // Main: Dock Fill 承接剩余空间，Padding 负责页面四周留白。
        panelMain.BackColor = Color.FromArgb(241, 245, 249);
        panelMain.Controls.Add(tblWorkspace);
        panelMain.Dock = DockStyle.Fill;
        panelMain.Name = "panelMain";
        panelMain.Padding = new Padding(12);

        // 三栏工作区：通讯/升级、设备/校准、监控/日志。
        tblWorkspace.ColumnCount = 3;
        tblWorkspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340F));
        tblWorkspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350F));
        tblWorkspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblWorkspace.Controls.Add(panelLeft, 0, 0);
        tblWorkspace.Controls.Add(panelCenter, 1, 0);
        tblWorkspace.Controls.Add(panelRight, 2, 0);
        tblWorkspace.Dock = DockStyle.Fill;
        tblWorkspace.Name = "tblWorkspace";
        tblWorkspace.RowCount = 1;
        tblWorkspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        panelLeft.Controls.Add(grpFirmwareUpgrade);
        panelLeft.Controls.Add(grpCommunication);
        panelLeft.Dock = DockStyle.Fill;
        panelLeft.Name = "panelLeft";
        panelLeft.Padding = new Padding(0, 0, 6, 0);

        panelCenter.Controls.Add(grpDataCalibration);
        panelCenter.Controls.Add(grpDeviceParameters);
        panelCenter.Dock = DockStyle.Fill;
        panelCenter.Name = "panelCenter";
        panelCenter.Padding = new Padding(6, 0, 6, 0);

        panelRight.Controls.Add(grpRuntimeLog);
        panelRight.Controls.Add(grpOperations);
        panelRight.Controls.Add(grpDataMonitoring);
        panelRight.Dock = DockStyle.Fill;
        panelRight.Name = "panelRight";
        panelRight.Padding = new Padding(6, 0, 0, 0);
        #endregion

        // ==================== 3. 通讯参数区域（左侧上方） ====================
        #region 通讯参数区域
        // Communication: TableLayoutPanel 让标签列和输入列自动对齐。
        grpCommunication.Controls.Add(tblCommunication);
        grpCommunication.Dock = DockStyle.Top;
        grpCommunication.Height = 276;
        grpCommunication.Name = "grpCommunication";
        grpCommunication.Padding = new Padding(10, 20, 10, 10);
        grpCommunication.Text = "通讯参数";
        grpCommunication.Margin = new Padding(0, 0, 0, 8);

        tblCommunication.ColumnCount = 2;
        tblCommunication.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
        tblCommunication.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64F));
        tblCommunication.Controls.Add(lblConnectionAddress, 0, 0);
        tblCommunication.Controls.Add(txtConnectionAddress, 1, 0);
        tblCommunication.Controls.Add(lblPort, 0, 1);
        tblCommunication.Controls.Add(cmbPort, 1, 1);
        tblCommunication.Controls.Add(lblBaudRate, 0, 2);
        tblCommunication.Controls.Add(cmbBaudRate, 1, 2);
        tblCommunication.Controls.Add(lblParity, 0, 3);
        tblCommunication.Controls.Add(cmbParity, 1, 3);
        tblCommunication.Controls.Add(lblDataBits, 0, 4);
        tblCommunication.Controls.Add(cmbDataBits, 1, 4);
        tblCommunication.Controls.Add(lblStopBits, 0, 5);
        tblCommunication.Controls.Add(cmbStopBits, 1, 5);
        tblCommunication.Controls.Add(tblCommunicationActions, 0, 6);
        tblCommunication.SetColumnSpan(tblCommunicationActions, 2);
        tblCommunication.Dock = DockStyle.Fill;
        tblCommunication.Name = "tblCommunication";
        tblCommunication.RowCount = 7;
        for (var i = 0; i < 6; i++) tblCommunication.RowStyles.Add(new RowStyle(SizeType.Absolute, 31F));
        tblCommunication.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        ConfigureFieldLabel(lblConnectionAddress, "地址");
        ConfigureFieldLabel(lblPort, "串口");
        ConfigureFieldLabel(lblBaudRate, "波特率");
        ConfigureFieldLabel(lblParity, "校验");
        ConfigureFieldLabel(lblDataBits, "数据位");
        ConfigureFieldLabel(lblStopBits, "停止位");
        ConfigureTextBox(txtConnectionAddress, "txtConnectionAddress", "1");
        txtConnectionAddress.PlaceholderText = "1–247";
        ConfigureCombo(cmbPort, "cmbPort");
        ConfigureCombo(cmbBaudRate, "cmbBaudRate");
        ConfigureCombo(cmbParity, "cmbParity");
        ConfigureCombo(cmbDataBits, "cmbDataBits");
        ConfigureCombo(cmbStopBits, "cmbStopBits");

        tblCommunicationActions.ColumnCount = 3;
        tblCommunicationActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        tblCommunicationActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        tblCommunicationActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.334F));
        tblCommunicationActions.Controls.Add(btnRefreshPorts, 0, 0);
        tblCommunicationActions.Controls.Add(btnOpenPort, 1, 0);
        tblCommunicationActions.Controls.Add(btnClosePort, 2, 0);
        tblCommunicationActions.Dock = DockStyle.Fill;
        tblCommunicationActions.Name = "tblCommunicationActions";
        tblCommunicationActions.RowCount = 1;
        tblCommunicationActions.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        ConfigureButton(btnRefreshPorts, "btnRefreshPorts", "更新端口", Color.FromArgb(37, 99, 235));
        ConfigureButton(btnOpenPort, "btnOpenPort", "打开", Color.FromArgb(22, 163, 74));
        ConfigureButton(btnClosePort, "btnClosePort", "关闭", Color.FromArgb(100, 116, 139));
        ConfigureCommunicationButton(btnRefreshPorts);
        ConfigureCommunicationButton(btnOpenPort);
        ConfigureCommunicationButton(btnClosePort);
        btnClosePort.Enabled = false;
        #endregion

        // ==================== 4. 设备参数区域（中间上方） ====================
        #region 设备参数区域
        // Device parameters: 地址与设备波特率独立放在中间栏，避免与连接参数混在一起。
        grpDeviceParameters.Controls.Add(tblDeviceParameters);
        grpDeviceParameters.Dock = DockStyle.Top;
        grpDeviceParameters.Height = 276;
        grpDeviceParameters.Name = "grpDeviceParameters";
        grpDeviceParameters.Padding = new Padding(10, 20, 10, 10);
        grpDeviceParameters.Text = "参数设置";
        grpDeviceParameters.Margin = new Padding(0, 0, 0, 8);

        tblDeviceParameters.ColumnCount = 5;
        tblDeviceParameters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
        tblDeviceParameters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        tblDeviceParameters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));
        tblDeviceParameters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23F));
        tblDeviceParameters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
        tblDeviceParameters.Controls.Add(lblSlaveAddress, 0, 0);
        tblDeviceParameters.Controls.Add(txtCurrentSlaveAddress, 1, 0);
        tblDeviceParameters.Controls.Add(btnReadSlaveAddress, 2, 0);
        tblDeviceParameters.Controls.Add(txtSlaveAddress, 3, 0);
        tblDeviceParameters.Controls.Add(btnWriteSlaveAddress, 4, 0);
        tblDeviceParameters.Controls.Add(lblDeviceBaudRate, 0, 1);
        tblDeviceParameters.Controls.Add(txtCurrentDeviceBaudRate, 1, 1);
        tblDeviceParameters.Controls.Add(btnReadDeviceBaudRate, 2, 1);
        tblDeviceParameters.Controls.Add(cmbDeviceBaudRate, 3, 1);
        tblDeviceParameters.Controls.Add(btnWriteDeviceBaudRate, 4, 1);
        tblDeviceParameters.Controls.Add(flowDeviceConnectionActions, 0, 2);
        tblDeviceParameters.SetColumnSpan(flowDeviceConnectionActions, 5);
        tblDeviceParameters.Dock = DockStyle.Fill;
        tblDeviceParameters.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
        tblDeviceParameters.Name = "tblDeviceParameters";
        tblDeviceParameters.RowCount = 3;
        tblDeviceParameters.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        tblDeviceParameters.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        tblDeviceParameters.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        ConfigureFieldLabel(lblSlaveAddress, "设备地址");
        ConfigureFieldLabel(lblDeviceBaudRate, "设备波特率");
        ConfigureRegisterTextBox(txtSlaveAddress, "txtSlaveAddress", "1", false);
        ConfigureRegisterTextBox(txtCurrentSlaveAddress, "txtCurrentSlaveAddress", "--", true);
        ConfigureCombo(cmbDeviceBaudRate, "cmbDeviceBaudRate");
        cmbDeviceBaudRate.Dock = DockStyle.Fill;
        cmbDeviceBaudRate.Margin = new Padding(2, 5, 2, 5);
        ConfigureRegisterTextBox(txtCurrentDeviceBaudRate, "txtCurrentDeviceBaudRate", "--", true);
        ConfigureSmallButton(btnReadSlaveAddress, "btnReadSlaveAddress", "读", Color.FromArgb(37, 99, 235));
        ConfigureSmallButton(btnWriteSlaveAddress, "btnWriteSlaveAddress", "写", Color.FromArgb(124, 58, 237));
        ConfigureSmallButton(btnReadDeviceBaudRate, "btnReadDeviceBaudRate", "读", Color.FromArgb(37, 99, 235));
        ConfigureSmallButton(btnWriteDeviceBaudRate, "btnWriteDeviceBaudRate", "写", Color.FromArgb(124, 58, 237));

        flowDeviceConnectionActions.Controls.Add(btnDeviceConnect);
        flowDeviceConnectionActions.Controls.Add(btnDeviceDisconnect);
        flowDeviceConnectionActions.Dock = DockStyle.Top;
        flowDeviceConnectionActions.Name = "flowDeviceConnectionActions";
        flowDeviceConnectionActions.Padding = new Padding(0, 8, 0, 0);
        flowDeviceConnectionActions.WrapContents = false;
        ConfigureButton(btnDeviceConnect, "btnDeviceConnect", "连接", Color.FromArgb(22, 163, 74));
        ConfigureButton(btnDeviceDisconnect, "btnDeviceDisconnect", "断开", Color.FromArgb(100, 116, 139));
        btnDeviceConnect.Enabled = false;
        btnDeviceDisconnect.Enabled = false;
        #endregion

        // ==================== 5. 数据校准区域（中间下方） ====================
        #region 数据校准区域
        // Data calibration: 三个采集量分别校准，读取和写入与设备参数分开。
        grpDataCalibration.Controls.Add(tblDataCalibration);
        grpDataCalibration.Dock = DockStyle.Fill;
        grpDataCalibration.Name = "grpDataCalibration";
        grpDataCalibration.Padding = new Padding(10, 20, 10, 10);
        grpDataCalibration.Text = "数据校准";

        tblDataCalibration.ColumnCount = 5;
        tblDataCalibration.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
        tblDataCalibration.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        tblDataCalibration.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));
        tblDataCalibration.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23F));
        tblDataCalibration.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
        tblDataCalibration.Controls.Add(lblTemperatureCalibration, 0, 0);
        tblDataCalibration.Controls.Add(txtCurrentTemperatureCalibration, 1, 0);
        tblDataCalibration.Controls.Add(btnReadTemperatureCalibration, 2, 0);
        tblDataCalibration.Controls.Add(txtTemperatureCalibration, 3, 0);
        tblDataCalibration.Controls.Add(btnWriteTemperatureCalibration, 4, 0);
        tblDataCalibration.Controls.Add(lblHumidityCalibration, 0, 1);
        tblDataCalibration.Controls.Add(txtCurrentHumidityCalibration, 1, 1);
        tblDataCalibration.Controls.Add(btnReadHumidityCalibration, 2, 1);
        tblDataCalibration.Controls.Add(txtHumidityCalibration, 3, 1);
        tblDataCalibration.Controls.Add(btnWriteHumidityCalibration, 4, 1);
        tblDataCalibration.Controls.Add(lblPressureCalibration, 0, 2);
        tblDataCalibration.Controls.Add(txtCurrentPressureCalibration, 1, 2);
        tblDataCalibration.Controls.Add(btnReadPressureCalibration, 2, 2);
        tblDataCalibration.Controls.Add(txtPressureCalibration, 3, 2);
        tblDataCalibration.Controls.Add(btnWritePressureCalibration, 4, 2);
        tblDataCalibration.Dock = DockStyle.Top;
        tblDataCalibration.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
        tblDataCalibration.Height = 114;
        tblDataCalibration.Name = "tblDataCalibration";
        tblDataCalibration.RowCount = 3;
        tblDataCalibration.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        tblDataCalibration.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        tblDataCalibration.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        ConfigureFieldLabel(lblTemperatureCalibration, "温度校准");
        ConfigureFieldLabel(lblHumidityCalibration, "湿度校准");
        ConfigureFieldLabel(lblPressureCalibration, "气压校准");
        ConfigureRegisterTextBox(txtTemperatureCalibration, "txtTemperatureCalibration", "0.0", false);
        ConfigureRegisterTextBox(txtHumidityCalibration, "txtHumidityCalibration", "0.0", false);
        ConfigureRegisterTextBox(txtPressureCalibration, "txtPressureCalibration", "0.0", false);
        ConfigureRegisterTextBox(txtCurrentTemperatureCalibration, "txtCurrentTemperatureCalibration", "--", true);
        ConfigureRegisterTextBox(txtCurrentHumidityCalibration, "txtCurrentHumidityCalibration", "--", true);
        ConfigureRegisterTextBox(txtCurrentPressureCalibration, "txtCurrentPressureCalibration", "--", true);
        ConfigureSmallButton(btnReadTemperatureCalibration, "btnReadTemperatureCalibration", "读", Color.FromArgb(37, 99, 235));
        ConfigureSmallButton(btnWriteTemperatureCalibration, "btnWriteTemperatureCalibration", "写", Color.FromArgb(124, 58, 237));
        ConfigureSmallButton(btnReadHumidityCalibration, "btnReadHumidityCalibration", "读", Color.FromArgb(37, 99, 235));
        ConfigureSmallButton(btnWriteHumidityCalibration, "btnWriteHumidityCalibration", "写", Color.FromArgb(124, 58, 237));
        ConfigureSmallButton(btnReadPressureCalibration, "btnReadPressureCalibration", "读", Color.FromArgb(37, 99, 235));
        ConfigureSmallButton(btnWritePressureCalibration, "btnWritePressureCalibration", "写", Color.FromArgb(124, 58, 237));
        #endregion

        // ==================== 6. 固件升级区域（左侧下方） ====================
        #region 固件升级区域
        // Firmware upgrade: 固件选择、升级进度和操作统一放在深色面板中。
        grpFirmwareUpgrade.Controls.Add(panelFirmwareUpgrade);
        grpFirmwareUpgrade.Dock = DockStyle.Fill;
        grpFirmwareUpgrade.Name = "grpFirmwareUpgrade";
        grpFirmwareUpgrade.Padding = new Padding(10, 20, 10, 10);
        grpFirmwareUpgrade.Text = "固件升级";

        panelFirmwareUpgrade.BackColor = Color.FromArgb(15, 23, 42);
        panelFirmwareUpgrade.Controls.Add(tblFirmwareUpgrade);
        panelFirmwareUpgrade.Dock = DockStyle.Fill;
        panelFirmwareUpgrade.Name = "panelFirmwareUpgrade";
        panelFirmwareUpgrade.Padding = new Padding(10);

        tblFirmwareUpgrade.ColumnCount = 2;
        tblFirmwareUpgrade.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 74F));
        tblFirmwareUpgrade.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblFirmwareUpgrade.Controls.Add(lblFirmwareTarget, 0, 0);
        tblFirmwareUpgrade.Controls.Add(panelFirmwareVersion, 1, 0);
        tblFirmwareUpgrade.Controls.Add(lblFirmwareMode, 0, 1);
        tblFirmwareUpgrade.Controls.Add(lblFirmwareModeValue, 1, 1);
        tblFirmwareUpgrade.Controls.Add(lblFirmwareFile, 0, 2);
        tblFirmwareUpgrade.Controls.Add(panelFirmwareFile, 1, 2);
        tblFirmwareUpgrade.Controls.Add(lblFirmwareProgress, 0, 3);
        tblFirmwareUpgrade.Controls.Add(progressFirmware, 1, 3);
        tblFirmwareUpgrade.Controls.Add(flowFirmwareActions, 0, 4);
        tblFirmwareUpgrade.SetColumnSpan(flowFirmwareActions, 2);
        tblFirmwareUpgrade.Dock = DockStyle.Fill;
        tblFirmwareUpgrade.Name = "tblFirmwareUpgrade";
        tblFirmwareUpgrade.RowCount = 5;
        tblFirmwareUpgrade.RowStyles.Add(new RowStyle(SizeType.Absolute, 31F));
        tblFirmwareUpgrade.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        tblFirmwareUpgrade.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        tblFirmwareUpgrade.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        tblFirmwareUpgrade.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        ConfigureFirmwareLabel(lblFirmwareTarget, "版本");
        ConfigureFirmwareLabel(lblFirmwareMode, "模式");
        ConfigureFirmwareLabel(lblFirmwareFile, "APP固件");
        ConfigureFirmwareLabel(lblFirmwareProgress, "进度");

        panelFirmwareVersion.Controls.Add(lblFirmwareVersionValue);
        panelFirmwareVersion.Controls.Add(btnReadFirmwareVersion);
        panelFirmwareVersion.Dock = DockStyle.Fill;
        panelFirmwareVersion.Name = "panelFirmwareVersion";

        lblFirmwareVersionValue.Dock = DockStyle.Fill;
        lblFirmwareVersionValue.ForeColor = Color.FromArgb(226, 232, 240);
        lblFirmwareVersionValue.Name = "lblFirmwareVersionValue";
        lblFirmwareVersionValue.Padding = new Padding(0, 0, 6, 0);
        lblFirmwareVersionValue.Text = "未读取";
        lblFirmwareVersionValue.TextAlign = ContentAlignment.MiddleLeft;

        btnReadFirmwareVersion.BackColor = Color.FromArgb(37, 99, 235);
        btnReadFirmwareVersion.Dock = DockStyle.Right;
        btnReadFirmwareVersion.FlatAppearance.BorderSize = 0;
        btnReadFirmwareVersion.FlatStyle = FlatStyle.Flat;
        btnReadFirmwareVersion.ForeColor = Color.White;
        btnReadFirmwareVersion.Margin = new Padding(2, 3, 0, 3);
        btnReadFirmwareVersion.Name = "btnReadFirmwareVersion";
        btnReadFirmwareVersion.Text = "读取";
        btnReadFirmwareVersion.UseVisualStyleBackColor = false;
        btnReadFirmwareVersion.Width = 56;

        lblFirmwareModeValue.Dock = DockStyle.Fill;
        lblFirmwareModeValue.ForeColor = Color.FromArgb(253, 186, 116);
        lblFirmwareModeValue.Name = "lblFirmwareModeValue";
        lblFirmwareModeValue.Text = "未连接";
        lblFirmwareModeValue.TextAlign = ContentAlignment.MiddleLeft;

        panelFirmwareFile.Controls.Add(txtFirmwarePath);
        panelFirmwareFile.Controls.Add(btnSelectFirmware);
        panelFirmwareFile.Dock = DockStyle.Fill;
        panelFirmwareFile.Name = "panelFirmwareFile";
        btnSelectFirmware.Dock = DockStyle.Right;
        btnSelectFirmware.FlatStyle = FlatStyle.Flat;
        btnSelectFirmware.ForeColor = Color.White;
        btnSelectFirmware.BackColor = Color.FromArgb(37, 99, 235);
        btnSelectFirmware.FlatAppearance.BorderSize = 0;
        btnSelectFirmware.Name = "btnSelectFirmware";
        btnSelectFirmware.Text = "文件";
        btnSelectFirmware.Width = 56;
        btnSelectFirmware.UseVisualStyleBackColor = false;
        txtFirmwarePath.BackColor = Color.FromArgb(30, 41, 59);
        txtFirmwarePath.BorderStyle = BorderStyle.FixedSingle;
        txtFirmwarePath.Dock = DockStyle.Fill;
        txtFirmwarePath.ForeColor = Color.FromArgb(226, 232, 240);
        txtFirmwarePath.Name = "txtFirmwarePath";
        txtFirmwarePath.PlaceholderText = "先选择bin 再切换BOOT并尽快升级";
        txtFirmwarePath.ReadOnly = true;

        progressFirmware.Dock = DockStyle.Fill;
        progressFirmware.Name = "progressFirmware";
        progressFirmware.Style = ProgressBarStyle.Continuous;

        flowFirmwareActions.ColumnCount = 2;
        flowFirmwareActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        flowFirmwareActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        flowFirmwareActions.RowCount = 1;
        flowFirmwareActions.RowStyles.Clear();
        flowFirmwareActions.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        flowFirmwareActions.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
        flowFirmwareActions.Controls.Add(btnEnterBoot, 0, 0);
        flowFirmwareActions.Controls.Add(btnStartFirmware, 1, 0);
        // 操作栏仅横向拉伸；高度固定，不能随固件区域或窗口高度增长。
        flowFirmwareActions.Dock = DockStyle.Top;
        flowFirmwareActions.Height = 44;
        flowFirmwareActions.Margin = new Padding(0);
        flowFirmwareActions.Name = "flowFirmwareActions";
        flowFirmwareActions.Padding = new Padding(0, 8, 0, 0);
        // 固件操作使用两等分网格，始终保持单行并铺满升级区域。
        ConfigureButton(btnEnterBoot, "btnEnterBoot", "切换BOOT", Color.FromArgb(217, 119, 6));
        ConfigureButton(btnStartFirmware, "btnStartFirmware", "升级", Color.FromArgb(124, 58, 237));
        btnEnterBoot.Font = new Font("Segoe UI", 8F);
        btnEnterBoot.AutoSize = false;//不根据文字自动调整按钮大小
        btnEnterBoot.Dock = DockStyle.Fill;//按钮填满它所在的父容器单元格
        btnEnterBoot.MinimumSize = new Size(0, 32);//按钮最小尺寸
        btnEnterBoot.Padding = new Padding(2, 0, 2, 0);//按钮内部文字与边缘的间距
        btnEnterBoot.Margin = new Padding(2, 2, 2, 2);//按钮与外部其他控件的间距
        btnStartFirmware.Font = new Font("Segoe UI", 8F);
        btnStartFirmware.AutoSize = false;
        btnStartFirmware.Dock = DockStyle.Fill;
        btnStartFirmware.MinimumSize = new Size(0, 32);
        btnStartFirmware.Padding = new Padding(2, 0, 2, 0);
        btnStartFirmware.Margin = new Padding(2, 2, 2, 2);
        #endregion

        // ==================== 7. 实时监控区域（右侧上方） ====================
        #region 实时监控区域
        grpDataMonitoring.Controls.Add(tblMetrics);
        grpDataMonitoring.Dock = DockStyle.Top;
        grpDataMonitoring.Height = 190;
        grpDataMonitoring.Name = "grpDataMonitoring";
        grpDataMonitoring.Padding = new Padding(10, 20, 10, 10);
        grpDataMonitoring.Text = "实时数据监控";
        grpDataMonitoring.Margin = new Padding(0, 0, 0, 8);

        tblMetrics.ColumnCount = 3;
        tblMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        tblMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        tblMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        tblMetrics.Controls.Add(cardTemperature, 0, 0);
        tblMetrics.Controls.Add(cardHumidity, 1, 0);
        tblMetrics.Controls.Add(cardPressure, 2, 0);
        tblMetrics.Dock = DockStyle.Fill;
        tblMetrics.Name = "tblMetrics";
        tblMetrics.RowCount = 1;
        tblMetrics.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        cardTemperature.Dock = DockStyle.Fill;
        cardHumidity.Dock = DockStyle.Fill;
        cardPressure.Dock = DockStyle.Fill;
        #endregion

        // ==================== 8. 采集控制区域（右侧中部） ====================
        #region 采集控制区域
        grpOperations.Controls.Add(flowOperations);
        grpOperations.Dock = DockStyle.Top;
        grpOperations.Height = 100;
        grpOperations.Name = "grpOperations";
        grpOperations.Padding = new Padding(10, 20, 10, 10);
        grpOperations.Text = "采集控制";
        grpOperations.Margin = new Padding(0, 0, 0, 8);

        flowOperations.Controls.Add(btnPollOnce);
        flowOperations.Controls.Add(chkAutoRefresh);
        flowOperations.Controls.Add(numRefreshInterval);
        flowOperations.Controls.Add(lblRefreshIntervalUnit);
        flowOperations.Controls.Add(btnClearLog);
        flowOperations.Dock = DockStyle.Fill;
        flowOperations.Name = "flowOperations";
        flowOperations.Padding = new Padding(0, 8, 0, 0);
        flowOperations.WrapContents = false;
        ConfigureButton(btnPollOnce, "btnPollOnce", "立即采集", Color.FromArgb(8, 145, 178));
        chkAutoRefresh.AutoSize = true;
        chkAutoRefresh.Checked = true;
        chkAutoRefresh.Margin = new Padding(14, 10, 6, 3);
        chkAutoRefresh.Name = "chkAutoRefresh";
        chkAutoRefresh.Text = "自动刷新";
        numRefreshInterval.AutoSize = false;
        numRefreshInterval.DecimalPlaces = 0;
        numRefreshInterval.Increment = 1M;
        numRefreshInterval.Maximum = 600M;
        numRefreshInterval.Minimum = 1M;
        numRefreshInterval.Name = "numRefreshInterval";
        numRefreshInterval.Size = new Size(64, 28);
        numRefreshInterval.TabIndex = 0;
        numRefreshInterval.TextAlign = HorizontalAlignment.Center;
        numRefreshInterval.Value = 2M;
        numRefreshInterval.Margin = new Padding(0, 7, 2, 3);
        lblRefreshIntervalUnit.AutoSize = true;
        lblRefreshIntervalUnit.Name = "lblRefreshIntervalUnit";
        lblRefreshIntervalUnit.Text = "秒（1-600）";
        lblRefreshIntervalUnit.Margin = new Padding(0, 11, 14, 3);
        ConfigureButton(btnClearLog, "btnClearLog", "清空日志", Color.FromArgb(100, 116, 139));
        #endregion

        // ==================== 9. 运行日志区域（右侧下方） ====================
        #region 运行日志区域
        grpRuntimeLog.Controls.Add(txtRuntimeLog);
        grpRuntimeLog.Dock = DockStyle.Fill;
        grpRuntimeLog.Name = "grpRuntimeLog";
        grpRuntimeLog.Padding = new Padding(10, 20, 10, 10);
        grpRuntimeLog.Text = "运行日志 / 操作反馈";
        txtRuntimeLog.BackColor = Color.White;
        txtRuntimeLog.BorderStyle = BorderStyle.None;
        txtRuntimeLog.Dock = DockStyle.Fill;
        txtRuntimeLog.ForeColor = Color.FromArgb(51, 65, 85);
        txtRuntimeLog.Name = "txtRuntimeLog";
        txtRuntimeLog.ReadOnly = true;
        txtRuntimeLog.ScrollBars = RichTextBoxScrollBars.Vertical;
        #endregion

        // ==================== 10. 状态栏、定时器和窗体属性 ====================
        #region 状态栏、定时器和窗体属性
        statusStripMain.Items.Add(lblStatus);
        statusStripMain.Items.Add(lblStatusTime);
        statusStripMain.Dock = DockStyle.Bottom;
        statusStripMain.Name = "statusStripMain";
        statusStripMain.SizingGrip = false;
        lblStatus.Name = "lblStatus";
        lblStatus.Spring = true;
        lblStatus.Text = "状态：未连接";
        lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        lblStatusTime.Name = "lblStatusTime";
        lblStatusTime.Text = "--";

        uiTimer.Interval = 2000;

        // 默认启动尺寸适配常见 1366x768 屏幕，运行时仍可继续放大。
        ClientSize = new Size(1366, 768);
        Controls.Add(panelMain);
        Controls.Add(panelHeader);
        Controls.Add(statusStripMain);
        Font = new Font("Segoe UI", 9F);
        MinimumSize = new Size(1300, 720);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "温湿度配置工具";
        #endregion

        // ---------- 11. 恢复布局 ----------
        // ResumeLayout 让控件在全部属性设置完成后一次性重新布局，减少闪烁。
        panelHeader.ResumeLayout(false);
        panelMain.ResumeLayout(false);
        tblWorkspace.ResumeLayout(false);
        panelLeft.ResumeLayout(false);
        panelCenter.ResumeLayout(false);
        panelRight.ResumeLayout(false);
        grpCommunication.ResumeLayout(false);
        tblCommunication.ResumeLayout(false);
        tblCommunicationActions.ResumeLayout(false);
        grpDeviceParameters.ResumeLayout(false);
        tblDeviceParameters.ResumeLayout(false);
        tblDeviceParameters.PerformLayout();
        flowDeviceConnectionActions.ResumeLayout(false);
        grpDataCalibration.ResumeLayout(false);
        tblDataCalibration.ResumeLayout(false);
        tblDataCalibration.PerformLayout();
        grpFirmwareUpgrade.ResumeLayout(false);
        panelFirmwareUpgrade.ResumeLayout(false);
        tblFirmwareUpgrade.ResumeLayout(false);
        panelFirmwareVersion.ResumeLayout(false);
        panelFirmwareFile.ResumeLayout(false);
        panelFirmwareFile.PerformLayout();
        flowFirmwareActions.ResumeLayout(false);
        grpDataMonitoring.ResumeLayout(false);
        tblMetrics.ResumeLayout(false);
        grpOperations.ResumeLayout(false);
        flowOperations.ResumeLayout(false);
        flowOperations.PerformLayout();
        grpRuntimeLog.ResumeLayout(false);
        statusStripMain.ResumeLayout(false);
        statusStripMain.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)numRefreshInterval).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    private static void ConfigureFieldLabel(Label label, string text)
    {
        label.AutoSize = false;
        label.Dock = DockStyle.Fill;
        label.Margin = new Padding(0, 2, 6, 2);
        label.Text = text;
        label.TextAlign = ContentAlignment.MiddleLeft;
    }

    private static void ConfigureCombo(ComboBox combo, string name)
    {
        combo.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.FlatStyle = FlatStyle.Standard;
        combo.FormattingEnabled = true;
        combo.Margin = new Padding(0, 2, 0, 2);
        combo.Name = name;
    }

    private static void ConfigureTextBox(TextBox textBox, string name, string text)
    {
        textBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        textBox.Margin = new Padding(0, 2, 0, 2);
        textBox.Name = name;
        textBox.Text = text;
    }

    private static TextBox CreateRegisterHeader(string text)
    {
        return new TextBox
        {
            BackColor = Color.FromArgb(226, 232, 240),
            BorderStyle = BorderStyle.FixedSingle,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(51, 65, 85),
            Margin = new Padding(1),
            ReadOnly = true,
            Text = text,
            TextAlign = HorizontalAlignment.Center
        };
    }

    private static void ConfigureRegisterTextBox(TextBox textBox, string name, string text, bool readOnly)
    {
        textBox.BackColor = readOnly ? Color.FromArgb(226, 232, 240) : Color.White;
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.Dock = DockStyle.Fill;
        textBox.Margin = new Padding(2, 4, 2, 4);
        textBox.Name = name;
        textBox.ReadOnly = readOnly;
        textBox.TabStop = !readOnly;
        textBox.Text = text;
        textBox.TextAlign = HorizontalAlignment.Center;
    }

    private static void ConfigureSmallButton(Button button, string name, string text, Color backColor)
    {
        button.BackColor = backColor;
        button.Dock = DockStyle.Fill;
        button.FlatAppearance.BorderSize = 0;
        button.FlatStyle = FlatStyle.Flat;
        button.ForeColor = Color.White;
        button.Margin = new Padding(2, 3, 2, 3);
        button.Name = name;
        button.Text = text;
        button.UseVisualStyleBackColor = false;
    }

    private static void ConfigureFirmwareLabel(Label label, string text)
    {
        label.AutoSize = false;
        label.Dock = DockStyle.Fill;
        label.ForeColor = Color.FromArgb(226, 232, 240);
        label.Name = $"lbl{text.Replace(" ", string.Empty)}";
        label.Text = text;
        label.TextAlign = ContentAlignment.MiddleLeft;
    }

    private static void ConfigureButton(Button button, string name, string text, Color backColor)
    {
        button.AutoSize = false;
        button.BackColor = backColor;
        button.FlatAppearance.BorderSize = 0;
        button.FlatStyle = FlatStyle.Flat;
        button.ForeColor = Color.White;
        button.Height = 32;
        button.Margin = new Padding(0, 2, 8, 2);
        button.Name = name;
        button.Padding = new Padding(8, 0, 8, 0);
        button.Text = text;
        button.UseVisualStyleBackColor = false;
        button.MinimumSize = new Size(108, 32);
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.Width = 108;
    }

    private static void ConfigureCommunicationButton(Button button)
    {
        button.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        button.Dock = DockStyle.None;
        button.AutoEllipsis = false;
        button.Margin = new Padding(2, 5, 2, 5);
        button.MinimumSize = new Size(0, 32);
        button.Padding = new Padding(0);
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.Width = 0;
    }
}

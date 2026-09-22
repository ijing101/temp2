namespace SHT85.Controls;

public sealed class MetricCard : Panel
{
    private readonly Label _valueLabel;
    private readonly Label _unitLabel;

    public MetricCard(string title, string unit, Color accentColor)
    {
        BackColor = Color.White;
        BorderStyle = BorderStyle.FixedSingle;
        MinimumSize = new Size(160, 96);
        Padding = new Padding(12, 10, 12, 8);
        Margin = new Padding(6);

        var titleLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 26,
            Text = title,
            ForeColor = Color.FromArgb(75, 85, 99),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        _unitLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Bottom,
            Height = 24,
            Text = unit,
            ForeColor = Color.FromArgb(107, 114, 128),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _valueLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = "--",
            ForeColor = accentColor,
            Font = new Font("Segoe UI", 20F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        Controls.Add(_valueLabel);
        Controls.Add(_unitLabel);
        Controls.Add(titleLabel);
    }

    public void SetValue(double value, string format = "0.0") => _valueLabel.Text = value.ToString(format);
}

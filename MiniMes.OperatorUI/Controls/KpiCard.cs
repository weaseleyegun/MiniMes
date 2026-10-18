namespace MiniMes.OperatorUI.Controls;

// 숫자 하나를 크게 보여주는 작은 카드
public class KpiCard : Panel
{
    private readonly Label _title = new();
    private readonly Label _value = new();
    private readonly Label _sub = new();

    public KpiCard(string title, Color accent)
    {
        DoubleBuffered = true;
        BackColor = Theme.Panel;
        Dock = DockStyle.Fill;
        Margin = new Padding(4);
        MinimumSize = new Size(120, 70);

        _title.Text = title;
        _title.AutoSize = true;
        _title.Font = Theme.Font(8.5f);
        _title.ForeColor = Theme.Muted;
        _title.Location = new Point(10, 6);

        _value.AutoSize = true;
        _value.Font = Theme.Font(18, true);
        _value.ForeColor = accent;
        _value.Location = new Point(8, 22);
        _value.Text = "-";

        _sub.AutoSize = true;
        _sub.Font = Theme.Font(8);
        _sub.ForeColor = Theme.Muted;
        _sub.Location = new Point(10, 56);

        Controls.AddRange(new Control[] { _title, _value, _sub });
    }

    public void Set(string value, string sub = "")
    {
        _value.Text = value;
        _sub.Text = sub;
    }
}

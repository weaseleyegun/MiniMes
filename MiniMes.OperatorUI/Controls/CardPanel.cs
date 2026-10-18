namespace MiniMes.OperatorUI.Controls;

// 제목과 테두리가 있는 카드. 내용은 Body에 넣는다.
public class CardPanel : Panel
{
    private readonly Panel _header = new() { Dock = DockStyle.Top };
    private readonly Label _title = new();
    private readonly Label _subtitle = new();

    public Panel Body { get; } = new() { Dock = DockStyle.Fill };

    // 제목 줄 오른쪽에 컨트롤을 얹을 때 쓴다.
    public Panel HeaderPanel => _header;

    public CardPanel(string? title = null, string? subtitle = null)
    {
        DoubleBuffered = true;
        BackColor = Theme.Card;
        Padding = new Padding(1);
        Margin = new Padding(6);
        Dock = DockStyle.Fill;

        Body.BackColor = Theme.Card;
        Body.Padding = new Padding(10, 0, 10, 10);

        if (title is not null)
        {
            _title.Text = title;
            _title.AutoSize = true;
            _title.Font = Theme.Font(11, true);
            _title.ForeColor = Theme.Text;
            _title.Location = new Point(14, 10);
            _header.Controls.Add(_title);

            _subtitle.AutoSize = true;
            _subtitle.Font = Theme.Font(8.5f);
            _subtitle.ForeColor = Theme.Muted;
            _subtitle.Location = new Point(14, 33);
            _subtitle.Text = subtitle ?? "";
            _header.Controls.Add(_subtitle);

            _header.Height = string.IsNullOrEmpty(subtitle) ? 36 : 56;
        }
        else
        {
            _header.Height = 6;
        }

        _header.BackColor = Theme.Card;

        // Fill 컨트롤을 먼저 추가해야 Top 도킹 뒤 남은 영역을 채운다.
        Controls.Add(Body);
        Controls.Add(_header);
    }

    public string Subtitle
    {
        get => _subtitle.Text;
        set
        {
            _subtitle.Text = value;
            if (!string.IsNullOrEmpty(value) && _header.Height < 56) _header.Height = 56;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(Theme.CardBorder);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
}

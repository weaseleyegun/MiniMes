namespace MiniMes.OperatorUI.Controls;

// 여러 화면에서 같이 쓰는 컨트롤 생성 함수 모음 (모양을 한곳에서 통일한다)
public static class Ui
{
    public static Label Caption(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = Theme.Muted,
        Font = Theme.Font(9),
        Margin = new Padding(0, 9, 6, 0),
    };

    public static ComboBox Combo(int width) => new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = width,
        Font = Theme.Font(10),
        BackColor = Theme.Panel,
        ForeColor = Theme.Text,
        FlatStyle = FlatStyle.Flat,
        Margin = new Padding(0, 4, 14, 0),
    };

    public static DateTimePicker DatePicker(DateTime value) => new()
    {
        Format = DateTimePickerFormat.Short,
        Width = 118,
        Value = value,
        Font = Theme.Font(10),
        CalendarMonthBackground = Theme.Panel,
        Margin = new Padding(0, 4, 14, 0),
    };

    public static Button Primary(string text, int width = 0) => Styled(text, Theme.Accent, Color.FromArgb(5, 34, 63), width);

    public static Button Secondary(string text, int width = 0) => Styled(text, Color.FromArgb(34, 51, 76), Theme.Text, width);

    private static Button Styled(string text, Color back, Color fore, int width)
    {
        var btn = new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            BackColor = back,
            ForeColor = fore,
            Font = Theme.Font(9.5f, true),
            Cursor = Cursors.Hand,
            Height = 32,
            Margin = new Padding(0, 2, 8, 0),
            TabStop = false,
        };
        btn.FlatAppearance.BorderSize = 0;
        if (width > 0) btn.Width = width; else { btn.AutoSize = true; btn.MinimumSize = new Size(70, 32); }
        return btn;
    }

    public static FlowLayoutPanel Row(params Control[] controls)
    {
        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
        };
        row.Controls.AddRange(controls);
        return row;
    }

    public static NumericUpDown Number(decimal value, decimal min, decimal max, decimal step, int decimals, int width = 80) => new()
    {
        DecimalPlaces = decimals,
        Minimum = min,
        Maximum = max,
        Increment = step,
        Value = value,
        Width = width,
        Font = Theme.Font(10),
        BackColor = Theme.Panel,
        ForeColor = Theme.Text,
        Margin = new Padding(0, 4, 12, 0),
    };

    public static void StyleGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = Theme.Line;
        grid.BackgroundColor = Theme.Card;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersHeight = 32;
        grid.RowTemplate.Height = 30;
        grid.Font = Theme.Font(9.5f);

        grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.Panel;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.Muted;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.Panel;
        grid.DefaultCellStyle.BackColor = Theme.Card;
        grid.DefaultCellStyle.ForeColor = Theme.Text;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(28, 44, 70);
        grid.DefaultCellStyle.SelectionForeColor = Theme.Text;
    }
}

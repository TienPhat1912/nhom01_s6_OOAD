using System.Drawing.Drawing2D;

namespace Hospital.Desktop.Design;

public static class UiTheme
{
    public static Font Body() => new("Segoe UI", 10F);
    public static Label Label(string text, float size = 10, bool bold = false, Color? color = null) => new()
    {
        Text = text, AutoSize = false, Dock = DockStyle.Fill, AutoEllipsis = true,
        TextAlign = ContentAlignment.MiddleLeft, ForeColor = color ?? DesignTokens.Slate700,
        Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular), Margin = new Padding(0)
    };
    public static GraphicsPath Rounded(Rectangle rect, int radius)
    {
        var path = new GraphicsPath(); var d = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));
        if (d <= 0) { path.AddRectangle(rect); return path; }
        path.AddArc(rect.Left, rect.Top, d, d, 180, 90); path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90); path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure(); return path;
    }
}

public sealed class UiButton : Button
{
    private bool secondary;
    [System.ComponentModel.DefaultValue(false)]
    public bool Secondary { get => secondary; set { secondary = value; Invalidate(); } }
    public UiButton()
    {
        FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; Cursor = Cursors.Hand;
        Font = new Font("Segoe UI", 10F, FontStyle.Bold); Size = new Size(150, 44);
        SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var background = !Enabled ? DesignTokens.Slate100 : secondary ? DesignTokens.Surface : DesignTokens.Primary700;
        var foreground = !Enabled ? DesignTokens.Slate500 : secondary ? DesignTokens.Primary700 : Color.White;
        using var path = UiTheme.Rounded(new Rectangle(0, 0, Width - 1, Height - 1), DesignTokens.RadiusMd);
        using var fill = new SolidBrush(background); e.Graphics.FillPath(fill, path);
        if (secondary) { using var pen = new Pen(DesignTokens.Slate200); e.Graphics.DrawPath(pen, path); }
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, foreground, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -5, -5), foreground, background);
    }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
}

public sealed class UiField : UserControl
{
    public TextBox Input { get; } = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Font = UiTheme.Body() };
    private readonly Label error = UiTheme.Label("", 9, color: DesignTokens.Danger600);
    public UiField(string label, bool password = false)
    {
        Height = 92; Dock = DockStyle.Top; BackColor = Color.White;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Margin = new Padding(0) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(UiTheme.Label(label, 10, true), 0, 0); layout.Controls.Add(Input, 0, 1); layout.Controls.Add(error, 0, 2);
        Input.UseSystemPasswordChar = password; Input.AccessibleName = label;
        Controls.Add(layout);
    }
    public void SetError(string? message) { error.Text = message ?? ""; Input.AccessibleDescription = error.Text; }
}

public sealed class UiBanner : UserControl
{
    private readonly Label label = UiTheme.Label("", 10);
    public UiBanner() { Height = 64; Dock = DockStyle.Top; Padding = new Padding(14, 8, 14, 8); label.AutoEllipsis = false; Controls.Add(label); Visible = false; AccessibleRole = AccessibleRole.Alert; }
    public void ShowMessage(string text, bool error = false)
    {
        label.Text = text; AccessibleName = text; label.ForeColor = error ? DesignTokens.Danger600 : DesignTokens.Primary700;
        BackColor = error ? Color.FromArgb(255, 245, 243) : DesignTokens.Primary50; Visible = true;
    }
}

public sealed class UiDataGrid : DataGridView
{
    public UiDataGrid()
    {
        Dock = DockStyle.Fill; BackgroundColor = Color.White; BorderStyle = BorderStyle.None;
        ReadOnly = true; AllowUserToAddRows = false; AllowUserToDeleteRows = false; RowHeadersVisible = false;
        AutoGenerateColumns = true; AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        SelectionMode = DataGridViewSelectionMode.FullRowSelect; MultiSelect = false; EnableHeadersVisualStyles = false;
        ColumnHeadersDefaultCellStyle = new() { BackColor = DesignTokens.Primary900, ForeColor = Color.White,
            SelectionBackColor = DesignTokens.Primary900, SelectionForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold), Padding = new Padding(8) };
        ColumnHeadersHeight = 44; ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        DefaultCellStyle = new() { Font = UiTheme.Body(), ForeColor = DesignTokens.Slate700, BackColor = Color.White,
            SelectionBackColor = DesignTokens.Primary100, SelectionForeColor = DesignTokens.Primary900, Padding = new Padding(8) };
        AlternatingRowsDefaultCellStyle.BackColor = DesignTokens.Slate50; RowTemplate.Height = 46;
        GridColor = DesignTokens.Slate200; CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        AccessibleName = "Danh sách dữ liệu";
    }
}

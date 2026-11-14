using System.ComponentModel.DataAnnotations;
using Hospital.Client;
using Hospital.Desktop.Services;

namespace Hospital.Desktop.Design;

public interface IDesktopDialogs
{
    bool Confirm(IWin32Window owner, string title, string message);
}

public sealed class DesktopDialogs : IDesktopDialogs
{
    public bool Confirm(IWin32Window owner, string title, string message)
    { using var dialog = new ConfirmForm(title, message); return dialog.ShowDialog(owner) == DialogResult.OK; }
}

public sealed class ConfirmForm : Form
{
    public ConfirmForm(string title, string message)
    {
        Text = title; StartPosition = FormStartPosition.CenterParent; Font = UiTheme.Body();
        AutoScaleMode = AutoScaleMode.Dpi; ClientSize = new Size(540, 280); MinimumSize = new Size(500, 280);
        MinimizeBox = MaximizeBox = false; BackColor = Color.White; ShowInTaskbar = false;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), RowCount = 3, ColumnCount = 1 };
        root.RowStyles.Add(new(SizeType.Absolute, 46)); root.RowStyles.Add(new(SizeType.Percent, 100)); root.RowStyles.Add(new(SizeType.Absolute, 60));
        root.Controls.Add(UiTheme.Label(title, 18, true), 0, 0);
        var text = UiTheme.Label(message); text.AutoEllipsis = false; root.Controls.Add(text, 0, 1);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var accept = new UiButton { Text = "Xác nhận", DialogResult = DialogResult.OK };
        var cancel = new UiButton { Text = "Hủy bỏ", Secondary = true, DialogResult = DialogResult.Cancel };
        buttons.Controls.Add(accept); buttons.Controls.Add(cancel); root.Controls.Add(buttons, 0, 2); Controls.Add(root);
        AcceptButton = CancelButton = cancel; Shown += (_, _) => cancel.Focus();
    }
}

public sealed class FormField : UserControl
{
    public Control Input { get; }
    public string Caption { get; }
    private readonly Label error = UiTheme.Label("", 9, color: DesignTokens.Danger600);
    public FormField(string caption, Control input, int height)
    {
        Caption = caption; Input = input; Height = height; Dock = DockStyle.Fill; Margin = new Padding(0, 0, 14, 10);
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Margin = new Padding(0) };
        grid.RowStyles.Add(new(SizeType.Absolute, 28)); grid.RowStyles.Add(new(SizeType.Percent, 100)); grid.RowStyles.Add(new(SizeType.Absolute, 30));
        input.Dock = DockStyle.Fill; input.AccessibleName = caption;
        grid.Controls.Add(UiTheme.Label(caption, 10, true), 0, 0); grid.Controls.Add(input, 0, 1); grid.Controls.Add(error, 0, 2); Controls.Add(grid);
    }
    public void SetError(string? text) { error.Text = text ?? ""; Input.AccessibleDescription = error.Text; }
}

public abstract class EditorForm : Form
{
    protected readonly AdminSession Session;
    protected readonly IDesktopDialogs Dialogs;
    protected readonly CancellationTokenSource Lifetime = new();
    protected readonly UiBanner Banner = new() { Dock = DockStyle.Fill };
    protected readonly UiButton Save = new() { Text = "Lưu thay đổi" };
    protected readonly Dictionary<string, FormField> Fields = [];
    private readonly TableLayoutPanel fields = new() { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(0, 4, 0, 4) };
    private readonly UiButton cancel = new() { Text = "Hủy bỏ", Secondary = true };
    private int position;
    private bool saving;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public object? SavedResult { get; protected set; }

    protected EditorForm(AdminSession session, IDesktopDialogs dialogs, string title, string subtitle)
    {
        Session = session; Dialogs = dialogs; Text = title; StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi; ClientSize = new Size(760, 700); MinimumSize = new Size(680, 560);
        Font = UiTheme.Body(); BackColor = Color.White; MinimizeBox = false; ShowInTaskbar = false;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 1, Padding = new Padding(24) };
        root.RowStyles.Add(new(SizeType.Absolute, 42)); root.RowStyles.Add(new(SizeType.Absolute, 60)); root.RowStyles.Add(new(SizeType.Absolute, 0));
        root.RowStyles.Add(new(SizeType.Percent, 100)); root.RowStyles.Add(new(SizeType.Absolute, 68));
        root.Controls.Add(UiTheme.Label(title, 22, true), 0, 0); root.Controls.Add(UiTheme.Label(subtitle, 10, color: DesignTokens.Slate500), 0, 1);
        root.Controls.Add(Banner, 0, 2);
        Banner.VisibleChanged += (_, _) => root.RowStyles[2].Height = Banner.Visible ? 110 : 0;
        fields.ColumnStyles.Add(new(SizeType.Percent, 50)); fields.ColumnStyles.Add(new(SizeType.Percent, 50));
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true }; scroll.Controls.Add(fields); root.Controls.Add(scroll, 0, 3);
        var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 10, 0, 0) };
        footer.Controls.Add(Save); footer.Controls.Add(cancel); root.Controls.Add(footer, 0, 4); Controls.Add(root);
        Save.Click += async (_, _) => await SubmitAsync(); cancel.Click += (_, _) => Close(); CancelButton = cancel;
        FormClosing += (_, e) => { if (saving) e.Cancel = true; else Lifetime.Cancel(); };
    }

    protected T AddField<T>(string key, string caption, T input, int height = 94, bool wide = false) where T : Control
    {
        if (wide && position % 2 != 0) position++;
        var row = position / 2;
        while (fields.RowStyles.Count <= row) { fields.RowStyles.Add(new(SizeType.AutoSize)); fields.RowCount++; }
        var field = new FormField(caption, input, height); input.Name = key;
        fields.Controls.Add(field, position % 2, row); if (wide) fields.SetColumnSpan(field, 2);
        position += wide ? 2 : 1; Fields[key] = field; return input;
    }
    protected TextBox TextField(string key, string label, int max, string? value = null, bool secret = false, bool multiline = false) =>
        AddField(key, label, new TextBox { Text = value ?? "", MaxLength = max, UseSystemPasswordChar = secret,
            Multiline = multiline, ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None, Font = UiTheme.Body() }, multiline ? 150 : 94, multiline);
    protected ComboBox ChoiceField<T>(string key, string label, IEnumerable<Choice<T>> choices, T selected) where T : notnull
    {
        var box = AddField(key, label, new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.Body() });
        foreach (var item in choices) box.Items.Add(item);
        box.SelectedItem = box.Items.Cast<Choice<T>>().FirstOrDefault(c => EqualityComparer<T>.Default.Equals(c.Value, selected));
        return box;
    }
    protected bool Error(string key, string message)
    { if (Fields.TryGetValue(key, out var field)) { field.SetError(message); field.Input.Focus(); } Banner.ShowMessage(message, true); return false; }
    protected abstract object CreateRequest();
    protected abstract Task<object?> PersistAsync(object request);
    protected virtual bool ValidateRequest(object request) => true;
    protected virtual string Confirmation => "Kiểm tra thông tin và lưu thay đổi này vào hệ thống?";
    protected virtual void AfterSaved() { }

    private bool ValidateAnnotations(object request)
    {
        var valid = true;
        foreach (var property in request.GetType().GetProperties())
        foreach (var rule in property.GetCustomAttributes(typeof(ValidationAttribute), true).Cast<ValidationAttribute>())
        {
            if (rule.IsValid(property.GetValue(request))) continue;
            valid = false;
            var message = rule switch
            {
                RequiredAttribute => "Vui lòng nhập thông tin bắt buộc.",
                StringLengthAttribute length => $"Độ dài từ {length.MinimumLength} đến {length.MaximumLength} ký tự.",
                EmailAddressAttribute => "Email không đúng định dạng.", PhoneAttribute => "Số điện thoại không hợp lệ.",
                MinLengthAttribute minimum => $"Cần chọn ít nhất {minimum.Length} mục.", _ => "Giá trị không hợp lệ."
            };
            if (Fields.TryGetValue(property.Name, out var field)) field.SetError(message);
            Banner.ShowMessage("Kiểm tra các trường được đánh dấu trước khi lưu.", true);
        }
        return valid;
    }
    private async Task SubmitAsync()
    {
        if (saving) return;
        foreach (var field in Fields.Values) field.SetError(null);
        Banner.Visible = false;
        var request = CreateRequest();
        if (!ValidateAnnotations(request) || !ValidateRequest(request)) return;
        if (!Dialogs.Confirm(this, Text, Confirmation)) return;
        saving = true; Save.Enabled = cancel.Enabled = fields.Enabled = false; Save.Text = "Đang lưu…";
        var uncertain = false;
        try
        {
            SavedResult = await PersistAsync(request); AfterSaved();
            saving = false; DialogResult = DialogResult.OK; Close();
        }
        catch (HospitalApiException ex)
        {
            if (Session.Expired) { saving = false; DialogResult = DialogResult.Abort; Close(); }
            else
            {
                uncertain = (int)ex.Status >= 500;
                Banner.ShowMessage(AdminSession.Error(ex) + (uncertain ? "\nChưa rõ kết quả lưu. Đóng form và tải lại danh sách để đối chiếu trước khi tạo yêu cầu mới." : ""), true);
            }
        }
        catch (OperationCanceledException) when (Lifetime.IsCancellationRequested) { }
        finally
        {
            saving = false;
            if (!IsDisposed) { Save.Enabled = !uncertain; cancel.Enabled = fields.Enabled = true; Save.Text = "Lưu thay đổi"; }
        }
    }
    protected override void Dispose(bool disposing)
    { if (disposing && !IsDisposed) { Lifetime.Cancel(); Lifetime.Dispose(); } base.Dispose(disposing); }
}

public record Choice<T>(T Value, string Label)
{
    public override string ToString() => Label;
}

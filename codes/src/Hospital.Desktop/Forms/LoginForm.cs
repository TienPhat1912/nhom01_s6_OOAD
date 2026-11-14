using Hospital.Application.DTOs.Auth;
using Hospital.Client;
using Hospital.Desktop.Design;

namespace Hospital.Desktop.Forms;

public sealed class LoginForm : Form
{
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(15), BaseAddress = new Uri(Environment.GetEnvironmentVariable("HOSPITAL_API_URL") ?? "http://localhost:5014/") };
    private readonly UiField username = new("Tên đăng nhập");
    private readonly UiField password = new("Mật khẩu", true);
    private readonly UiButton submit = new() { Text = "Đăng nhập", Dock = DockStyle.Fill };
    private readonly UiBanner banner = new();
    private readonly CancellationTokenSource lifetime = new();
    public LoginForm()
    {
        Text = "An Tâm · Đăng nhập quản trị"; StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
        ClientSize = new Size(940, 590); MinimumSize = new Size(800, 590); Font = UiTheme.Body(); BackColor = Color.White;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 57));
        var story = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = DesignTokens.Primary900, Padding = new Padding(38), RowCount = 4, ColumnCount = 1, Margin = new Padding(0) };
        story.RowStyles.Add(new RowStyle(SizeType.Absolute, 80)); story.RowStyles.Add(new RowStyle(SizeType.Absolute, 150)); story.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); story.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
        story.Controls.Add(UiTheme.Label("✚  An Tâm", 26, true, Color.White), 0, 0);
        story.Controls.Add(UiTheme.Label("Quản trị rõ ràng.\nVận hành an tâm.", 18, true, Color.White), 0, 1);
        story.Controls.Add(UiTheme.Label("Quản lý tài khoản, bác sĩ, chuyên khoa và lịch làm việc. Theo dõi thay đổi qua nhật ký hệ thống.", 11, color: Color.FromArgb(190, 216, 205)), 0, 2);
        story.Controls.Add(UiTheme.Label("Dành cho quản trị viên\nThông tin được bảo vệ theo quyền truy cập.", 9, color: Color.FromArgb(190, 216, 205)), 0, 3);
        var form = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(40, 50, 40, 30), RowCount = 8, ColumnCount = 1 };
        foreach (var height in new[] { 52, 42, 72, 92, 92, 34, 46 }) form.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        form.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        form.Controls.Add(UiTheme.Label("Chào mừng trở lại", 23, true, DesignTokens.Slate900), 0, 0);
        form.Controls.Add(UiTheme.Label("Đăng nhập để mở không gian quản trị.", 10, color: DesignTokens.Slate500), 0, 1);
        form.Controls.Add(banner, 0, 2); form.Controls.Add(username, 0, 3); form.Controls.Add(password, 0, 4);
        var show = new CheckBox { Text = "Hiện mật khẩu", AutoSize = true, ForeColor = DesignTokens.Slate500 };
        show.CheckedChanged += (_, _) => password.Input.UseSystemPasswordChar = !show.Checked;
        form.Controls.Add(show, 0, 5); form.Controls.Add(submit, 0, 6);
        username.Input.MaxLength = 50; password.Input.MaxLength = 72;
        root.Controls.Add(story, 0, 0); root.Controls.Add(form, 1, 0); Controls.Add(root);
        AcceptButton = submit; submit.Click += LoginAsync;
        FormClosing += (_, _) => lifetime.Cancel();
    }
    private async void LoginAsync(object? sender, EventArgs e)
    {
        username.SetError(null); password.SetError(null);
        if (string.IsNullOrWhiteSpace(username.Input.Text)) { username.SetError("Vui lòng nhập tên đăng nhập."); username.Input.Focus(); return; }
        if (string.IsNullOrEmpty(password.Input.Text)) { password.SetError("Vui lòng nhập mật khẩu."); password.Input.Focus(); return; }
        submit.Enabled = false; submit.Text = "Đang đăng nhập…"; banner.Visible = false;
        try
        {
            var api = new HospitalApiClient(http);
            var auth = await api.SendAsync<AuthResponseDto>(HttpMethod.Post, "api/auth/login",
                new LoginRequestDto { Username = username.Input.Text, Password = password.Input.Text }, cancellationToken: lifetime.Token);
            password.Input.Clear();
            if (auth.Role != "Admin") { banner.ShowMessage("Tài khoản này không có quyền quản trị. Vui lòng sử dụng cổng Web.", true); return; }
            Hide();
            using var shell = new AdminShellForm(api, auth);
            shell.ShowDialog();
            if (!IsDisposed) { Show(); banner.ShowMessage(shell.SessionEndedMessage); }
        }
        catch (HospitalApiException ex) { banner.ShowMessage(ex.Message + (ex.TraceId == null ? "" : "\nMã hỗ trợ: " + ex.TraceId), true); }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        finally { if (!IsDisposed) { submit.Enabled = true; submit.Text = "Đăng nhập"; } }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing && !IsDisposed) { lifetime.Cancel(); lifetime.Dispose(); http.Dispose(); }
        base.Dispose(disposing);
    }
}

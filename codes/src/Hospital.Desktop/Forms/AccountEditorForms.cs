using Hospital.Application.DTOs.Auth;
using Hospital.Desktop.Design;
using Hospital.Desktop.Services;

namespace Hospital.Desktop.Forms;

public sealed class ProfileEditorForm : EditorForm
{
    private readonly TextBox name, email, phone;
    public ProfileEditorForm(AdminSession session, IDesktopDialogs dialogs, UserProfileDto profile) : base(session, dialogs, "Hồ sơ cá nhân", $"{profile.Username} · Quản trị viên")
    {
        name = TextField("FullName", "Họ và tên", 100, profile.FullName); email = TextField("Email", "Email", 100, profile.Email);
        phone = TextField("PhoneNumber", "Số điện thoại", 20, profile.PhoneNumber);
    }
    protected override object CreateRequest() => new UpdateProfileRequestDto { FullName = name.Text.Trim(), Email = email.Text.Trim(), PhoneNumber = string.IsNullOrWhiteSpace(phone.Text) ? null : phone.Text.Trim() };
    protected override async Task<object?> PersistAsync(object request) => await Session.SendAsync<object>(HttpMethod.Put, "api/auth/profile", request, Lifetime.Token);
    protected override void AfterSaved() => Session.FullName = name.Text.Trim();
}

public sealed class PasswordEditorForm : EditorForm
{
    private readonly TextBox oldPassword, newPassword, confirm;
    public PasswordEditorForm(AdminSession session, IDesktopDialogs dialogs) : base(session, dialogs, "Đổi mật khẩu", "Sau khi đổi mật khẩu, các phiên cũ sẽ hết hiệu lực. Bạn cần đăng nhập lại.")
    {
        oldPassword = TextField("OldPassword", "Mật khẩu hiện tại", 72, secret: true);
        newPassword = TextField("NewPassword", "Mật khẩu mới (8–72 ký tự)", 72, secret: true);
        confirm = TextField("ConfirmPassword", "Nhập lại mật khẩu mới", 72, secret: true);
    }
    protected override object CreateRequest() => new ChangePasswordRequestDto { OldPassword = oldPassword.Text, NewPassword = newPassword.Text };
    protected override bool ValidateRequest(object request) => newPassword.Text == confirm.Text || Error("ConfirmPassword", "Mật khẩu xác nhận chưa khớp.");
    protected override string Confirmation => "Đổi mật khẩu và đăng nhập lại? Các phiên đăng nhập hiện có sẽ hết hiệu lực.";
    protected override async Task<object?> PersistAsync(object request) => await Session.SendAsync<object>(HttpMethod.Post, "api/auth/change-password", request, Lifetime.Token);
    protected override void AfterSaved()
    { oldPassword.Clear(); newPassword.Clear(); confirm.Clear(); Session.End("Mật khẩu đã được thay đổi. Vui lòng đăng nhập bằng mật khẩu mới."); }
}

using Hospital.Application.DTOs.Admin;
using Hospital.Application.DTOs.Catalog;
using Hospital.Desktop.Design;
using Hospital.Desktop.Services;
using Hospital.Domain.Enums;

namespace Hospital.Desktop.Forms;

public sealed class UserEditorForm : EditorForm
{
    private readonly UserDto? existing;
    private readonly TextBox username, name, email, phone;
    private readonly TextBox? password;
    private readonly ComboBox? role;
    private readonly CheckBox? active;
    private readonly UiButton? doctor;
    private DoctorDto? selectedDoctor;
    public UserEditorForm(AdminSession session, IDesktopDialogs dialogs, UserDto? existing = null)
        : base(session, dialogs, existing == null ? "Cấp tài khoản" : "Cập nhật tài khoản",
            existing == null ? "Cấp tài khoản Staff, Doctor hoặc Admin. Hồ sơ bác sĩ cần được tạo trước khi cấp tài khoản Doctor." : "Tên đăng nhập được giữ nguyên. Khóa tài khoản sẽ thu hồi các phiên đăng nhập hiện có.")
    {
        this.existing = existing;
        username = TextField("Username", "Tên đăng nhập", 50, existing?.Username); username.ReadOnly = existing != null;
        name = TextField("FullName", "Họ và tên", 100, existing?.FullName);
        email = TextField("Email", "Email", 100, existing?.Email); phone = TextField("PhoneNumber", "Số điện thoại", 20, existing?.PhoneNumber);
        if (existing == null)
        {
            password = TextField("Password", "Mật khẩu ban đầu (8–72 ký tự)", 72, secret: true);
            role = ChoiceField("Role", "Vai trò", Roles(), UserRole.Staff);
            doctor = AddField("DoctorId", "Hồ sơ bác sĩ", new UiButton { Text = "Chọn hồ sơ bác sĩ", Secondary = true, Enabled = false }, wide: true);
            role.SelectedIndexChanged += (_, _) => RefreshRole();
            doctor.Click += (_, _) =>
            {
                using var picker = new DoctorPickerForm(Session, unlinkedOnly: true);
                if (picker.ShowDialog(this) == DialogResult.OK) { selectedDoctor = picker.SelectedDoctor; doctor.Text = selectedDoctor!.FullName; name.Text = selectedDoctor.FullName; }
                else if (Session.Expired) { DialogResult = DialogResult.Abort; Close(); }
            };
        }
        else active = AddField("IsActive", "Trạng thái", new CheckBox { Text = "Tài khoản đang hoạt động", Checked = existing.IsActive });
    }
    internal static IEnumerable<Choice<UserRole>> Roles() => new[] { UserRole.Staff, UserRole.Doctor, UserRole.Admin }.Select(r => new Choice<UserRole>(r, AdminSession.Role(r)));
    private UserRole SelectedRole => (role?.SelectedItem as Choice<UserRole>)?.Value ?? UserRole.Staff;
    private void RefreshRole()
    {
        var isDoctor = SelectedRole == UserRole.Doctor;
        doctor!.Enabled = isDoctor; name.ReadOnly = isDoctor;
        if (isDoctor && selectedDoctor != null) name.Text = selectedDoctor.FullName;
    }
    protected override object CreateRequest() => existing == null ? new CreateUserRequest
    {
        Username = username.Text.Trim(), Password = password!.Text, FullName = name.Text.Trim(), Email = email.Text.Trim(),
        PhoneNumber = string.IsNullOrWhiteSpace(phone.Text) ? null : phone.Text.Trim(), Role = SelectedRole,
        DoctorId = SelectedRole == UserRole.Doctor ? selectedDoctor?.Id : null
    } : new UpdateUserRequest { FullName = name.Text.Trim(), Email = email.Text.Trim(), PhoneNumber = string.IsNullOrWhiteSpace(phone.Text) ? null : phone.Text.Trim(), IsActive = active!.Checked };
    protected override bool ValidateRequest(object request) => request is not CreateUserRequest { Role: UserRole.Doctor, DoctorId: null }
        || Error("DoctorId", "Chọn hồ sơ bác sĩ đang hoạt động, chưa có tài khoản.");
    protected override string Confirmation => existing != null && !active!.Checked
        ? $"Khóa tài khoản {existing.Username}? Các phiên đăng nhập của tài khoản này sẽ hết hiệu lực."
        : $"Lưu thông tin tài khoản {username.Text.Trim()}?";
    protected override async Task<object?> PersistAsync(object request) => await Session.SendAsync<UserDto>(existing == null ? HttpMethod.Post : HttpMethod.Put,
        existing == null ? "api/admin/users" : $"api/admin/users/{existing.Id}", request, Lifetime.Token);
    protected override void AfterSaved()
    {
        password?.Clear();
        if (existing?.Id == Session.UserId && SavedResult is UserDto saved)
        { Session.FullName = saved.FullName; if (!saved.IsActive) Session.End("Tài khoản của bạn đã được khóa. Vui lòng đăng nhập bằng tài khoản còn hiệu lực."); }
    }
}

public sealed class RoleEditorForm : EditorForm
{
    private readonly UserDto user;
    private readonly ComboBox role;
    private readonly UiButton doctor;
    private Guid? doctorId;
    public RoleEditorForm(AdminSession session, IDesktopDialogs dialogs, UserDto user) : base(session, dialogs, "Phân quyền tài khoản",
        $"{user.Username} · {user.FullName}. Thay đổi vai trò sẽ thu hồi phiên đăng nhập. Tài khoản bệnh nhân giữ nguyên vai trò để bảo toàn hồ sơ.")
    {
        this.user = user; doctorId = user.DoctorId;
        var options = user.Role == UserRole.Patient ? new[] { new Choice<UserRole>(UserRole.Patient, "Bệnh nhân") } : UserEditorForm.Roles();
        role = ChoiceField("Role", "Vai trò mới", options, user.Role); role.Enabled = user.Role != UserRole.Patient;
        doctor = AddField("DoctorId", "Hồ sơ bác sĩ", new UiButton { Text = user.DoctorId.HasValue ? "Giữ hồ sơ bác sĩ hiện tại" : "Chọn hồ sơ bác sĩ", Secondary = true }, wide: true);
        role.SelectedIndexChanged += (_, _) => UpdateDoctor(); UpdateDoctor();
        doctor.Click += (_, _) =>
        {
            using var picker = new DoctorPickerForm(Session, unlinkedOnly: true, currentUserId: user.Id);
            if (picker.ShowDialog(this) == DialogResult.OK) { doctorId = picker.SelectedDoctor!.Id; doctor.Text = picker.SelectedDoctor.FullName; }
            else if (Session.Expired) { DialogResult = DialogResult.Abort; Close(); }
        };
        Save.Enabled = user.Role != UserRole.Patient;
    }
    private UserRole SelectedRole => ((Choice<UserRole>)role.SelectedItem!).Value;
    private void UpdateDoctor() => doctor.Enabled = SelectedRole == UserRole.Doctor && !user.DoctorId.HasValue;
    protected override object CreateRequest() => new ChangeRoleRequest { Role = SelectedRole, DoctorId = SelectedRole == UserRole.Doctor ? doctorId : null };
    protected override bool ValidateRequest(object request) => SelectedRole != UserRole.Doctor || doctorId.HasValue || Error("DoctorId", "Cần chọn hồ sơ bác sĩ trước khi cấp quyền.");
    protected override string Confirmation => $"Đổi vai trò của {user.Username} thành {AdminSession.Role(SelectedRole)}? Các phiên hiện có sẽ hết hiệu lực.";
    protected override async Task<object?> PersistAsync(object request) => await Session.SendAsync<UserDto>(HttpMethod.Put, $"api/admin/users/{user.Id}/role", request, Lifetime.Token);
    protected override void AfterSaved() { if (user.Id == Session.UserId) Session.End("Quyền của bạn vừa được cập nhật. Vui lòng đăng nhập lại."); }
}

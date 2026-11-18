using Hospital.Application.DTOs.Admin;
using Hospital.Application.DTOs.Catalog;
using Hospital.Desktop.Design;
using Hospital.Desktop.Services;

namespace Hospital.Desktop.Forms;

public sealed class SpecialtyEditorForm : EditorForm
{
    private readonly SpecialtyDto? existing;
    private readonly TextBox code, name, description;
    private readonly CheckBox active;
    public SpecialtyEditorForm(AdminSession session, IDesktopDialogs dialogs, SpecialtyDto? existing = null)
        : base(session, dialogs, existing == null ? "Thêm chuyên khoa" : "Cập nhật chuyên khoa",
            "Mã và tên chuyên khoa phải duy nhất. Ngừng hoạt động giữ lại dữ liệu lịch sử.")
    {
        this.existing = existing;
        code = TextField("Code", "Mã chuyên khoa", 20, existing?.Code);
        name = TextField("Name", "Tên chuyên khoa", 100, existing?.Name);
        description = TextField("Description", "Mô tả", 500, existing?.Description, multiline: true);
        active = AddField("IsActive", "Trạng thái", new CheckBox { Text = "Đang hoạt động", Checked = existing?.IsActive ?? true });
    }
    protected override object CreateRequest() => new SaveSpecialtyRequest { Code = code.Text.Trim(), Name = name.Text.Trim(), Description = description.Text.Trim(), IsActive = active.Checked };
    protected override string Confirmation => active.Checked ? $"Lưu chuyên khoa {name.Text.Trim()}?" : $"Ngừng hoạt động chuyên khoa {name.Text.Trim()}? Hệ thống sẽ kiểm tra lịch hẹn còn bị ảnh hưởng.";
    protected override async Task<object?> PersistAsync(object request) => await Session.SendAsync<SpecialtyDto>(existing == null ? HttpMethod.Post : HttpMethod.Put,
        existing == null ? "api/admin/specialties" : $"api/admin/specialties/{existing.Id}", request, Lifetime.Token);
}

public sealed class DoctorEditorForm : EditorForm
{
    private readonly DoctorDetailDto? existing;
    private readonly TextBox name, title, bio, avatar;
    private readonly ComboBox specialty;
    private readonly CheckBox active;
    public DoctorEditorForm(AdminSession session, IDesktopDialogs dialogs, List<SpecialtyDto> options, DoctorDetailDto? existing = null)
        : base(session, dialogs, existing == null ? "Thêm hồ sơ bác sĩ" : "Cập nhật hồ sơ bác sĩ",
            "Mỗi bác sĩ thuộc một chuyên khoa. Hồ sơ có thể được tạo trước khi cấp tài khoản.")
    {
        this.existing = existing;
        name = TextField("FullName", "Họ và tên bác sĩ", 100, existing?.FullName);
        title = TextField("Title", "Chức danh", 50, existing?.Title);
        bio = TextField("Bio", "Giới thiệu", 1000, existing?.Bio, multiline: true);
        avatar = TextField("AvatarUrl", "Đường dẫn ảnh (/images/ hoặc HTTPS)", 500, existing?.AvatarUrl);
        active = AddField("IsActive", "Trạng thái", new CheckBox { Text = "Đang hoạt động", Checked = existing?.IsActive ?? true });
        specialty = ChoiceField("SpecialtyId", "Chuyên khoa phụ trách",
            options.Where(s => s.IsActive || s.Id == existing?.SpecialtyId)
                .Select(s => new Choice<int>(s.Id, s.Name + (s.IsActive ? "" : " (ngừng hoạt động)"))),
            existing?.SpecialtyId ?? 0);
    }
    protected override object CreateRequest() => new SaveDoctorRequest
    {
        FullName = name.Text.Trim(), Title = title.Text.Trim(), Bio = bio.Text.Trim(),
        AvatarUrl = string.IsNullOrWhiteSpace(avatar.Text) ? null : avatar.Text.Trim(), IsActive = active.Checked,
        SpecialtyId = (specialty.SelectedItem as Choice<int>)?.Value ?? 0
    };
    protected override bool ValidateRequest(object request) => request is SaveDoctorRequest d &&
        (d.SpecialtyId > 0 || Error("SpecialtyId", "Vui lòng chọn một chuyên khoa."));
    protected override string Confirmation => active.Checked ? $"Lưu hồ sơ bác sĩ {name.Text.Trim()}?" : $"Ngừng hoạt động bác sĩ {name.Text.Trim()}? Lịch hẹn bị ảnh hưởng cần được nhân viên xử lý trước.";
    protected override async Task<object?> PersistAsync(object request) => await Session.SendAsync<DoctorDetailDto>(existing == null ? HttpMethod.Post : HttpMethod.Put,
        existing == null ? "api/admin/doctors" : $"api/admin/doctors/{existing.Id}", request, Lifetime.Token);
}

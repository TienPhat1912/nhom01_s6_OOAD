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
    private readonly CheckedListBox specialties;
    private readonly ComboBox primary;
    private readonly CheckBox active;
    public DoctorEditorForm(AdminSession session, IDesktopDialogs dialogs, List<SpecialtyDto> options, DoctorDetailDto? existing = null)
        : base(session, dialogs, existing == null ? "Thêm hồ sơ bác sĩ" : "Cập nhật hồ sơ bác sĩ",
            "Chọn ít nhất một chuyên khoa và một chuyên khoa chính. Hồ sơ có thể được tạo trước khi cấp tài khoản.")
    {
        this.existing = existing;
        name = TextField("FullName", "Họ và tên bác sĩ", 100, existing?.FullName);
        title = TextField("Title", "Chức danh", 50, existing?.Title);
        bio = TextField("Bio", "Giới thiệu", 1000, existing?.Bio, multiline: true);
        avatar = TextField("AvatarUrl", "Đường dẫn ảnh (/images/ hoặc HTTPS)", 500, existing?.AvatarUrl);
        active = AddField("IsActive", "Trạng thái", new CheckBox { Text = "Đang hoạt động", Checked = existing?.IsActive ?? true });
        specialties = AddField("SpecialtyIds", "Chuyên khoa phụ trách", new CheckedListBox { CheckOnClick = true, IntegralHeight = false, Font = UiTheme.Body() }, 210, true);
        foreach (var item in options.Where(s => s.IsActive || existing?.Specialties.Any(e => e.Id == s.Id) == true))
            specialties.Items.Add(new Choice<int>(item.Id, item.Name + (item.IsActive ? "" : " (ngừng hoạt động)")), existing?.Specialties.Any(s => s.Id == item.Id) == true);
        primary = ChoiceField("PrimarySpecialtyId", "Chuyên khoa chính", specialties.CheckedItems.Cast<Choice<int>>(), existing?.PrimarySpecialtyId ?? 0);
        if (primary.SelectedIndex < 0 && primary.Items.Count > 0) primary.SelectedIndex = 0;
        specialties.ItemCheck += (_, e) =>
        {
            var selected = specialties.Items.Cast<Choice<int>>().Where((_, index) => index == e.Index ? e.NewValue == CheckState.Checked : specialties.GetItemChecked(index)).ToList();
            var old = (primary.SelectedItem as Choice<int>)?.Value;
            primary.Items.Clear(); primary.Items.AddRange(selected.Cast<object>().ToArray());
            primary.SelectedItem = selected.FirstOrDefault(s => s.Value == old) ?? selected.FirstOrDefault();
        };
    }
    protected override object CreateRequest() => new SaveDoctorRequest
    {
        FullName = name.Text.Trim(), Title = title.Text.Trim(), Bio = bio.Text.Trim(),
        AvatarUrl = string.IsNullOrWhiteSpace(avatar.Text) ? null : avatar.Text.Trim(), IsActive = active.Checked,
        SpecialtyIds = specialties.CheckedItems.Cast<Choice<int>>().Select(s => s.Value).ToList(),
        PrimarySpecialtyId = (primary.SelectedItem as Choice<int>)?.Value ?? 0
    };
    protected override bool ValidateRequest(object request) => request is SaveDoctorRequest d &&
        (d.SpecialtyIds.Contains(d.PrimarySpecialtyId) || Error("PrimarySpecialtyId", "Chuyên khoa chính phải nằm trong danh sách đã chọn."));
    protected override string Confirmation => active.Checked ? $"Lưu hồ sơ bác sĩ {name.Text.Trim()}?" : $"Ngừng hoạt động bác sĩ {name.Text.Trim()}? Lịch hẹn bị ảnh hưởng cần được nhân viên xử lý trước.";
    protected override async Task<object?> PersistAsync(object request) => await Session.SendAsync<DoctorDetailDto>(existing == null ? HttpMethod.Post : HttpMethod.Put,
        existing == null ? "api/admin/doctors" : $"api/admin/doctors/{existing.Id}", request, Lifetime.Token);
}

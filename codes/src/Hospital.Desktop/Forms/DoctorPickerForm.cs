using System.ComponentModel;
using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Catalog;
using Hospital.Client;
using Hospital.Desktop.Design;
using Hospital.Desktop.Services;

namespace Hospital.Desktop.Forms;

public sealed class DoctorPickerForm : Form
{
    private readonly AdminSession session;
    private readonly bool unlinkedOnly;
    private readonly bool onlyActive;
    private readonly Guid? currentUserId;
    private readonly CancellationTokenSource lifetime = new();
    private readonly UiDataGrid grid = new();
    private readonly TextBox search = new() { Dock = DockStyle.Fill, AccessibleName = "Tìm hồ sơ bác sĩ", PlaceholderText = "Tên, mã hoặc chuyên khoa" };
    private readonly UiBanner banner = new() { Dock = DockStyle.Fill };
    private readonly UiButton find = new() { Text = "Tìm kiếm", Dock = DockStyle.Fill };
    private readonly UiButton choose = new() { Text = "Chọn bác sĩ", Enabled = false };
    private readonly UiButton previous = new() { Text = "Trước", Secondary = true, Width = 90 };
    private readonly UiButton next = new() { Text = "Sau", Secondary = true, Width = 90 };
    private int page = 1, total;
    private bool loading;
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DoctorDto? SelectedDoctor { get; private set; }
    public DoctorPickerForm(AdminSession session, bool unlinkedOnly = false, Guid? currentUserId = null, bool onlyActive = true)
    {
        this.session = session; this.unlinkedOnly = unlinkedOnly; this.currentUserId = currentUserId; this.onlyActive = onlyActive || unlinkedOnly;
        Text = "Chọn hồ sơ bác sĩ"; Font = UiTheme.Body(); AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(820, 580); MinimumSize = new Size(740, 500); StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false; BackColor = DesignTokens.Slate50;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), RowCount = 5, ColumnCount = 1 };
        foreach (var height in new[] { 50, 48, 76 }) root.RowStyles.Add(new(SizeType.Absolute, height));
        root.RowStyles.Add(new(SizeType.Percent, 100)); root.RowStyles.Add(new(SizeType.Absolute, 66));
        root.Controls.Add(UiTheme.Label(unlinkedOnly ? "Chọn hồ sơ hoạt động chưa liên kết tài khoản khác." : "Chọn bác sĩ để quản lý lịch làm việc.", 12, true), 0, 0);
        var toolbar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        toolbar.ColumnStyles.Add(new(SizeType.Percent, 100)); toolbar.ColumnStyles.Add(new(SizeType.Absolute, 140));
        toolbar.Controls.Add(search); toolbar.Controls.Add(find); root.Controls.Add(toolbar, 0, 1);
        root.Controls.Add(banner, 0, 2); root.Controls.Add(grid, 0, 3);
        var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
        var cancel = new UiButton { Text = "Hủy bỏ", Secondary = true, DialogResult = DialogResult.Cancel, Width = 100 };
        foreach (var control in new Control[] { choose, cancel, next, previous }) footer.Controls.Add(control);
        root.Controls.Add(footer, 0, 4); Controls.Add(root); CancelButton = cancel;
        find.Click += async (_, _) => { page = 1; await LoadAsync(); };
        search.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; page = 1; await LoadAsync(); } };
        previous.Click += async (_, _) => { page--; await LoadAsync(); }; next.Click += async (_, _) => { page++; await LoadAsync(); };
        grid.CurrentCellChanged += (_, _) => choose.Enabled = !loading && Eligible(grid.CurrentRow?.Tag as DoctorDto);
        choose.Click += (_, _) => { if (!Eligible(grid.CurrentRow?.Tag as DoctorDto)) return; SelectedDoctor = (DoctorDto)grid.CurrentRow!.Tag!; DialogResult = DialogResult.OK; Close(); };
        Shown += async (_, _) => await LoadAsync(); FormClosing += (_, _) => lifetime.Cancel();
    }
    private bool Eligible(DoctorDto? doctor) => doctor != null && (!onlyActive || doctor.IsActive) &&
        (!unlinkedOnly || doctor.LinkedUserId == null || doctor.LinkedUserId == currentUserId);
    private async Task LoadAsync()
    {
        if (loading || lifetime.IsCancellationRequested) return;
        loading = true; choose.Enabled = previous.Enabled = next.Enabled = find.Enabled = search.Enabled = false;
        grid.Rows.Clear(); grid.Columns.Clear(); banner.ShowMessage("Đang tìm hồ sơ…");
        try
        {
            var result = await session.GetAsync<PagedResult<DoctorDto>>($"api/doctors?onlyActive={onlyActive.ToString().ToLowerInvariant()}&page={page}&pageSize=10&keyword={Uri.EscapeDataString(search.Text.Trim())}", lifetime.Token);
            total = result.TotalCount;
            grid.Columns.Add("Code", "Mã bác sĩ"); grid.Columns.Add("Name", "Họ tên"); grid.Columns.Add("Specialties", "Chuyên khoa"); grid.Columns.Add("Account", "Tài khoản");
            foreach (var doctor in result.Items)
            {
                var row = grid.Rows[grid.Rows.Add(doctor.DoctorCode, doctor.FullName, doctor.SpecialtyName, doctor.LinkedUserId.HasValue ? "Đã liên kết" : "Chưa liên kết")];
                row.Tag = doctor;
                if (!Eligible(doctor)) row.DefaultCellStyle.ForeColor = DesignTokens.Slate500;
            }
            banner.ShowMessage(result.Items.Count == 0 ? "Không tìm thấy hồ sơ phù hợp. Thử từ khóa khác." : $"{total} hồ sơ · Trang {page}");
        }
        catch (HospitalApiException ex)
        { if (session.Expired) { DialogResult = DialogResult.Abort; Close(); } else { total = 0; banner.ShowMessage(AdminSession.Error(ex), true); } }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        finally
        {
            loading = false;
            if (!IsDisposed) { find.Enabled = search.Enabled = true; previous.Enabled = page > 1; next.Enabled = page * 10 < total; choose.Enabled = Eligible(grid.CurrentRow?.Tag as DoctorDto); }
        }
    }
    protected override void Dispose(bool disposing) { if (disposing && !IsDisposed) { lifetime.Cancel(); lifetime.Dispose(); } base.Dispose(disposing); }
}

import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

// Mutates only the isolated, demo-seeded database documented in phase5-verification.md.
test.describe.configure({ mode: 'serial' });
const patientName = `Phase 5 Patient ${Date.now()}`;
const username = `phase5_${Date.now()}`;
let receivedId;
let receivedStatus;
let receivingDoctor = 'doctor1';

async function inspect(page, name) {
  const result = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa']).analyze();
  expect(result.violations.map(v => ({ id: v.id, nodes: v.nodes.map(n => n.target) }))).toEqual([]);
  for (const width of [320, 360, 430, 768, 1440]) {
    await page.setViewportSize({ width, height: 900 });
    expect(await page.evaluate(() => document.documentElement.scrollWidth), name).toBeLessThanOrEqual(width + 1);
    if (width === 360) await page.screenshot({ path: `artifacts/phase5-${name}-mobile.png`, fullPage: true });
  }
  await page.screenshot({ path: `artifacts/phase5-${name}.png`, fullPage: true });
}
async function confirm(page, buttonName) {
  await page.getByRole('button', { name: buttonName, exact: true }).click();
  await expect(page.getByRole('dialog')).toBeVisible();
  await page.getByRole('button', { name: 'Xác nhận', exact: true }).click();
}
async function login(page, name, password) {
  await page.goto('/Account/Login');
  await page.locator('#Username').fill(name); await page.locator('#Password').fill(password);
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await expect(page).toHaveURL(/\/Dashboard$/);
}
async function chooseDoctor(page, specialty = 'Nội tổng quát') {
  await page.getByRole('link', { name: `Chọn ${specialty}`, exact: true }).click();
  await page.locator('main a[href*="/Booking/Slots"]').first().click();
  await expect(page.locator('#date')).toBeVisible();
}

test('patient registers, books, reschedules, cancels and changes password through real API', async ({ page }) => {
  const errors = []; page.on('pageerror', error => errors.push(error.message));
  await page.goto('/Account/Register'); await inspect(page, 'register');
  await page.locator('#Username').fill(username); await page.locator('#FullName').fill(patientName);
  await page.locator('#Email').fill(`${username}@example.test`); await page.locator('#PhoneNumber').fill('0901234567');
  await page.locator('#DateOfBirth').fill('1995-01-01');
  await page.locator('#Password').fill('PhaseFive@123'); await page.locator('#ConfirmPassword').fill('PhaseFive@123');
  await page.getByRole('button', { name: 'Tạo tài khoản' }).click();
  await expect(page).toHaveURL(/\/Dashboard$/);
  await page.goto('/Booking'); await inspect(page, 'specialties');
  await page.getByRole('link', { name: 'Chọn Nội tổng quát', exact: true }).click(); await inspect(page, 'doctors');
  await page.locator('main a[href*="/Booking/Slots"]').first().click();
  // Last available day leaves the full patient cancellation/rescheduling window regardless of test time.
  const dates = await page.locator('#date option').evaluateAll(options => options.map(o => o.value));
  await page.locator('#date').selectOption(dates.at(-1));
  await page.getByRole('button', { name: 'Xem khung giờ', exact: true }).click();
  await inspect(page, 'slots');
  await page.locator('input[name="TimeSlotId"]').first().check();
  await page.locator('#Notes').fill('Phase 5 browser appointment');
  await page.getByRole('button', { name: 'Kiểm tra thông tin' }).click(); await inspect(page, 'review');
  await confirm(page, 'Xác nhận đặt lịch'); await expect(page).toHaveURL(/\/Booking\/Receipt\//);
  const id = page.url().split('/').at(-1); await inspect(page, 'receipt');
  await page.emulateMedia({ media: 'print' });
  await expect(page.locator('.receipt')).toBeVisible(); await expect(page.locator('.sidebar')).not.toBeVisible();
  await page.emulateMedia({ media: 'screen' });
  await page.getByRole('link', { name: 'Quản lý lịch hẹn' }).click();
  await page.getByRole('link', { name: 'Đổi lịch', exact: true }).click(); await chooseDoctor(page);
  await page.locator('#date').selectOption(dates.at(-1)); await page.getByRole('button', { name: 'Xem khung giờ', exact: true }).click();
  await page.locator('input[name="TimeSlotId"]').first().check(); await page.locator('#Reason').fill('Đổi giờ theo nhu cầu');
  await page.getByRole('button', { name: 'Kiểm tra thông tin' }).click(); await confirm(page, 'Xác nhận đổi lịch');
  await expect(page).toHaveURL(new RegExp(`/Booking/Receipt/${id}$`));
  await page.getByRole('link', { name: 'Quản lý lịch hẹn' }).click(); await page.getByRole('link', { name: 'Hủy lịch', exact: true }).click();
  await inspect(page, 'cancel'); await confirm(page, 'Hủy lịch hẹn');
  await expect(page.locator('.card-header .badge')).toContainText('Đã hủy');
  await page.goto('/Notifications'); await expect(page.locator('main')).toContainText('Hủy lịch thành công');
  await page.goto('/Appointments?descending=true'); await inspect(page, 'filters');
  await page.goto('/Account/ChangePassword'); await inspect(page, 'password');
  await page.locator('#OldPassword').fill('PhaseFive@123'); await page.locator('#NewPassword').fill('ChangedPhaseFive@123');
  await page.locator('#ConfirmPassword').fill('ChangedPhaseFive@123'); await confirm(page, 'Lưu mật khẩu');
  await expect(page).toHaveURL(/\/Account\/Login$/); await expect(page.locator('.alert-success')).toContainText('Mật khẩu');
  await login(page, username, 'ChangedPhaseFive@123'); expect(errors).toEqual([]);
});

test('staff finds existing patient, books with reason, cancels and receives patient', async ({ page }) => {
  await login(page, 'staff1', 'Staff@123');
  await page.goto('/Patients'); await page.locator('#keyword').fill(patientName); await page.getByRole('button', { name: 'Tìm kiếm' }).click();
  await inspect(page, 'patients'); await page.getByRole('link', { name: `Đặt lịch cho ${patientName}`, exact: true }).click();
  await chooseDoctor(page);
  const dates = await page.locator('#date option').evaluateAll(options => options.map(o => o.value));
  await page.locator('#date').selectOption(dates.at(-1)); await page.getByRole('button', { name: 'Xem khung giờ', exact: true }).click();
  await page.locator('input[name="TimeSlotId"]').first().check(); await page.getByRole('button', { name: 'Kiểm tra thông tin' }).click();
  await confirm(page, 'Xác nhận đặt lịch'); await page.getByRole('link', { name: 'Quản lý lịch hẹn' }).click();
  await page.getByRole('link', { name: 'Hủy lịch', exact: true }).click();
  await page.getByRole('button', { name: 'Hủy lịch hẹn', exact: true }).click(); await expect(page.locator('#Reason')).toBeFocused();
  await expect(page.getByRole('dialog')).not.toBeVisible(); await page.locator('#Reason').fill('Bệnh nhân yêu cầu hủy qua quầy tiếp nhận');
  await confirm(page, 'Hủy lịch hẹn'); await expect(page.locator('.alert-success')).toBeVisible();
  await page.goto('/Patients?keyword=' + encodeURIComponent(patientName));
  await page.getByRole('link', { name: `Đặt lịch cho ${patientName}`, exact: true }).click(); await chooseDoctor(page);
  const today = new Date(Date.now() + 7 * 60 * 60 * 1000).toISOString().slice(0, 10);
  if (await page.locator(`#date option[value="${today}"]`).count() === 0) {
    await page.goto('/Patients?keyword=' + encodeURIComponent(patientName));
    await page.getByRole('link', { name: `Đặt lịch cho ${patientName}`, exact: true }).click();
    await chooseDoctor(page, 'Nhi khoa'); receivingDoctor = 'doctor2';
  }
  const hasToday = await page.locator(`#date option[value="${today}"]`).count() > 0;
  if (hasToday) { await page.locator('#date').selectOption(today); await page.getByRole('button', { name: 'Xem khung giờ', exact: true }).click(); }
  await page.locator('input[name="TimeSlotId"]').first().check(); await page.getByRole('button', { name: 'Kiểm tra thông tin' }).click();
  await confirm(page, 'Xác nhận đặt lịch'); receivedId = page.url().split('/').at(-1);
  await page.getByRole('link', { name: 'Quản lý lịch hẹn' }).click();
  if (hasToday) {
    await page.getByRole('link', { name: 'Tiếp nhận bệnh nhân', exact: true }).click();
    await inspect(page, 'check-in'); await page.locator('#PatientVerified').check(); await confirm(page, 'Tiếp nhận bệnh nhân');
    receivedStatus = 'Đã tiếp nhận';
  } else {
    // After all seeded shifts end, verify tomorrow's appointment cannot be received early.
    // The deterministic Web/API integration suite always exercises successful check-in and no-show.
    await expect(page.getByRole('button', { name: 'Tiếp nhận không khả dụng' })).toBeDisabled();
    receivedStatus = 'Đã xác nhận';
  }
  await expect(page.locator('.card-header .badge')).toContainText(receivedStatus);
});

test('doctor reads own reception result and is denied booking and staff functions', async ({ page }) => {
  await login(page, receivingDoctor, 'Doctor@123'); await page.goto(`/Appointments/Detail/${receivedId}`);
  await expect(page.locator('.card-header .badge')).toContainText(receivedStatus);
  await expect(page.locator('main')).toContainText(patientName); await inspect(page, 'doctor-detail');
  await expect(page.getByRole('link', { name: 'Hủy lịch', exact: true })).toHaveCount(0);
  await page.goto('/Appointments'); await page.locator('#keyword').fill(patientName); await page.getByRole('button', { name: 'Tìm kiếm' }).click();
  await expect(page.locator('tbody')).toContainText(patientName);
  expect((await page.goto('/Booking')).status()).toBe(403); expect((await page.goto('/Patients')).status()).toBe(403);
});

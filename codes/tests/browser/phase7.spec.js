import { test, expect } from '@playwright/test';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import { readFile, writeFile, appendFile, mkdir } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const exec = promisify(execFile);
const root = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
const fixturePath = process.env.HOSPITAL_PHASE7_FIXTURE;

test('Phase 7: Desktop setup, Web booking/reception, concurrent patients and Desktop closure', async ({ browser, baseURL }) => {
  test.skip(!fixturePath, 'Run scripts/verify-phase7.ps1 against an isolated database.');
  test.setTimeout(240_000);
  const output = dirname(fixturePath); await mkdir(output, { recursive: true });
  async function desktop(stage) {
    const result = await exec('dotnet', ['tests/Hospital.DesktopSmoke/bin/Debug/net9.0-windows/Hospital.DesktopSmoke.dll', '--phase7', stage, fixturePath],
      { cwd: root, windowsHide: true, timeout: 60_000, env: process.env });
    await appendFile(resolve(output, 'desktop.log'), result.stdout + result.stderr);
    expect(result.stdout).toContain(`PHASE7 ${stage}:`);
  }
  await desktop('setup');
  const data = JSON.parse(await readFile(fixturePath, 'utf8'));
  const contexts = []; const errors = [];
  async function login(username, password) {
    const context = await browser.newContext({ baseURL }); contexts.push(context);
    const page = await context.newPage(); page.on('pageerror', error => errors.push(error.message));
    await page.goto('/Account/Login'); await page.locator('#Username').fill(username); await page.locator('#Password').fill(password);
    await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click(); await expect(page).toHaveURL(/\/Dashboard$/); return page;
  }
  async function confirm(page, label) {
    await page.getByRole('button', { name: label, exact: true }).click();
    await expect(page.getByRole('dialog')).toBeVisible(); await page.getByRole('button', { name: 'Xác nhận', exact: true }).click();
  }
  async function review(page, slot, date, appointmentId) {
    await page.goto('/Booking/Start' + (appointmentId ? `?appointmentId=${appointmentId}` : ''));
    await page.getByRole('link', { name: `Chọn ${data.specialtyName}`, exact: true }).click();
    await expect(page.locator('main')).toContainText(data.doctorName);
    await page.locator('main a[href*="/Booking/Slots"]').first().click();
    await page.locator('#date').selectOption(date); await page.getByRole('button', { name: 'Xem khung giờ', exact: true }).click();
    await page.locator(`input[name="TimeSlotId"][value="${slot}"]`).check();
    if (appointmentId) await page.locator('#Reason').fill('Phase 7 đổi lịch qua Web');
    else await page.locator('#Notes').fill('Phase 7 browser/Desktop integration');
    await page.getByRole('button', { name: 'Kiểm tra thông tin' }).click();
    await expect(page.getByRole('heading', { name: 'Kiểm tra và xác nhận', exact: true })).toBeVisible();
  }
  async function book(page, slot, date) {
    await review(page, slot, date); await confirm(page, 'Xác nhận đặt lịch');
    await expect(page).toHaveURL(/\/Booking\/Receipt\/[0-9a-f-]+$/); return page.url().split('/').at(-1);
  }
  async function cancel(page, id) {
    await page.goto(`/Reception/Cancel/${id}`); await confirm(page, 'Hủy lịch hẹn');
    await expect(page.locator('.card-header .badge')).toContainText('Đã hủy');
  }
  try {
    const patient = await login('patient1', 'Patient@123');
    const other = await login('patient2', 'Patient@123');
    const staff = await login('staff1', 'Staff@123');
    const doctor = await login(data.doctorUsername, 'Doctor@123');
    await patient.goto('/Catalog?keyword=' + encodeURIComponent(data.doctorName));
    await expect(patient.locator('main')).toContainText(data.doctorName);
    await patient.screenshot({ path: resolve(output, 'patient-catalog.png'), fullPage: true });

    const todayAppointmentId = await book(patient, data.todaySlotId, data.today);
    await doctor.goto(`/Appointments/Detail/${todayAppointmentId}`); await expect(doctor.locator('.card-header .badge')).toContainText('Đã xác nhận');
    expect((await other.goto(`/Appointments/Detail/${todayAppointmentId}`)).status()).toBe(404);
    await desktop('confirmed');
    await staff.goto(`/Reception/CheckIn/${todayAppointmentId}`); await staff.locator('#PatientVerified').check(); await confirm(staff, 'Tiếp nhận bệnh nhân');
    await expect(staff.locator('.card-header .badge')).toContainText('Đã tiếp nhận');
    for (const page of [patient, doctor]) {
      await page.goto(`/Appointments/Detail/${todayAppointmentId}`); await expect(page.locator('.card-header .badge')).toContainText('Đã tiếp nhận');
    }
    await patient.screenshot({ path: resolve(output, 'patient-received.png'), fullPage: true }); await desktop('received');

    await review(patient, data.futureSlots[0], data.futureDate);
    const sent = patient.waitForRequest(r => r.method() === 'POST' && new URL(r.url()).pathname === '/Booking/Confirm');
    await confirm(patient, 'Xác nhận đặt lịch'); const originalRequest = await sent;
    await expect(patient).toHaveURL(/\/Booking\/Receipt\//); const lifecycleId = patient.url().split('/').at(-1);
    const repeat = await patient.request.post('/Booking/Confirm', { form: Object.fromEntries(new URLSearchParams(originalRequest.postData())), maxRedirects: 0 });
    expect(repeat.status()).toBe(302); expect(repeat.headers().location).toBe(`/Booking/Receipt/${lifecycleId}`);
    await review(patient, data.futureSlots[1], data.futureDate, lifecycleId); await confirm(patient, 'Xác nhận đổi lịch');
    await expect(patient).toHaveURL(new RegExp(`/Booking/Receipt/${lifecycleId}$`)); await cancel(patient, lifecycleId);
    await patient.goto('/Notifications');
    const lifecycleNotifications = patient.locator('article.notification').filter({ has: patient.locator(`a[href="/Appointments/Detail/${lifecycleId}"]`) });
    await expect(lifecycleNotifications).toHaveCount(3);
    for (const title of ['Đặt lịch thành công', 'Đổi lịch thành công', 'Hủy lịch thành công']) await expect(lifecycleNotifications.filter({ hasText: title })).toHaveCount(1);
    await lifecycleNotifications.first().getByRole('button', { name: 'Đánh dấu đã đọc' }).click();
    await expect(lifecycleNotifications.getByRole('button', { name: 'Đánh dấu đã đọc' })).toHaveCount(2);

    // Both patients review the same still-open slot, then release both POSTs together.
    await review(patient, data.futureSlots[2], data.futureDate); await review(other, data.futureSlots[2], data.futureDate);
    let arrivals = 0; let release; const gate = new Promise(resolveGate => { release = resolveGate; });
    const route = async request => { if (++arrivals === 2) release(); await gate; await request.continue(); };
    await patient.route('**/Booking/Confirm', route); await other.route('**/Booking/Confirm', route);
    let raceId;
    try {
      const responses = Promise.all([patient, other].map(page => page.waitForResponse(r => r.request().method() === 'POST' && new URL(r.url()).pathname === '/Booking/Confirm')));
      await Promise.all([patient, other].map(page => confirm(page, 'Xác nhận đặt lịch')));
      const results = await responses; expect(results.map(r => r.status()).sort()).toEqual([200, 302]); expect(arrivals).toBe(2);
      const winner = [patient, other][results.findIndex(r => r.status() === 302)]; const loser = winner === patient ? other : patient;
      await expect(winner).toHaveURL(/\/Booking\/Receipt\//); raceId = winner.url().split('/').at(-1);
      await expect(loser.locator('.validation-summary-errors')).toBeVisible();
      await loser.screenshot({ path: resolve(output, 'concurrent-loser.png'), fullPage: true });
      await staff.goto(`/Appointments?fromDate=${data.futureDate}&toDate=${data.futureDate}&doctorId=${data.doctorId}&status=Confirmed`);
      await expect(staff.locator('tbody tr')).toHaveCount(1); await expect(staff.locator(`a[href="/Appointments/Detail/${raceId}"]`)).toBeVisible();
      await cancel(winner, raceId);
    } finally { release(); await patient.unroute('**/Booking/Confirm', route); await other.unroute('**/Booking/Confirm', route); }
    await desktop('released');
    await patient.goto('/Booking/Start'); await patient.getByRole('link', { name: `Chọn ${data.specialtyName}`, exact: true }).click();
    await patient.locator('main a[href*="/Booking/Slots"]').first().click(); await patient.locator('#date').selectOption(data.futureDate);
    await patient.getByRole('button', { name: 'Xem khung giờ', exact: true }).click();
    await expect(patient.locator(`input[value="${data.futureSlots[0]}"]`)).toHaveCount(0);
    await expect(patient.locator(`input[value="${data.futureSlots[2]}"]`)).toBeVisible();
    expect(errors).toEqual([]);
    await writeFile(resolve(output, 'result.json'), JSON.stringify({ ...data, todayAppointmentId, lifecycleId, raceId, concurrentRequests: arrivals }, null, 2));
  } finally { await Promise.all(contexts.map(context => context.close())); }
});

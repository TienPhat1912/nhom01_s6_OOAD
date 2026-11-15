import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import { fileURLToPath } from 'node:url';

const output = process.env.HOSPITAL_PHASE8_OUTPUT;
const manifest = [];
let fixture;
test.beforeAll(async () => {
  if (!output) return;
  await mkdir(output, { recursive: true });
  fixture = JSON.parse(await readFile(resolve(dirname(process.env.HOSPITAL_PHASE7_FIXTURE), 'result.json'), 'utf8'));
});
test.beforeEach(() => test.skip(!output, 'Run verify-phase7.ps1 -FinalQa on an isolated database.'));
test.afterAll(async () => {
  if (!output) return;
  await writeFile(resolve(output, 'pages.json'), JSON.stringify(manifest, null, 2));
  const cards = manifest.map(p => `<article><h2>${p.name}</h2><p>${p.path}</p>${p.images.map(i => `<a href="${i}"><img src="${i}" alt="${p.name}" loading="lazy"></a>`).join('')}</article>`).join('');
  await writeFile(resolve(output, 'index.html'), `<!doctype html><html lang="vi"><meta charset="utf-8"><title>Phase 8 — Kiểm tra giao diện</title><style>body{font:16px system-ui;background:#f4f7fa;color:#163430;margin:32px}main{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:24px}article{background:white;padding:16px;border-radius:12px}h2{font-size:18px}img{width:48%;height:420px;object-fit:contain;object-position:top;vertical-align:top}p{overflow-wrap:anywhere}</style><h1>Phase 8 — ${manifest.length} màn hình</h1><p>Bấm ảnh để xem toàn bộ ở độ phân giải gốc. Dữ liệu từ API/SQL Server thử nghiệm.</p><main>${cards}</main></html>`);
});
async function login(page, user, password) {
  await page.goto('/Account/Login'); await page.locator('#Username').fill(user); await page.locator('#Password').fill(password);
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click(); await expect(page).toHaveURL(/\/Dashboard$/);
}
async function inspect(page, name) {
  await expect(page.locator('main h1')).toBeVisible();
  const images = [];
  for (const width of [1440, 768, 320, 360]) {
    await page.setViewportSize({ width, height: 900 });
    expect(await page.evaluate(() => document.documentElement.scrollWidth), name).toBeLessThanOrEqual(width + 1);
    if (width === 1440) {
      const result = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa']).analyze();
      expect(result.violations.map(v => ({ id: v.id, nodes: v.nodes.map(n => n.target) })), name).toEqual([]);
    }
    if (width === 1440 || width === 360) {
      const file = `${name}-${width}.png`; images.push(file);
      await page.screenshot({ path: resolve(output, file), fullPage: true });
    }
  }
  manifest.push({ name, path: new URL(page.url()).pathname, images });
}
test('final public pages and registration validation', async ({ page }) => {
  for (const [name, path] of Object.entries({ home: '/', privacy: '/Home/Privacy', login: '/Account/Login', register: '/Account/Register', design: '/Design' })) {
    expect((await page.goto(path)).status()).toBe(200); await inspect(page, name);
  }
  await page.goto('/Account/Register'); await page.locator('main button[type=submit]').click();
  expect(await page.locator('input:invalid').count()).toBeGreaterThan(0);
});
for (const role of ['patient', 'staff', 'doctor', 'admin']) {
  test(`final ${role} pages with live data`, async ({ page }) => {
    test.setTimeout(240_000);
    const errors = []; page.on('pageerror', e => errors.push(e.message));
    const users = { patient: ['patient1', 'Patient@123'], staff: ['staff1', 'Staff@123'], doctor: ['doctor1', 'Doctor@123'], admin: ['admin', 'Admin@123'] };
    await login(page, ...users[role]);
    const paths = { dashboard: '/Dashboard', profile: '/Account/Profile', password: '/Account/ChangePassword' };
    if (role !== 'admin') paths.appointments = '/Appointments';
    if (role === 'doctor') paths.schedule = '/Schedule';
    else Object.assign(paths, { doctors: '/Catalog', specialties: '/Catalog/Specialties', 'doctor-detail': `/Catalog/Doctor/${fixture.doctorId}` });
    if (role === 'staff') paths.patients = '/Patients';
    if (role === 'patient') Object.assign(paths, { notifications: '/Notifications', detail: `/Appointments/Detail/${fixture.todayAppointmentId}`, receipt: `/Booking/Receipt/${fixture.lifecycleId}`, 'cancel-unavailable': `/Reception/Cancel/${fixture.lifecycleId}`, empty: '/Appointments?keyword=NO_MATCH_PHASE8' });
    if (role === 'staff') Object.assign(paths, { detail: `/Appointments/Detail/${fixture.todayAppointmentId}`, checkin: `/Reception/CheckIn/${fixture.todayAppointmentId}`, noshow: `/Reception/NoShow/${fixture.todayAppointmentId}`, cancel: `/Reception/Cancel/${fixture.lifecycleId}` });
    for (const [name, path] of Object.entries(paths)) {
      expect((await page.goto(path)).status()).toBe(200); await inspect(page, `${role}-${name}`);
    }
    if (role === 'doctor') {
      await page.goto('/Appointments'); await page.locator('a[href*="/Appointments/Detail/"]').first().click(); await inspect(page, 'doctor-detail');
    }
    if (role === 'patient') {
      await page.goto('/Booking/Start'); await inspect(page, 'booking-specialty');
      await page.getByRole('link', { name: `Chọn ${fixture.specialtyName}`, exact: true }).click(); await inspect(page, 'booking-doctor');
      await page.locator('main a[href*="/Booking/Slots"]').first().click();
      await page.locator('#date').selectOption(fixture.futureDate); await page.getByRole('button', { name: 'Xem khung giờ' }).click(); await inspect(page, 'booking-slots');
      await page.locator(`input[value="${fixture.futureSlots[1]}"]`).check(); await page.getByRole('button', { name: 'Kiểm tra thông tin' }).click(); await inspect(page, 'booking-review');
      await page.getByRole('button', { name: 'Xác nhận đặt lịch', exact: true }).click();
      await expect(page.getByRole('dialog')).toBeVisible(); await page.screenshot({ path: resolve(output, 'booking-confirm-dialog.png'), fullPage: true });
      await page.keyboard.press('Escape'); await expect(page.getByRole('dialog')).not.toBeVisible();
      const bad = '/Booking?context=expired&keyword=phase8';
      expect((await page.goto(bad)).status()).toBe(400); await expect(page.getByRole('link', { name: 'Thử lại' })).toHaveAttribute('href', bad); await inspect(page, 'booking-expired');
      expect((await page.goto('/Schedule')).status()).toBe(403); await inspect(page, 'forbidden');
      expect((await page.goto('/Catalog/Doctor/00000000-0000-0000-0000-000000000001')).status()).toBe(404); await inspect(page, 'not-found');
      await page.goto(`/Booking/Receipt/${fixture.lifecycleId}`); await page.emulateMedia({ media: 'print' });
      await expect(page.locator('.receipt')).toBeVisible(); await expect(page.locator('.sidebar')).not.toBeVisible();
      await page.pdf({ path: resolve(output, 'receipt.pdf'), format: 'A4' }); await page.emulateMedia({ media: 'screen' });
    }
    expect(errors).toEqual([]);
  });
}
test('mobile menu isolates background, closes on resize and remains usable on short screens', async ({ page }) => {
  await login(page, 'patient2', 'Patient@123');
  await page.setViewportSize({ width: 640, height: 360 });
  const toggle = page.locator('[data-nav-toggle]'); await toggle.click();
  await expect(page.locator('.workspace')).toHaveAttribute('inert', '');
  await page.locator('.logout-button').scrollIntoViewIfNeeded(); await expect(page.locator('.logout-button')).toBeInViewport();
  await page.keyboard.press('Escape'); await expect(toggle).toBeFocused();
  await toggle.click(); await page.locator('[data-sidebar-close]').click(); await expect(toggle).toBeFocused();
  await toggle.click(); await page.setViewportSize({ width: 1440, height: 720 });
  await expect(page.locator('.nav-scrim')).not.toBeVisible(); await expect(page.locator('.workspace')).not.toHaveAttribute('inert', '');
  await expect(page.locator('.logout-button')).toBeInViewport();
  await page.setViewportSize({ width: 360, height: 800 }); await toggle.click();
  const result = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa']).analyze(); expect(result.violations).toEqual([]);
  await page.screenshot({ path: resolve(output, 'mobile-menu.png'), fullPage: true });
});
test('final Desktop forms render with real data at standard and minimum sizes', async () => {
  const root = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
  const result = await promisify(execFile)('dotnet', ['tests/Hospital.DesktopSmoke/bin/Debug/net9.0-windows/Hospital.DesktopSmoke.dll', '--phase8', process.env.HOSPITAL_PHASE7_FIXTURE], { cwd: root, env: process.env, windowsHide: true, timeout: 90_000 });
  await writeFile(resolve(output, 'desktop.log'), result.stdout + result.stderr);
  expect(result.stdout).toContain('PHASE8 Desktop visual review:');
});

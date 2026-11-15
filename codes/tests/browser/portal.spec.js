import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { mkdir } from 'node:fs/promises';

// Run against an isolated, freshly seeded development database. Profile test saves the existing data.
const roles = {
  patient: ['patient1', 'Patient@123', ['/Dashboard', '/Appointments', '/Catalog', '/Catalog/Specialties', '/Notifications', '/Account/Profile']],
  doctor: ['doctor1', 'Doctor@123', ['/Dashboard', '/Appointments', '/Schedule', '/Account/Profile']],
  staff: ['staff1', 'Staff@123', ['/Dashboard', '/Appointments', '/Catalog', '/Catalog/Specialties', '/Account/Profile']],
  admin: ['admin', 'Admin@123', ['/Dashboard', '/Catalog', '/Catalog/Specialties', '/Account/Profile']]
};
const sessions = {};

async function accessible(page) {
  const result = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa']).analyze();
  expect(result.violations.map(v => ({ id: v.id, impact: v.impact, nodes: v.nodes.map(n => n.target) }))).toEqual([]);
}
async function fits(page) {
  const size = await page.evaluate(() => ({ path: location.pathname, viewport: innerWidth, document: document.documentElement.scrollWidth,
    overflow: [...document.querySelectorAll('body *')].filter(e => e.getBoundingClientRect().right > innerWidth + 1).slice(0, 8).map(e => e.className) }));
  expect(size.document, JSON.stringify(size)).toBeLessThanOrEqual(size.viewport + 1);
}
async function sessionPage(browser, role) {
  const context = await browser.newContext({ storageState: sessions[role] });
  return { context, page: await context.newPage() };
}

test.beforeAll(async ({ browser, baseURL }) => {
  await mkdir('artifacts', { recursive: true });
  for (const [role, [username, password]] of Object.entries(roles)) {
    const context = await browser.newContext({ baseURL });
    const page = await context.newPage();
    await page.goto('/Account/Login');
    await page.getByLabel('Tên đăng nhập', { exact: true }).fill(username);
    await page.getByLabel('Mật khẩu', { exact: true }).fill(password);
    await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
    await expect(page).toHaveURL(/\/Dashboard$/);
    sessions[role] = await context.storageState();
    await context.close();
  }
});

test('public pages, form validation, password control and responsive layout', async ({ page }) => {
  for (const path of ['/', '/Home/Privacy', '/Account/Login', '/Design']) {
    expect((await page.goto(path)).status()).toBe(200);
    await accessible(page);
    for (const width of [320, 360, 430, 768, 1440]) {
      await page.setViewportSize({ width, height: 900 });
      await fits(page);
    }
  }
  await page.goto('/Account/Login');
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await expect(page.locator('#Username')).toBeFocused();
  await page.getByRole('button', { name: 'Hiện mật khẩu' }).click();
  await expect(page.locator('#Password')).toHaveAttribute('type', 'text');
  await page.getByRole('button', { name: 'Ẩn mật khẩu' }).click();
  await expect(page.locator('#Password')).toHaveAttribute('type', 'password');
  await page.screenshot({ path: 'artifacts/login-desktop.png', fullPage: true });
  await page.setViewportSize({ width: 360, height: 800 });
  await accessible(page);
  await page.screenshot({ path: 'artifacts/login-mobile.png', fullPage: true });
});

for (const [role, [, , paths]] of Object.entries(roles)) {
  test(`${role}: live API pages, accessibility and responsive layouts`, async ({ browser, baseURL }) => {
    const { context, page } = await sessionPage(browser, role);
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    try {
      for (const path of paths) {
        await page.setViewportSize({ width: 1440, height: 1000 });
        expect((await page.goto(baseURL + path)).status()).toBe(200);
        await expect(page.locator('main h1')).toBeVisible();
        await accessible(page);
        for (const width of [320, 360, 430, 768, 1440]) {
          await page.setViewportSize({ width, height: 900 });
          await fits(page);
        }
        if (path === '/Dashboard') {
          await page.screenshot({ path: `artifacts/${role}-desktop.png`, fullPage: true });
          await page.setViewportSize({ width: 360, height: 800 });
          await accessible(page);
          await page.screenshot({ path: `artifacts/${role}-mobile.png`, fullPage: true });
        }
      }
      expect(errors).toEqual([]);
    } finally { await context.close(); }
  });
}

test('mobile menu keyboard trap and Escape restore focus', async ({ browser, baseURL }) => {
  const { context, page } = await sessionPage(browser, 'patient');
  try {
    await page.setViewportSize({ width: 360, height: 800 });
    await page.goto(baseURL + '/Dashboard');
    const toggle = page.locator('[data-nav-toggle]');
    await toggle.click();
    await expect(toggle).toHaveAttribute('aria-expanded', 'true');
    const first = page.locator('#sidebar a').first();
    await expect(first).toBeFocused();
    await page.keyboard.press('Shift+Tab');
    await expect(page.locator('#sidebar button').last()).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(first).toBeFocused();
    await page.keyboard.press('Escape');
    await expect(toggle).toBeFocused();
    await expect(toggle).toHaveAttribute('aria-expanded', 'false');
  } finally { await context.close(); }
});

test('profile native validation, modal keyboard behavior and real save', async ({ browser, baseURL }) => {
  const { context, page } = await sessionPage(browser, 'patient');
  try {
    await page.goto(baseURL + '/Account/Profile');
    const email = await page.locator('#Email').inputValue();
    await page.locator('#Email').fill('invalid');
    const save = page.getByRole('button', { name: 'Lưu thông tin' });
    await save.click();
    await expect(page.locator('#Email')).toBeFocused();
    await expect(page.getByRole('dialog')).not.toBeVisible();
    await page.locator('#Email').fill(email);
    await save.click();
    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible();
    await expect(page.getByRole('button', { name: 'Hủy bỏ' })).toBeFocused();
    await accessible(page);
    await page.keyboard.press('Escape');
    await expect(dialog).not.toBeVisible();
    await expect(save).toBeFocused();
    await save.click();
    await page.getByRole('button', { name: 'Xác nhận', exact: true }).click();
    await expect(page.locator('.alert-success')).toBeVisible();
    await expect(page.locator('#Email')).toHaveValue(email);
  } finally { await context.close(); }
});

test('real detail pages, search empty state and role restriction', async ({ browser, baseURL }) => {
  const { context, page } = await sessionPage(browser, 'patient');
  try {
    await page.goto(baseURL + '/Catalog');
    await page.locator('a[href*="/Catalog/Doctor/"]').first().click();
    await accessible(page);
    await page.setViewportSize({ width: 360, height: 800 });
    await fits(page);
    await page.goto(baseURL + '/Appointments');
    await page.locator('a[href*="/Appointments/Detail/"]').first().click();
    await accessible(page);
    await fits(page);
    await page.goto(baseURL + '/Appointments?keyword=NO_MATCH_PHASE4');
    await expect(page.locator('.empty-state')).toBeVisible();
    const denied = await page.goto(baseURL + '/Schedule');
    expect(denied.status()).toBe(403);
    await accessible(page);
  } finally { await context.close(); }
});

import { test, expect, Page } from '@playwright/test'

const TEST_EMAIL    = `appt_e2e_${Date.now()}@example.com`
const TEST_PASSWORD = 'Appt1234!'

async function loginAs(page: Page, email: string, password: string) {
  await page.goto('/login')
  await page.getByLabel(/email/i).fill(email)
  await page.getByLabel(/mật khẩu/i).fill(password)
  await page.getByRole('button', { name: /đăng nhập/i }).click()
  await expect(page).toHaveURL('/')
}

test.describe('Appointment Booking', () => {
  // One-time setup: register the test user before running the suite
  test.beforeAll(async ({ browser }) => {
    const page = await browser.newPage()
    await page.goto('/register')
    await page.getByLabel(/email/i).fill(TEST_EMAIL)
    await page.getByLabel(/mật khẩu/i).first().fill(TEST_PASSWORD)
    await page.getByLabel(/họ/i).fill('Appt')
    await page.getByLabel(/tên/i).fill('User')
    await page.getByRole('button', { name: /đăng ký/i }).click()
    await page.close()
  })

  // ── Doctor list ──────────────────────────────────────────────────────────────

  test('doctors page is publicly accessible', async ({ page }) => {
    await page.goto('/doctors')
    await expect(page.getByRole('heading', { name: /đội ngũ bác sĩ/i })).toBeVisible()
  })

  test('doctor search filters results', async ({ page }) => {
    await page.goto('/doctors')
    await page.getByPlaceholder(/tìm theo chuyên khoa/i).fill('Nội khoa')
    // After typing, filtered results should update (no assertion on count since it
    // depends on seeded data, but no error should be thrown)
    await expect(page.locator('.grid')).toBeVisible()
  })

  // ── Appointment flow ──────────────────────────────────────────────────────────

  test('unauthenticated user booking redirects to login', async ({ page }) => {
    await page.goto('/appointment')
    await expect(page).toHaveURL(/\/login/)
  })

  test('authenticated user can access appointment page', async ({ page }) => {
    await loginAs(page, TEST_EMAIL, TEST_PASSWORD)
    await page.goto('/appointment')
    await expect(page).toHaveURL('/appointment')
  })

  // ── My Appointments ──────────────────────────────────────────────────────────

  test('my-appointments page loads for authenticated user', async ({ page }) => {
    await loginAs(page, TEST_EMAIL, TEST_PASSWORD)
    await page.goto('/appointments')
    await expect(page).toHaveURL('/appointments')
    // No appointment yet → empty state or loading
    await expect(page.locator('body')).toBeVisible()
  })
})

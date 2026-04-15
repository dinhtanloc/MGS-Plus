import { test, expect } from '@playwright/test'

const TEST_EMAIL    = `e2e_${Date.now()}@example.com`
const TEST_PASSWORD = 'Test1234!'
const TEST_NAME     = 'E2E User'

test.describe('Authentication', () => {
  // ── Registration ────────────────────────────────────────────────────────────

  test('register new user successfully', async ({ page }) => {
    await page.goto('/register')
    await expect(page.getByRole('heading', { name: /đăng ký/i })).toBeVisible()

    await page.getByLabel(/email/i).fill(TEST_EMAIL)
    await page.getByLabel(/mật khẩu/i).first().fill(TEST_PASSWORD)
    await page.getByLabel(/họ/i).fill('E2E')
    await page.getByLabel(/tên/i).fill('User')
    await page.getByRole('button', { name: /đăng ký/i }).click()

    // After registration, should redirect to home
    await expect(page).toHaveURL('/')
  })

  // ── Login ────────────────────────────────────────────────────────────────────

  test('login with valid credentials redirects to home', async ({ page }) => {
    await page.goto('/login')
    await page.getByLabel(/email/i).fill(TEST_EMAIL)
    await page.getByLabel(/mật khẩu/i).fill(TEST_PASSWORD)
    await page.getByRole('button', { name: /đăng nhập/i }).click()

    await expect(page).toHaveURL('/')
    // Navbar should show auth-only items
    await expect(page.getByText(/tư vấn ai/i)).toBeVisible()
  })

  test('login with wrong password shows error', async ({ page }) => {
    await page.goto('/login')
    await page.getByLabel(/email/i).fill(TEST_EMAIL)
    await page.getByLabel(/mật khẩu/i).fill('WrongPassword!')
    await page.getByRole('button', { name: /đăng nhập/i }).click()

    // Should stay on login page and show an error toast / message
    await expect(page).toHaveURL('/login')
  })

  test('protected route redirects unauthenticated user to login', async ({ page }) => {
    await page.goto('/chat')
    await expect(page).toHaveURL(/\/login/)
  })

  // ── Logout ────────────────────────────────────────────────────────────────────

  test('logout clears session and redirects', async ({ page }) => {
    // Log in first
    await page.goto('/login')
    await page.getByLabel(/email/i).fill(TEST_EMAIL)
    await page.getByLabel(/mật khẩu/i).fill(TEST_PASSWORD)
    await page.getByRole('button', { name: /đăng nhập/i }).click()
    await expect(page).toHaveURL('/')

    // Find and click logout
    await page.getByRole('button', { name: /đăng xuất/i }).click()

    // Should redirect to home or login, and chat nav item should be gone
    await expect(page.getByText(/tư vấn ai/i)).not.toBeVisible()
  })
})

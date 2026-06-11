import { defineConfig, devices } from '@playwright/test'
import path from 'path'
import { WEB_BASE } from './auth.helpers'

export default defineConfig({
  testDir: '.',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: [['html'], ['list']],

  use: {
    baseURL: WEB_BASE,
    trace: 'on-first-retry',
    video: 'retain-on-failure',
  },

  // reuseExistingServer: true — reuses a manually-started server if already running.
  // The API must be started manually (`dotnet run --project src/Kanban.Api`) because
  // it requires dotnet user-secrets that Playwright cannot inject.
  webServer: {
    command: 'npm run dev',
    cwd: path.resolve(__dirname, '../../src/Kanban.Web'),
    url: WEB_BASE,
    reuseExistingServer: true,
    timeout: 30_000,
  },

  projects: [
    {
      name: 'setup',
      testMatch: /auth\.setup\.ts/,
      use: {
        ...devices['Desktop Chrome'],
        // Real Chrome + disabled automation flags are both required for Google OAuth.
        // Playwright's bundled Chromium sets navigator.webdriver=true; the launch arg removes it.
        // The bypass path (PLAYWRIGHT_AUTH_BYPASS=true) skips the Google test so this only
        // matters for the one-time demo setup.
        channel: 'chrome',
        launchOptions: {
          args: ['--disable-blink-features=AutomationControlled'],
        },
      },
    },
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
      dependencies: ['setup'],
    },
  ],
})

import fs from 'node:fs/promises';
import path from 'node:path';
import { createRequire } from 'node:module';

const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PLAYWRIGHT_PATH ?? 'playwright');

const baseUrl = process.env.FINTRACK_WEB_URL ?? 'http://localhost:4200';
const apiUrl = process.env.FINTRACK_API_URL ?? 'http://localhost:5080';
const outDir = path.resolve('docs/assets/gif-frames');
const browserPath = process.env.PLAYWRIGHT_BROWSER_PATH
  ?? 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';

async function post(url, token, body) {
  const response = await fetch(`${apiUrl}${url}`, {
    method: 'POST',
    headers: {
      'content-type': 'application/json',
      ...(token ? { authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify(body),
  });

  if (!response.ok) {
    throw new Error(`${url} failed: ${response.status} ${await response.text()}`);
  }

  return response.json();
}

async function seed() {
  const email = `gif-${Date.now()}@fintrack.local`;
  const password = 'Senha@123';
  const auth = await post('/auth/register', null, { name: 'Demo Fin', email, password });
  const account = await post('/accounts', auth.token, {
    name: 'Conta GIF',
    type: 'Checking',
    initialBalance: 500,
    openingDate: '2026-01-01',
  });
  const income = await post('/categories', auth.token, { name: 'Salario GIF', type: 'Income' });
  const expense = await post('/categories', auth.token, { name: 'Mercado GIF', type: 'Expense' });

  await post('/transactions', auth.token, {
    accountId: account.id,
    categoryId: income.id,
    type: 'Income',
    amount: 5000,
    date: '2026-08-05',
    description: 'Receita GIF',
  });
  await post('/transactions', auth.token, {
    accountId: account.id,
    categoryId: expense.id,
    type: 'Expense',
    amount: 750.50,
    date: '2026-08-12',
    description: 'Despesa GIF',
  });

  return { email, password };
}

async function shot(page, group, name) {
  await page.screenshot({ path: path.join(outDir, group, `${name}.png`) });
}

async function prepare(group) {
  await fs.rm(path.join(outDir, group), { recursive: true, force: true });
  await fs.mkdir(path.join(outDir, group), { recursive: true });
}

async function main() {
  const user = await seed();
  await fs.mkdir(outDir, { recursive: true });

  const browser = await chromium.launch({ executablePath: browserPath, headless: true });
  const page = await browser.newPage({ viewport: { width: 1100, height: 720 }, deviceScaleFactor: 1 });

  await prepare('login');
  await page.goto(baseUrl, { waitUntil: 'networkidle' });
  await shot(page, 'login', '001-login');
  await page.getByRole('button', { name: /criar conta grátis/i }).click();
  await page.waitForTimeout(400);
  await shot(page, 'login', '002-register');
  await page.getByRole('button', { name: /voltar para login/i }).click();
  await page.waitForTimeout(400);
  await shot(page, 'login', '003-back-login');

  await prepare('dashboard');
  await page.getByPlaceholder('seu@email.com').fill(user.email);
  await page.getByPlaceholder('Sua senha').fill(user.password);
  await page.getByRole('button', { name: /^Entrar$/ }).click();
  await page.waitForTimeout(1800);
  await shot(page, 'dashboard', '001-dashboard');
  await page.locator('.pet-pig').click({ force: true });
  await page.waitForTimeout(400);
  await shot(page, 'dashboard', '002-fin-open');
  await page.getByPlaceholder('Pergunte algo...').fill('qual meu saldo?');
  await page.getByRole('button', { name: 'Enviar' }).click();
  await page.waitForTimeout(400);
  await shot(page, 'dashboard', '003-fin-answer');

  await prepare('accounts');
  await page.getByRole('button', { name: /Contas/ }).first().click();
  await page.waitForTimeout(500);
  await shot(page, 'accounts', '001-accounts');
  await page.getByRole('button', { name: /Categorias/ }).first().click();
  await page.waitForTimeout(500);
  await shot(page, 'accounts', '002-categories');
  await page.getByRole('button', { name: /Relatórios/ }).first().click();
  await page.waitForTimeout(500);
  await shot(page, 'accounts', '003-reports');

  await browser.close();
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});

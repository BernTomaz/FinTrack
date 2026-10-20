import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import http from 'node:http';
import path from 'node:path';
import { createRequire } from 'node:module';

const require = createRequire(new URL('../../src/FinTrack.Web/package.json', import.meta.url));
const { chromium } = require(process.env.PLAYWRIGHT_PATH ?? 'playwright');
const apiUrl = process.env.FINTRACK_API_URL;
assert(apiUrl, 'Informe FINTRACK_API_URL apontando para a API com banco descartável.');
assert(['localhost', '127.0.0.1'].includes(new URL(apiUrl).hostname), 'Use uma API local de validação.');
const root = path.resolve('src/FinTrack.Web/dist/FinTrack.Web/browser');
const output = path.resolve('out/validation/ui');
await fs.mkdir(output, { recursive: true });
const server = http.createServer(async (request, response) => {
  try {
    const file = path.resolve(root, `.${new URL(request.url, 'http://localhost').pathname}`);
    assert(file === root || file.startsWith(root + path.sep));
    const target = file === root ? path.join(root, 'index.html') : file;
    let body = await fs.readFile(target);
    if (target.endsWith('index.html')) body = Buffer.from(body.toString().replace('http://localhost:5080', apiUrl));
    response.setHeader('Content-Type', { '.html': 'text/html', '.js': 'text/javascript', '.css': 'text/css', '.png': 'image/png', '.svg': 'image/svg+xml' }[path.extname(target)] ?? 'application/octet-stream');
    response.end(body);
  } catch {
    response.writeHead(404).end();
  }
});
await new Promise((resolve) => server.listen(4201, '127.0.0.1', resolve));
const browser = await chromium.launch({ executablePath: process.env.PLAYWRIGHT_BROWSER_PATH, headless: true });
const page = await browser.newPage({ viewport: { width: 1440, height: 1000 }, acceptDownloads: true });
const errors = [];
const layouts = [];
page.on('pageerror', (error) => errors.push(error.message));
const id = Date.now();
const email = `ui-${id}@fintrack.local`;
const password = 'Teste@12345';

async function nav(title) {
  await page.locator('.app-shell.sidebar-hidden .compact-nav, .app-shell:not(.sidebar-hidden) .sidebar nav').getByTitle(title, { exact: true }).click();
}
async function save(name, endpoint, expectedStatus) {
  const result = page.waitForResponse((response) => response.url().includes(endpoint) && ['POST', 'PUT'].includes(response.request().method()));
  await page.getByRole('button', { name, exact: true }).click();
  const response = await result;
  assert(expectedStatus ? response.status() === expectedStatus : response.ok(), `${name}: ${response.status()} ${await response.text()}`);
  await page.locator('.loading-strip').waitFor({ state: 'hidden' });
}
async function screenshot(name) {
  await page.screenshot({ path: path.join(output, `${name}.png`), fullPage: true });
  const layout = await page.evaluate(() => ({ width: innerWidth, scrollWidth: document.documentElement.scrollWidth }));
  layouts.push({ name, ...layout });
  assert(layout.scrollWidth <= layout.width + 1, `${name}: página transborda (${layout.scrollWidth}/${layout.width})`);
  assert.match(await page.locator('.app-shell, .login-page').evaluate(element => getComputedStyle(element).fontFamily), /Segoe UI/, `${name}: fonte principal ausente`);
  for (const button of await page.locator('.topbar .top-actions > button').all()) {
    const box = await button.boundingBox();
    assert(box && box.x >= 0 && box.x + box.width <= layout.width + 1, `${name}: botão do cabeçalho cortado`);
  }
  if (await page.locator('.compact-nav').count()) {
    const menu = await page.locator('.compact-nav').boundingBox();
    const metric = await page.locator('.metric, app-reports-panel .panel').first().boundingBox();
    assert(metric && metric.x >= menu.x + menu.width, `${name}: menu compacto cobre o conteúdo`);
  }
}

try {
  await page.goto('http://localhost:4201', { waitUntil: 'networkidle' });
  await screenshot('desktop-login');
  await page.getByRole('button', { name: 'Criar conta grátis', exact: true }).click();
  await page.getByPlaceholder('Seu nome', { exact: true }).fill(`Validação ${id}`);
  await page.getByPlaceholder('seu@email.com').fill(email);
  await page.getByPlaceholder('Crie sua senha').fill(password);
  await save('Criar conta', '/auth/register');
  await page.getByPlaceholder('Sua senha', { exact: true }).fill(password);
  await save('Entrar', '/auth/login');
  await page.locator('.app-shell').waitFor();
  await nav('Contas');
  await page.getByLabel('Nome', { exact: true }).fill('Conta de validação');
  await page.getByLabel('Saldo inicial').fill('500');
  await page.getByLabel('Início da conta').fill('2026-01-01');
  await save('Salvar conta', '/accounts');
  await page.getByText('Conta de validação', { exact: true }).waitFor();
  await nav('Categorias');
  for (const [name, type] of [['Salário teste', 'Income'], ['Mercado teste', 'Expense']]) {
    await page.getByLabel('Nome', { exact: true }).fill(name);
    await page.getByLabel(/^Tipo/).selectOption(type);
    await save('Salvar categoria', '/categories');
    await page.getByText(name, { exact: true }).waitFor();
  }
  for (const [view, category, amount, description] of [['Receitas', 'Salário teste', '1000', 'Receita validada'], ['Despesas', 'Mercado teste', '200', 'Despesa validada']]) {
    await nav(view);
    await page.getByLabel(/^Conta/).selectOption({ label: 'Conta de validação' });
    await page.getByLabel(/^Categoria/).selectOption({ label: category });
    await page.getByLabel('Valor', { exact: true }).fill(amount);
    await page.getByLabel('Data', { exact: true }).fill('2026-10-10');
    await page.getByLabel('Descrição', { exact: true }).fill(description);
    await save('Salvar lançamento', '/transactions');
  }
  await nav('Relatórios');
  await page.getByLabel('Mês', { exact: true }).fill('2026-10');
  await page.getByLabel('Mês', { exact: true }).dispatchEvent('change');
  await page.getByText('Despesa validada', { exact: true }).waitFor();
  const expenseRow = page.locator('.table-row').filter({ hasText: 'Despesa validada' });
  await expenseRow.getByTitle('Editar lançamento', { exact: true }).click();
  await page.getByLabel('Descrição', { exact: true }).fill('Despesa validada editada');
  await save('Atualizar lançamento', '/transactions');
  await nav('Contas');
  await page.getByTitle('Editar conta', { exact: true }).click();
  await page.getByLabel('Início da conta').fill('2026-10-11');
  await save('Atualizar conta', '/accounts', 409);
  assert.match(await page.locator('.message').innerText(), /posterior/);
  await page.getByRole('button', { name: 'Cancelar', exact: true }).click();
  await page.getByTitle('Excluir conta', { exact: true }).click();
  const deletion = page.waitForResponse((response) => response.request().method() === 'DELETE');
  await page.locator('.confirm-card').getByRole('button', { name: 'Excluir', exact: true }).click();
  assert.equal((await deletion).status(), 409);
  await nav('Categorias');
  await page.locator('.account-line').filter({ hasText: 'Mercado teste' }).getByTitle('Editar categoria', { exact: true }).click();
  await page.getByLabel(/^Tipo/).selectOption('Income');
  await save('Atualizar categoria', '/categories', 409);
  assert.match(await page.locator('.message').innerText(), /compatível/);
  await nav('Relatórios');
  await page.getByPlaceholder('Descrição, conta ou categoria').fill('Despesa validada');
  await page.getByText('Página 1 de 1 · 1 lançamentos', { exact: true }).waitFor();
  assert.equal(await page.locator('.table-row:not(.table-head)').count(), 1);
  await page.getByRole('button', { name: 'Limpar filtros', exact: true }).click();
  await nav('Dashboard');
  await page.getByText(/1\.300,00/).first().waitFor();
  assert(await page.locator('.donut-svg path').evaluate((element) => element.getBBox().width > 90), 'Gráfico de uma categoria não foi desenhado');
  await screenshot('desktop-dashboard');
  for (const width of [320, 390, 768, 1024, 1440]) {
    await page.setViewportSize({ width, height: 900 });
    for (const view of ['Dashboard', 'Contas', 'Categorias', 'Receitas', 'Despesas', 'Relatórios', 'Exportação CSV']) {
      await nav(view);
      await screenshot(`${width}-${view.replaceAll(' ', '-')}`);
    }
  }
  await page.getByTitle('Menu do usuário', { exact: true }).click();
  await page.getByRole('button', { name: 'Preferências', exact: true }).click();
  await page.getByLabel(/^Tema/).selectOption('dark');
  await nav('Dashboard');
  await screenshot('desktop-dark');
  await page.setViewportSize({ width: 320, height: 900 });
  await screenshot('mobile-dark');
  await page.getByTitle('Ocultar menu lateral', { exact: true }).click();
  await screenshot('mobile-collapsed');
  await nav('Exportação CSV');
  await page.getByRole('button', { name: 'Baixar CSV', exact: true }).waitFor();
  const downloaded = page.waitForEvent('download');
  await page.getByRole('button', { name: 'Baixar CSV', exact: true }).click();
  const download = await downloaded;
  const csvPath = path.join(output, 'export.csv');
  await download.saveAs(csvPath);
  assert.match(await fs.readFile(csvPath, 'utf8'), /Despesa validada/);
  const token = await page.evaluate(() => sessionStorage.getItem('fintrack.token'));
  const headers = { Authorization: `Bearer ${token}` };
  const accounts = await (await page.request.get(`${apiUrl}/accounts`, { headers })).json();
  const categories = await (await page.request.get(`${apiUrl}/categories`, { headers })).json();
  for (let i = 1; i <= 53; i++) {
    const response = await page.request.post(`${apiUrl}/transactions`, { headers, data: {
      accountId: accounts[0].id, categoryId: categories.find(category => category.name === 'Mercado teste').id,
      type: 'Expense', amount: 1, date: '2026-10-10', description: `Página teste ${String(i).padStart(2, '0')}`,
    } });
    assert.equal(response.status(), 201);
  }
  await nav('Relatórios');
  await page.getByText('Página 1 de 2 · 55 lançamentos', { exact: true }).waitFor();
  assert.equal(await page.locator('.table-row:not(.table-head)').count(), 50);
  await page.getByRole('button', { name: 'Próxima', exact: true }).click();
  await page.getByText('Página 2 de 2 · 55 lançamentos', { exact: true }).waitFor();
  await page.getByText('Receita validada', { exact: true }).waitFor();
  assert.equal(await page.locator('.table-row:not(.table-head)').count(), 5);
  await screenshot('mobile-report-page-2');
  await page.getByPlaceholder('Descrição, conta ou categoria').fill('Página teste 53');
  await page.getByText('Página 1 de 1 · 1 lançamentos', { exact: true }).waitFor();
  await page.getByText('Página teste 53', { exact: true }).waitFor();
  await page.getByRole('button', { name: 'Limpar filtros', exact: true }).click();
  await page.getByText('Página 1 de 2 · 55 lançamentos', { exact: true }).waitFor();
  await nav('Dashboard');
  await page.locator('.topbar').getByRole('button', { name: 'Atualizar', exact: true }).click();
  await page.getByText(/1\.247,00/).first().waitFor();
  assert.match(await page.locator('.bar-group').getAttribute('title'), /253,00/);
  await nav('Exportação CSV');
  const fullDownload = page.waitForEvent('download');
  await page.getByRole('button', { name: 'Baixar CSV', exact: true }).click();
  await (await fullDownload).saveAs(csvPath);
  assert.equal((await fs.readFile(csvPath, 'utf8')).trim().split(/\r?\n/).length, 56);
  await page.getByTitle('Menu do usuário', { exact: true }).click();
  await page.getByRole('button', { name: 'Sair', exact: true }).click();
  await page.setViewportSize({ width: 320, height: 900 });
  await screenshot('mobile-login');
  await page.getByPlaceholder('seu@email.com').fill(email);
  await page.getByPlaceholder('Sua senha', { exact: true }).fill(password);
  await save('Entrar', '/auth/login');
  await page.locator('.app-shell').waitFor();
  assert.deepEqual(errors, []);
  await fs.writeFile(path.join(output, 'result.json'), JSON.stringify({ passed: true, layouts, errors }, null, 2));
  await Promise.all(['failure.png', 'failure.txt'].map(file => fs.rm(path.join(output, file), { force: true })));
  console.log(JSON.stringify({ passed: true, screenshots: layouts.length, errors }));
} catch (error) {
  await page.screenshot({ path: path.join(output, 'failure.png'), fullPage: true });
  await fs.writeFile(path.join(output, 'failure.txt'), `${error}\n${errors.join('\n')}\n${await page.locator('body').innerText()}`);
  throw error;
} finally {
  await browser.close();
  server.close();
}

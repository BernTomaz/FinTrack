import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import { execFileSync } from 'node:child_process';

const database = process.env.FINTRACK_VALIDATION_DATABASE;
const api = process.env.FINTRACK_API_URL;
assert(/^FinTrackValidation_[a-f0-9]{32}$/.test(database ?? ''), 'Use somente um banco descartável FinTrackValidation_<GUID>.');
assert(api, 'Informe FINTRACK_API_URL da API ligada ao banco descartável.');
assert(['localhost', '127.0.0.1'].includes(new URL(api).hostname), 'Use uma API local de validação.');
const sqlcmd = process.env.FINTRACK_SQLCMD ?? 'C:/Program Files/Microsoft SQL Server/Client SDK/ODBC/170/Tools/Binn/SQLCMD.EXE';
function sql(query, db = database) {
  return execFileSync(sqlcmd, ['-S', 'localhost', '-E', '-C', '-b', '-h', '-1', '-y', '4000', '-w', '65535', '-d', db, '-Q', `SET NOCOUNT ON; ${query}`], { encoding: 'utf8', timeout: 60000 }).trim();
}
async function post(url, token, body) {
  const response = await fetch(`${api}${url}`, { method: 'POST', headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) }, body: JSON.stringify(body) });
  assert(response.ok, `${url}: ${response.status} ${await response.clone().text()}`);
  return response.json();
}
const id = Date.now();
const reuse = process.env.FINTRACK_BENCHMARK_REUSE === 'true';
const auth = reuse
  ? await post('/auth/login', null, { email: sql("SELECT TOP (1) Email FROM Users u WHERE Email LIKE 'volume-%@fintrack.local' AND EXISTS (SELECT 1 FROM Transactions t WHERE t.UserId = u.Id) ORDER BY CreatedAt DESC"), password: 'Teste@12345' })
  : await post('/auth/register', null, { name: `Volume ${id}`, email: `volume-${id}@fintrack.local`, password: 'Teste@12345' });
const guidPattern = /^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i;
assert(guidPattern.test(auth.userId), 'Identificador de usuário inválido.');
assert.equal(sql(`SELECT COUNT(*) FROM Users WHERE Id = '${auth.userId}'`), '1', 'A API não está ligada ao banco informado.');
if (!reuse) {
  const account = await post('/accounts', auth.token, { name: 'Conta de volume', type: 'Checking', initialBalance: 500, openingDate: '2026-01-01' });
  const income = await post('/categories', auth.token, { name: 'Receita volume', type: 'Income' });
  const expense = await post('/categories', auth.token, { name: 'Despesa volume', type: 'Expense' });
  assert([account.id, income.id, expense.id].every((value) => guidPattern.test(value)), 'Identificador financeiro inválido.');
  sql(`WITH Numbers AS (
    SELECT TOP (120000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS n FROM sys.all_objects a CROSS JOIN sys.all_objects b
  )
  INSERT INTO Transactions (Id, UserId, AccountId, CategoryId, Type, Amount, Date, Description, CreatedAt)
  SELECT NEWID(), '${auth.userId}', '${account.id}',
    CASE WHEN n % 4 = 0 THEN '${income.id}' ELSE '${expense.id}' END,
    CASE WHEN n % 4 = 0 THEN 'Income' ELSE 'Expense' END,
    CASE WHEN n % 4 = 0 THEN 20.02 ELSE 10.01 END,
    DATEFROMPARTS(2026, 1 + n / 10000, 1 + n % 28), N'Validação de volume', SYSUTCDATETIME()
  FROM Numbers;`);
}
assert.equal(sql(`SELECT COUNT(*) FROM Transactions WHERE UserId = '${auth.userId}'`), '120000');
const measurements = [];
for (const endpoint of ['/dashboard/monthly?year=2026&month=12', '/transactions?year=2026&month=12', '/exports/transactions.csv?year=2026&month=12']) {
  const durations = [];
  let bytes = 0;
  for (let i = 0; i < 8; i++) {
    const start = performance.now();
    const response = await fetch(`${api}${endpoint}`, { headers: { Authorization: `Bearer ${auth.token}` } });
    assert(response.ok, `${endpoint}: ${response.status}`);
    const body = await response.text();
    if (i > 0) durations.push(performance.now() - start);
    bytes = Buffer.byteLength(body);
    if (endpoint.startsWith('/dashboard')) {
      const dashboard = JSON.parse(body);
      assert.equal(dashboard.totalIncome, 50050);
      assert.equal(dashboard.totalExpense, 75075);
      assert.equal(dashboard.currentBalance, -299800);
    } else if (endpoint.startsWith('/transactions')) assert.equal(JSON.parse(body).length, 10000);
    else assert.equal(body.trim().split(/\r?\n/).length, 10001);
  }
  durations.sort((a, b) => a - b);
  measurements.push({ endpoint, samples: durations.length, medianMs: Math.round(durations[3]), maxMs: Math.round(durations.at(-1)), bytes });
}

const restored = `${database}_Restore`;
console.log(JSON.stringify({ rows: 120000, measurements }));
await fs.mkdir('out/validation', { recursive: true });
await fs.writeFile('out/validation/performance-result.json', JSON.stringify({ rows: 120000, monthRows: 10000, measurements }, null, 2));
if (reuse) process.exit(0);
const backupDirectory = sql("SELECT CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultBackupPath'))", 'master');
assert(backupDirectory && backupDirectory !== 'NULL');
const backupPath = `${backupDirectory.replace(/[\\/]$/, '')}\\${database}_${id}.bak`;
const literal = (value) => value.replaceAll("'", "''");
const files = JSON.parse(sql(`SELECT name, physical_name, type FROM sys.master_files WHERE database_id = DB_ID('${database}') FOR JSON PATH`, 'master'));
assert.equal(files.length, 2);
const moves = files.map((file) => `MOVE N'${literal(file.name)}' TO N'${literal(file.physical_name.replace(database, restored))}'`).join(', ');
assert(files.every((file) => file.physical_name.includes(database)), 'Arquivos físicos inesperados.');
try {
  sql(`BACKUP DATABASE [${database}] TO DISK = N'${literal(backupPath)}' WITH COPY_ONLY, CHECKSUM;
       RESTORE VERIFYONLY FROM DISK = N'${literal(backupPath)}' WITH CHECKSUM;
       RESTORE DATABASE [${restored}] FROM DISK = N'${literal(backupPath)}' WITH ${moves}, CHECKSUM;`, 'master');
  for (const table of ['Users', 'Accounts', 'Categories', 'Transactions']) {
    const query = `SELECT COUNT_BIG(*) AS rows, CHECKSUM_AGG(BINARY_CHECKSUM(*)) AS checksum FROM ${table} FOR JSON PATH`;
    assert.equal(sql(query, restored), sql(query), `${table}: dados restaurados diferentes`);
  }
} finally {
  sql(`IF DB_ID('${restored}') IS NOT NULL BEGIN ALTER DATABASE [${restored}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [${restored}]; END`, 'master');
}
await fs.mkdir('out/validation', { recursive: true });
await fs.writeFile('out/validation/database-result.json', JSON.stringify({ rows: 120000, monthRows: 10000, measurements, backupRestoredAndCompared: true, backupPath }, null, 2));
console.log(JSON.stringify({ rows: 120000, measurements, backupRestoredAndCompared: true }));

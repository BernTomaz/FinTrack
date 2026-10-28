import { vi } from 'vitest';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { App } from './app';
import { FinTrackApiService } from './fintrack-api.service';
import { ReportsPanelComponent } from './reports/reports-panel.component';

describe('App', () => {
  let http: HttpTestingController;
  const account = { id: 'account', name: 'Conta', type: 'Checking', initialBalance: 50, openingDate: '2026-01-01' };
  const category = { id: 'category', name: 'Mercado', type: 'Expense' };
  const transaction = { id: 'transaction', accountId: 'account', categoryId: 'category', type: 'Expense', amount: 10, date: '2026-08-02', description: null };
  const dashboard = { totalIncome: 0, totalExpense: 10, monthBalance: -10, currentBalance: 40, expensesByCategory: [{ categoryName: 'Mercado', total: 10 }] };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    TestBed.inject(FinTrackApiService).clearSession();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => { http.verify(); vi.useRealTimers(); });

  function flushPeriod(month: string): void {
    const [year, number] = month.split('-').map(Number);
    http.expectOne((request) => request.url.includes(`/transactions?year=${year}&month=${number}&`)).flush({ items: [transaction], totalCount: 1, page: 1, pageSize: 5 });
    http.expectOne((request) => request.url.endsWith(`/dashboard/monthly?year=${year}&month=${number}`)).flush(dashboard);
  }

  function loadedApp(): App {
    const app = TestBed.createComponent(App).componentInstance;
    app['token'].set('test-token');
    app['loadAll']();
    http.expectOne((request) => request.url.endsWith('/accounts')).flush([account]);
    http.expectOne((request) => request.url.endsWith('/categories')).flush([category]);
    flushPeriod(app['selectedMonth']());
    return app;
  }

  it.each([
    ['dashboard', 'Income'], ['dashboard', 'Expense'],
    ['reports', 'Income'], ['reports', 'Expense'],
  ] as const)('duplicates a %s %s only after review and saves with POST', async (view, type) => {
    const fixture = TestBed.createComponent(App);
    fixture.autoDetectChanges();
    const app = fixture.componentInstance;
    const original = Object.freeze({ ...transaction, type, description: type === 'Income' ? null : 'Original' });
    app['token'].set('test-token');
    app['loadAll']();
    http.expectOne(request => request.url.endsWith('/accounts')).flush([account]);
    http.expectOne(request => request.url.endsWith('/categories')).flush([{ ...category, type }]);
    flushPeriod(app['selectedMonth']());
    app['transactions'].set([original]);
    app['reportTransactions'].set([original]);
    app['activeView'].set(view);
    app['editingTransactionId'].set('previous-edit');
    await fixture.whenStable();

    const duplicate = fixture.nativeElement.querySelector('button[aria-label="Duplicar lançamento"]') as HTMLButtonElement;
    expect(duplicate).not.toBeNull();
    duplicate.click();
    await fixture.whenStable();
    expect(app['editingTransactionId']()).toBe('');
    expect(app['activeView']()).toBe(type === 'Income' ? 'income' : 'expense');
    expect(app['transactionForm'].getRawValue()).toEqual({
      accountId: original.accountId, categoryId: original.categoryId, type,
      amount: original.amount, date: original.date, description: original.description ?? '',
    });
    http.expectNone(request => ['POST', 'PUT'].includes(request.method));

    app['transactionForm'].patchValue({ amount: 22.5, date: '2026-08-03', description: 'Cópia revisada' });
    const form = fixture.nativeElement.querySelector('app-transaction-form-panel form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    const creation = http.expectOne(request => request.url.endsWith('/transactions') && request.method === 'POST');
    expect(creation.request.body).toEqual({
      accountId: original.accountId, categoryId: original.categoryId, type,
      amount: 22.5, date: '2026-08-03', description: 'Cópia revisada',
    });
    http.expectNone(request => request.method === 'PUT');
    creation.flush({ ...original, ...creation.request.body, id: 'copy' });
    flushPeriod(app['selectedMonth']());
    expect(original.amount).toBe(10);
    expect(original.date).toBe('2026-08-02');
    expect(original.description).toBe(type === 'Income' ? null : 'Original');
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render login title', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Controle suas finanças');
  });

  it('moves the loading bar horizontally, including when reduced motion is enabled', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance['isEntering'].set(true);
    fixture.detectChanges();
    const bar = fixture.nativeElement.querySelector('.loading-bar i') as HTMLElement;
    const animation = bar.getAnimations()[0];
    expect(animation).toBeDefined();
    const frames = (animation.effect as KeyframeEffect).getKeyframes();
    expect(frames.some(frame => String(frame['transform']).includes('125%'))).toBe(true);
    animation.pause();
    animation.currentTime = 0;
    const start = bar.getBoundingClientRect().left;
    animation.currentTime = Number(animation.effect!.getTiming().duration) / 2;
    expect(bar.getBoundingClientRect().left).toBeGreaterThan(start);
  });

  it('opens deletion as a native modal and cancels without deleting', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.autoDetectChanges();
    const app = fixture.componentInstance;
    app['token'].set('test-token');
    app['deleteTransaction']('transaction');
    await fixture.whenStable();
    const dialog = fixture.nativeElement.querySelector('dialog') as HTMLDialogElement;
    expect(dialog.open).toBe(true);
    expect(dialog.matches(':modal')).toBe(true);
    expect(dialog.contains(document.activeElement)).toBe(true);
    dialog.dispatchEvent(new Event('cancel'));
    await fixture.whenStable();
    expect(app['deleteTarget']()).toBeNull();
    expect(fixture.nativeElement.querySelector('dialog')).toBeNull();
    http.expectNone(request => request.method === 'DELETE');
  });

  it('offers mobile sorting through the existing report sort event', async () => {
    const fixture = TestBed.createComponent(ReportsPanelComponent);
    fixture.autoDetectChanges();
    const sort = vi.fn();
    fixture.componentInstance.sort.subscribe(sort);
    await fixture.whenStable();
    const select = fixture.nativeElement.querySelector('.mobile-sort select') as HTMLSelectElement;
    select.value = 'amount';
    select.dispatchEvent(new Event('change'));
    expect(sort).toHaveBeenLastCalledWith('amount');
    fixture.componentRef.setInput('sortField', 'amount');
    fixture.componentRef.setInput('sortDirection', 'desc');
    await fixture.whenStable();
    const direction = fixture.nativeElement.querySelector('.mobile-sort button') as HTMLButtonElement;
    expect(direction.textContent).toContain('decrescente');
    direction.click();
    expect(sort).toHaveBeenLastCalledWith('amount');
  });

  it('loads balances and catalogs, then reuses catalogs when changing month', () => {
    const app = loadedApp();
    expect(app['dashboard']()?.currentBalance).toBe(40);
    app['setCurrentMonth']('2026-08');
    http.expectNone((request) => request.url.endsWith('/accounts') || request.url.endsWith('/categories'));
    flushPeriod('2026-08');
    expect(app['accounts']()[0].name).toBe('Conta');
    expect(app['categories']()[0].name).toBe('Mercado');
    expect(app['isLoading']()).toBe(false);
  });

  it('reloads catalogs on explicit refresh', () => {
    const app = loadedApp();
    app['refreshData']();
    http.expectOne((request) => request.url.endsWith('/accounts')).flush([{ ...account, name: 'Reserva' }]);
    http.expectOne((request) => request.url.endsWith('/categories')).flush([category]);
    flushPeriod(app['selectedMonth']());
    expect(app['accounts']()[0].name).toBe('Reserva');
  });

  it('refreshes transactions and balance after deletion without reloading catalogs', () => {
    const app = loadedApp();
    app['deleteTransaction']('transaction');
    app['confirmDelete']();
    const deletion = http.expectOne((request) => request.url.endsWith('/transactions/transaction'));
    expect(deletion.request.method).toBe('DELETE');
    deletion.flush(null);
    http.expectNone((request) => request.url.endsWith('/accounts') || request.url.endsWith('/categories'));
    http.expectOne((request) => request.url.includes('/transactions?')).flush({ items: [], totalCount: 0, page: 1, pageSize: 5 });
    http.expectOne((request) => request.url.includes('/dashboard/monthly?')).flush({ ...dashboard, totalExpense: 0, monthBalance: 0, currentBalance: 50 });
    expect(app['transactions']()).toEqual([]);
    expect(app['dashboard']()?.currentBalance).toBe(50);
  });

  it('refreshes catalogs after editing an account', () => {
    const app = loadedApp();
    app['editingAccountId'].set('account');
    app['accountForm'].setValue({ name: 'Reserva', type: 'Savings', initialBalance: 50, openingDate: '2026-01-01' });
    app['saveAccount']();
    const update = http.expectOne((request) => request.url.endsWith('/accounts/account'));
    expect(update.request.method).toBe('PUT');
    update.flush({ ...account, name: 'Reserva' });
    http.expectOne((request) => request.url.endsWith('/accounts')).flush([{ ...account, name: 'Reserva' }]);
    http.expectOne((request) => request.url.endsWith('/categories')).flush([category]);
    flushPeriod(app['selectedMonth']());
    expect(app['accounts']()[0].name).toBe('Reserva');
  });

  it('keeps loaded data after a failed period request', () => {
    const app = loadedApp();
    app['setCurrentMonth']('2026-08');
    const transactions = http.expectOne((request) => request.url.includes('/transactions?'));
    const monthly = http.expectOne((request) => request.url.includes('/dashboard/monthly?'));
    transactions.flush('Unavailable', { status: 503, statusText: 'Unavailable' });
    expect(monthly.cancelled).toBe(true);
    expect(app['dashboard']()?.currentBalance).toBe(40);
    expect(app['isLoading']()).toBe(false);
    expect(app['message']()).toContain('Unavailable');
  });

  it('cancels an older month request when the selected month changes again', () => {
    const app = loadedApp();
    app['setCurrentMonth']('2026-08');
    const oldTransactions = http.expectOne((request) => request.url.includes('/transactions?'));
    const oldDashboard = http.expectOne((request) => request.url.includes('/dashboard/monthly?'));
    app['setCurrentMonth']('2026-09');
    expect(oldTransactions.cancelled).toBe(true);
    expect(oldDashboard.cancelled).toBe(true);
    flushPeriod('2026-09');
    expect(app['selectedMonth']()).toBe('2026-09');
  });

  it('still loads catalogs when switching month during the initial load', () => {
    const app = TestBed.createComponent(App).componentInstance;
    app['token'].set('test-token');
    app['loadAll']();
    const old = http.match(() => true);
    expect(old.length).toBe(4);
    app['setCurrentMonth']('2026-08');
    expect(old.every((request) => request.cancelled)).toBe(true);
    http.expectOne((request) => request.url.endsWith('/accounts')).flush([account]);
    http.expectOne((request) => request.url.endsWith('/categories')).flush([category]);
    flushPeriod('2026-08');
    expect(app['accounts']().length).toBe(1);
  });

  it('cancels loading and clears financial data on logout', () => {
    const app = loadedApp();
    app['setCurrentMonth']('2026-08');
    const pending = http.match(() => true);
    app['logout']();
    expect(pending.every((request) => request.cancelled)).toBe(true);
    expect(app['accounts']()).toEqual([]);
    expect(app['categories']()).toEqual([]);
    expect(app['transactions']()).toEqual([]);
    expect(app['dashboard']()).toBeNull();
    expect(app['isLoggedIn']()).toBe(false);
  });

  it('loads report pages on the server and resets pagination when filtering or sorting', () => {
    vi.useFakeTimers();
    const app = loadedApp();
    app['openView']('reports');
    const first = http.expectOne(request => request.url.includes('page=1&pageSize=50'));
    first.flush({ items: [transaction], totalCount: 51, page: 1, pageSize: 50 });
    app['changeReportPage'](2);
    http.expectOne(request => request.url.includes('page=2&pageSize=50')).flush({ items: [{ ...transaction, id: 'last' }], totalCount: 51, page: 2, pageSize: 50 });
    expect(app['reportTransactions']()[0].id).toBe('last');
    app['updateTransactionSearch']('Mercado');
    vi.advanceTimersByTime(250);
    const filtered = http.expectOne(request => request.url.includes('page=1&pageSize=50') && request.url.includes('search=Mercado'));
    filtered.flush({ items: [transaction], totalCount: 1, page: 1, pageSize: 50 });
    expect(app['reportPage']()).toBe(1);
    expect(app['reportPageCount']()).toBe(1);
    app['sortTransactions']('amount');
    http.expectOne(request => request.url.includes('sort=amount&direction=desc')).flush({ items: [transaction], totalCount: 1, page: 1, pageSize: 50 });
    expect(app['chartMonths']()[0].expenseAmount).toBe(10);
  });

  it('cancels stale report requests and pending searches on logout', () => {
    vi.useFakeTimers();
    const app = loadedApp();
    app['openView']('reports');
    const old = http.expectOne(request => request.url.includes('pageSize=50'));
    app['updateTransactionTypeFilter']('Expense');
    expect(old.cancelled).toBe(true);
    const filtered = http.expectOne(request => request.url.includes('type=Expense'));
    app['updateTransactionSearch']('Mercado');
    expect(filtered.cancelled).toBe(true);
    app['logout']();
    vi.advanceTimersByTime(250);
    http.expectNone(request => request.url.includes('/transactions?'));
    expect(app['reportTransactions']()).toEqual([]);
    expect(app['reportLoading']()).toBe(false);
  });
});

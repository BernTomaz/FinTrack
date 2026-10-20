import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { App } from './app';
import { FinTrackApiService } from './fintrack-api.service';

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

  afterEach(() => http.verify());

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

  it('loads balances and catalogs, then reuses catalogs when changing month', () => {
    const app = loadedApp();
    expect(app['dashboard']()?.currentBalance).toBe(40);
    app['setCurrentMonth']('2026-08');
    http.expectNone((request) => request.url.endsWith('/accounts') || request.url.endsWith('/categories'));
    flushPeriod('2026-08');
    expect(app['accounts']()[0].name).toBe('Conta');
    expect(app['categories']()[0].name).toBe('Mercado');
    expect(app['isLoading']()).toBeFalse();
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
    expect(monthly.cancelled).toBeTrue();
    expect(app['dashboard']()?.currentBalance).toBe(40);
    expect(app['isLoading']()).toBeFalse();
    expect(app['message']()).toContain('Unavailable');
  });

  it('cancels an older month request when the selected month changes again', () => {
    const app = loadedApp();
    app['setCurrentMonth']('2026-08');
    const oldTransactions = http.expectOne((request) => request.url.includes('/transactions?'));
    const oldDashboard = http.expectOne((request) => request.url.includes('/dashboard/monthly?'));
    app['setCurrentMonth']('2026-09');
    expect(oldTransactions.cancelled).toBeTrue();
    expect(oldDashboard.cancelled).toBeTrue();
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
    expect(old.every((request) => request.cancelled)).toBeTrue();
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
    expect(pending.every((request) => request.cancelled)).toBeTrue();
    expect(app['accounts']()).toEqual([]);
    expect(app['categories']()).toEqual([]);
    expect(app['transactions']()).toEqual([]);
    expect(app['dashboard']()).toBeNull();
    expect(app['isLoggedIn']()).toBeFalse();
  });

  it('loads report pages on the server and resets pagination when filtering or sorting', fakeAsync(() => {
    const app = loadedApp();
    app['openView']('reports');
    const first = http.expectOne(request => request.url.includes('page=1&pageSize=50'));
    first.flush({ items: [transaction], totalCount: 51, page: 1, pageSize: 50 });
    app['changeReportPage'](2);
    http.expectOne(request => request.url.includes('page=2&pageSize=50')).flush({ items: [{ ...transaction, id: 'last' }], totalCount: 51, page: 2, pageSize: 50 });
    expect(app['reportTransactions']()[0].id).toBe('last');
    app['updateTransactionSearch']('Mercado');
    tick(250);
    const filtered = http.expectOne(request => request.url.includes('page=1&pageSize=50') && request.url.includes('search=Mercado'));
    filtered.flush({ items: [transaction], totalCount: 1, page: 1, pageSize: 50 });
    expect(app['reportPage']()).toBe(1);
    expect(app['reportPageCount']()).toBe(1);
    app['sortTransactions']('amount');
    http.expectOne(request => request.url.includes('sort=amount&direction=desc')).flush({ items: [transaction], totalCount: 1, page: 1, pageSize: 50 });
    expect(app['chartMonths']()[0].expenseAmount).toBe(10);
  }));

  it('cancels stale report requests and pending searches on logout', fakeAsync(() => {
    const app = loadedApp();
    app['openView']('reports');
    const old = http.expectOne(request => request.url.includes('pageSize=50'));
    app['updateTransactionTypeFilter']('Expense');
    expect(old.cancelled).toBeTrue();
    const filtered = http.expectOne(request => request.url.includes('type=Expense'));
    app['updateTransactionSearch']('Mercado');
    expect(filtered.cancelled).toBeTrue();
    app['logout']();
    tick(250);
    http.expectNone(request => request.url.includes('/transactions?'));
    expect(app['reportTransactions']()).toEqual([]);
    expect(app['reportLoading']()).toBeFalse();
  }));
});

import { CommonModule } from '@angular/common';
import { Component, ElementRef, OnDestroy, ViewEncapsulation, afterRenderEffect, computed, inject, signal, viewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Subscription, finalize, forkJoin, of } from 'rxjs';
import { Account, AccountType, AuthResponse, Category, CategoryType, Dashboard, FinTrackApiService, HealthStatus, LockedUser, Transaction, TransactionType } from './fintrack-api.service';
import { AuthComponent } from './auth/auth.component';
import { AccountsPanelComponent } from './accounts/accounts-panel.component';
import { CategoriesPanelComponent } from './categories/categories-panel.component';
import { DashboardPanelComponent } from './dashboard/dashboard-panel.component';
import { ExportPanelComponent } from './reports/export-panel.component';
import { PasswordPanelComponent } from './settings/password-panel.component';
import { ProfilePanelComponent } from './settings/profile-panel.component';
import { ReportsPanelComponent } from './reports/reports-panel.component';
import { TransactionFormPanelComponent } from './transactions/transaction-form-panel.component';
import { formatMoney } from './shared/format-money';
import { isStrongPassword } from './shared/password-rules';

type Theme = 'light' | 'dark';
type PetColor = 'green' | 'blue' | 'orange' | 'purple';
type DeleteTarget = { type: 'account' | 'category' | 'transaction'; id: string; title: string; description: string };
type TransactionSortField = 'date' | 'description' | 'category' | 'type' | 'amount';
type View =
  | 'dashboard'
  | 'income'
  | 'expense'
  | 'accounts'
  | 'categories'
  | 'reports'
  | 'categorySummary'
  | 'export'
  | 'profile'
  | 'password'
  | 'admin'
  | 'status'
  | 'preferences'
  | 'about';

@Component({
  selector: 'app-root',
  imports: [
    AuthComponent,
    CommonModule,
    ReactiveFormsModule,
    AccountsPanelComponent,
    CategoriesPanelComponent,
    DashboardPanelComponent,
    ExportPanelComponent,
    PasswordPanelComponent,
    ProfilePanelComponent,
    ReportsPanelComponent,
    TransactionFormPanelComponent,
  ],
  templateUrl: './app.html',
  styleUrls: ['./app.css', './app-responsive.css'],
  encapsulation: ViewEncapsulation.None,
})
export class App implements OnDestroy {
  protected readonly isStrongPassword = isStrongPassword;

  private readonly api = inject(FinTrackApiService);
  private readonly fb = inject(FormBuilder);

  private messageTimeoutId: ReturnType<typeof setTimeout> | null = null;
  private messageClearTimeoutId: ReturnType<typeof setTimeout> | null = null;
  private loadSubscription?: Subscription;
  private catalogsLoaded = false;
  private reportSubscription?: Subscription;
  private searchTimeout?: ReturnType<typeof setTimeout>;
  private readonly deleteDialog = viewChild<ElementRef<HTMLDialogElement>>('deleteDialog');

  protected readonly token = signal(sessionStorage.getItem('fintrack.token') ?? '');
  protected readonly isEntering = signal((sessionStorage.getItem('fintrack.token') ?? '').length > 0);
  protected readonly userName = signal(sessionStorage.getItem('fintrack.name') ?? '');
  protected readonly userEmail = signal(sessionStorage.getItem('fintrack.email') ?? '');
  protected readonly isAdmin = signal(sessionStorage.getItem('fintrack.admin') === 'true');
  protected readonly message = signal('');
  protected readonly messageKind = signal<'success' | 'error' | 'info'>('info');
  protected readonly isMessageLeaving = signal(false);
  protected readonly isLoading = signal(false);
  protected readonly deleteTarget = signal<DeleteTarget | null>(null);
  protected readonly accounts = signal<Account[]>([]);
  protected readonly categories = signal<Category[]>([]);
  protected readonly transactions = signal<Transaction[]>([]);
  protected readonly lockedUsers = signal<LockedUser[]>([]);
  protected readonly dashboard = signal<Dashboard | null>(null);
  protected readonly healthStatus = signal<HealthStatus | null>(null);
  protected readonly healthCorrelationId = signal('');
  protected readonly healthCheckedAt = signal('');
  protected readonly userMenuOpen = signal(false);
  protected readonly sidebarHidden = signal(false);
  protected readonly petOpen = signal(false);
  protected readonly petEnabled = signal(localStorage.getItem('fintrack.pet.enabled') !== 'false');
  protected readonly petColor = signal<PetColor>((localStorage.getItem('fintrack.pet.color') as PetColor | null) ?? 'green');
  protected readonly customCursorEnabled = signal(localStorage.getItem('fintrack.cursor.custom') !== 'false');
  protected readonly petQuestion = signal('');
  protected readonly petAnswer = signal('Oi, sou o Fin. Pergunte sobre saldo, receitas, despesas ou categorias.');
  protected readonly transactionSearch = signal('');
  protected readonly transactionTypeFilter = signal('');
  protected readonly transactionCategoryFilter = signal('');
  protected readonly transactionAccountFilter = signal('');
  protected readonly transactionSortField = signal<TransactionSortField>('date');
  protected readonly transactionSortDirection = signal<'asc' | 'desc'>('desc');
  protected readonly transactionCount = signal(0);
  protected readonly reportTransactions = signal<Transaction[]>([]);
  protected readonly reportTotal = signal(0);
  protected readonly reportPage = signal(1);
  protected readonly reportLoading = signal(false);
  protected readonly reportPageCount = computed(() => Math.max(1, Math.ceil(this.reportTotal() / 50)));
  protected readonly exportStartDate = signal('');
  protected readonly exportEndDate = signal('');
  protected readonly editingAccountId = signal('');
  protected readonly editingCategoryId = signal('');
  protected readonly editingTransactionId = signal('');
  protected readonly activeView = signal<View>('dashboard');
  protected readonly theme = signal<Theme>('light');
  protected readonly selectedMonth = signal(this.currentMonthKey());

  protected readonly isLoggedIn = computed(() => this.token().length > 0);
  protected readonly selectedYear = computed(() => Number(this.selectedMonth().slice(0, 4)));
  protected readonly selectedMonthNumber = computed(() => Number(this.selectedMonth().slice(5, 7)));
  protected readonly hasMoreTransactions = computed(() => this.transactionCount() > this.transactions().length);
  protected readonly categorySummary = computed(() => {
    const total = this.dashboard()?.totalExpense ?? 0;

    return (this.dashboard()?.expensesByCategory ?? []).map((item) => ({
      ...item,
      percent: total > 0 ? (item.total / total) * 100 : 0,
    }));
  });
  protected readonly petTip = computed(() => {
    const dashboard = this.dashboard();

    if (!dashboard) {
      return 'Carregando seus dados...';
    }

    return dashboard.monthBalance >= 0 ? 'Seu mês está positivo.' : 'Seu mês está no vermelho.';
  });
  protected readonly accountTypes: AccountType[] = ['Wallet', 'Checking', 'Savings', 'CreditCard'];
  protected readonly categoryTypes: CategoryType[] = ['Income', 'Expense'];
  protected readonly transactionTypes: TransactionType[] = ['Income', 'Expense'];
  protected readonly chartMonths = computed(() => {
    const dashboard = this.dashboard();
    const rows = dashboard && this.transactionCount() > 0 ? [{
      label: this.shortMonthLabel(new Date(this.selectedYear(), this.selectedMonthNumber() - 1, 1)),
      incomeAmount: dashboard.totalIncome,
      expenseAmount: dashboard.totalExpense,
    }] : [];
    const maxAmount = Math.max(...rows.flatMap((month) => [month.incomeAmount, month.expenseAmount]), 0);

    return rows.map((month) => ({
      label: month.label,
      income: maxAmount > 0 ? Math.max((month.incomeAmount / maxAmount) * 100, month.incomeAmount > 0 ? 12 : 0) : 0,
      expense: maxAmount > 0 ? Math.max((month.expenseAmount / maxAmount) * 100, month.expenseAmount > 0 ? 12 : 0) : 0,
      incomeAmount: month.incomeAmount,
      expenseAmount: month.expenseAmount,
    }));
  });

  protected readonly passwordForm = this.fb.nonNullable.group({
    currentPassword: ['', [Validators.required, Validators.maxLength(100)]],
    newPassword: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
    confirmPassword: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
  });

  protected readonly accountForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(80)]],
    type: ['Checking' as AccountType, Validators.required],
    initialBalance: [0, Validators.required],
    openingDate: [this.today(), Validators.required],
  });

  protected readonly categoryForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(80)]],
    type: ['Expense' as CategoryType, Validators.required],
  });

  protected readonly transactionForm = this.fb.nonNullable.group({
    accountId: ['', Validators.required],
    categoryId: ['', Validators.required],
    type: ['Expense' as TransactionType, Validators.required],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    date: [this.today(), Validators.required],
    description: ['', Validators.maxLength(160)],
  });

  protected readonly profileForm = this.fb.nonNullable.group({
    name: [this.userName(), [Validators.required, Validators.minLength(2), Validators.maxLength(80)]],
  });

  protected readonly adminResetForm = this.fb.nonNullable.group({
    newPassword: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
  });

  constructor() {
    afterRenderEffect(() => {
      const dialog = this.deleteDialog()?.nativeElement;
      if (dialog && !dialog.open) dialog.showModal();
    });
    if (this.isLoggedIn()) {
      this.theme.set(this.savedThemeFor(this.userEmail()));
      this.loadAll();
      setTimeout(() => this.isEntering.set(false), 1400);
    }
  }

  protected logout(): void {
    this.loadSubscription?.unsubscribe();
    this.catalogsLoaded = false;
    this.accounts.set([]);
    this.categories.set([]);
    this.transactions.set([]);
    this.transactionCount.set(0);
    this.cancelReportLoad();
    this.reportTransactions.set([]);
    this.reportTotal.set(0);
    this.reportPage.set(1);
    this.dashboard.set(null);
    this.api.clearSession();
    this.token.set('');
    this.userName.set('');
    this.userEmail.set('');
    this.isAdmin.set(false);
    this.message.set('');
    this.isMessageLeaving.set(false);
    this.userMenuOpen.set(false);
  }

  protected saveAccount(): void {
    const validation = this.accountValidationMessage();
    if (validation) {
      this.accountForm.markAllAsTouched();
      this.showMessage(validation);
      return;
    }

    const id = this.editingAccountId();
    const request = id
      ? this.api.updateAccount(this.token(), id, this.accountForm.getRawValue())
      : this.api.createAccount(this.token(), this.accountForm.getRawValue());

    request.subscribe({
      next: () => {
        this.editingAccountId.set('');
        this.accountForm.reset({ name: '', type: 'Checking', initialBalance: 0, openingDate: this.today() });
        this.loadAll();
        this.showMessage(id ? 'Conta atualizada com sucesso.' : 'Conta salva com sucesso.', 'success');
      },
      error: (error) => this.showMessage(this.api.errorMessage(error, 'Não foi possível salvar a conta.')),
    });
  }

  protected editAccount(account: Account): void {
    this.openView('accounts');
    this.editingAccountId.set(account.id);
    this.accountForm.setValue({
      name: account.name,
      type: account.type,
      initialBalance: account.initialBalance,
      openingDate: account.openingDate,
    });
  }

  protected cancelAccountEdit(): void {
    this.editingAccountId.set('');
    this.accountForm.reset({ name: '', type: 'Checking', initialBalance: 0, openingDate: this.today() });
  }

  protected deleteAccount(id: string): void {
    this.requestDelete('account', id, 'Excluir conta', 'Esta conta será removida se não tiver lançamentos vinculados.');
  }

  protected deleteCategory(id: string): void {
    this.requestDelete('category', id, 'Excluir categoria', 'Esta categoria será removida se não tiver lançamentos vinculados.');
  }

  protected deleteTransaction(id: string): void {
    this.requestDelete('transaction', id, 'Excluir lançamento', 'Este lançamento será removido do seu mês.');
  }

  protected cancelDelete(): void {
    this.deleteTarget.set(null);
  }

  protected confirmDelete(): void {
    const target = this.deleteTarget();
    if (!target) return;

    const requests = {
      account: this.api.deleteAccount(this.token(), target.id),
      category: this.api.deleteCategory(this.token(), target.id),
      transaction: this.api.deleteTransaction(this.token(), target.id),
    };

    const labels = {
      account: 'Conta excluída.',
      category: 'Categoria excluída.',
      transaction: 'Lançamento excluído.',
    };

    requests[target.type].subscribe({
      next: () => {
        this.deleteTarget.set(null);
        this.loadAll(undefined, target.type !== 'transaction');
        this.showMessage(labels[target.type], 'success');
      },
      error: (error) => {
        this.deleteTarget.set(null);
        this.showMessage(this.api.errorMessage(error, 'Não foi possível excluir.'), 'error');
      },
    });
  }

  protected saveCategory(): void {
    const validation = this.categoryValidationMessage();
    if (validation) {
      this.categoryForm.markAllAsTouched();
      this.showMessage(validation);
      return;
    }

    const id = this.editingCategoryId();
    const request = id
      ? this.api.updateCategory(this.token(), id, this.categoryForm.getRawValue())
      : this.api.createCategory(this.token(), this.categoryForm.getRawValue());

    request.subscribe({
      next: () => {
        this.editingCategoryId.set('');
        this.categoryForm.reset({ name: '', type: 'Expense' });
        this.loadAll();
        this.showMessage(id ? 'Categoria atualizada com sucesso.' : 'Categoria salva com sucesso.', 'success');
      },
      error: (error) => this.showMessage(this.api.errorMessage(error, 'Não foi possível salvar a categoria.')),
    });
  }

  protected editCategory(category: Category): void {
    this.openView('categories');
    this.editingCategoryId.set(category.id);
    this.categoryForm.setValue({
      name: category.name,
      type: category.type,
    });
  }

  protected cancelCategoryEdit(): void {
    this.editingCategoryId.set('');
    this.categoryForm.reset({ name: '', type: 'Expense' });
  }

  protected createTransaction(): void {
    const validation = this.transactionValidationMessage();
    if (validation) {
      this.transactionForm.markAllAsTouched();
      this.showMessage(validation);
      return;
    }

    const id = this.editingTransactionId();
    const request = id
      ? this.api.updateTransaction(this.token(), id, this.transactionForm.getRawValue())
      : this.api.createTransaction(this.token(), this.transactionForm.getRawValue());

    request.subscribe({
      next: () => {
        this.editingTransactionId.set('');
        this.transactionForm.patchValue({ amount: 0, description: '' });
        this.loadAll(undefined, false);
        this.showMessage(id ? 'Lançamento atualizado com sucesso.' : 'Lançamento salvo com sucesso.', 'success');
      },
      error: () => this.showMessage('Não foi possível salvar o lançamento.', 'error'),
    });
  }

  protected exportCsv(): void {
    const params = new URLSearchParams();
    if (this.exportStartDate() || this.exportEndDate()) {
      if (this.exportStartDate()) params.set('startDate', this.exportStartDate());
      if (this.exportEndDate()) params.set('endDate', this.exportEndDate());
    } else {
      params.set('year', String(this.selectedYear()));
      params.set('month', String(this.selectedMonthNumber()));
    }
    if (this.transactionTypeFilter()) params.set('type', this.transactionTypeFilter());
    if (this.transactionCategoryFilter()) params.set('categoryId', this.transactionCategoryFilter());
    if (this.transactionAccountFilter()) params.set('accountId', this.transactionAccountFilter());

    this.api.exportTransactions(this.token(), params).subscribe({
      next: (response) => {
        const blob = response.body;
        if (!blob) {
          this.showMessage('Não foi possível gerar o CSV.', 'error');
          return;
        }

        const contentDisposition = response.headers.get('content-disposition') ?? '';
        const fileName = /filename="?([^"]+)"?/i.exec(contentDisposition)?.[1] ?? `fintrack-transactions-${Date.now()}.csv`;
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');

        link.href = url;
        link.download = fileName;
        link.click();
        URL.revokeObjectURL(url);
        this.showMessage('CSV baixado com sucesso.', 'success');
      },
      error: (error) => this.showMessage(this.api.errorMessage(error, 'Não foi possível exportar o CSV.'), 'error'),
    });
  }

  protected readonly money = formatMoney;

  protected categoryExpenseTooltip(item: { categoryName: string; total: number }): string {
    const total = this.dashboard()?.totalExpense ?? 0;
    const percent = total > 0 ? ` (${((item.total / total) * 100).toFixed(1).replace('.', ',')}%)` : '';

    return `${item.categoryName}: ${this.money(item.total)}${percent}\nClique para ver os lançamentos`;
  }

  protected sortTransactions(field: TransactionSortField): void {
    if (this.transactionSortField() === field) {
      this.transactionSortDirection.update((direction) => direction === 'asc' ? 'desc' : 'asc');
      this.loadReport();
      return;
    }

    this.transactionSortField.set(field);
    this.transactionSortDirection.set(field === 'amount' ? 'desc' : 'asc');
    this.loadReport();
  }

  protected initial(): string {
    return this.userName().trim()[0]?.toUpperCase() ?? 'U';
  }

  protected toggleUserMenu(): void {
    this.userMenuOpen.update((open) => !open);
  }

  protected toggleSidebar(): void {
    this.sidebarHidden.update((hidden) => !hidden);
  }

  protected togglePet(): void {
    this.petOpen.update((open) => !open);
  }

  protected setPetEnabled(enabled: boolean): void {
    localStorage.setItem('fintrack.pet.enabled', String(enabled));
    this.petEnabled.set(enabled);
    this.petOpen.set(enabled && this.petOpen());
  }

  protected setPetColor(color: string): void {
    const nextColor: PetColor = ['blue', 'orange', 'purple'].includes(color) ? (color as PetColor) : 'green';
    localStorage.setItem('fintrack.pet.color', nextColor);
    this.petColor.set(nextColor);
  }

  protected setCustomCursorEnabled(enabled: boolean): void {
    localStorage.setItem('fintrack.cursor.custom', String(enabled));
    this.customCursorEnabled.set(enabled);
  }

  protected petImageSrc(): string {
    return {
      green: 'fin-pig-green.png',
      blue: 'fin-pig-blue.png',
      orange: 'fin-pig-orange.png',
      purple: 'fin-pig-purple.png',
    }[this.petColor()];
  }

  protected updatePetQuestion(value: string): void {
    this.petQuestion.set(value);
  }

  protected updateTransactionSearch(value: string): void {
    this.transactionSearch.set(value);
    this.cancelReportLoad();
    this.reportTransactions.set([]);
    this.searchTimeout = setTimeout(() => this.loadReport(), 250);
  }

  protected updateTransactionTypeFilter(value: string): void {
    this.transactionTypeFilter.set(value);
    this.loadReport();
  }

  protected updateTransactionCategoryFilter(value: string): void {
    this.transactionCategoryFilter.set(value);
    this.loadReport();
  }

  protected updateTransactionAccountFilter(value: string): void {
    this.transactionAccountFilter.set(value);
    this.loadReport();
  }

  protected clearTransactionFilters(): void {
    this.transactionSearch.set('');
    this.transactionTypeFilter.set('');
    this.transactionCategoryFilter.set('');
    this.transactionAccountFilter.set('');
    this.loadReport();
  }

  protected changeReportPage(page: number): void {
    if (!this.reportLoading() && page >= 1 && page <= this.reportPageCount()) this.loadReport(page);
  }

  protected updateExportStartDate(value: string): void {
    this.exportStartDate.set(value);
  }

  protected updateExportEndDate(value: string): void {
    this.exportEndDate.set(value);
  }

  protected editTransaction(transaction: Transaction): void {
    this.openView(transaction.type === 'Income' ? 'income' : 'expense');
    this.editingTransactionId.set(transaction.id);
    this.transactionForm.setValue({
      accountId: transaction.accountId,
      categoryId: transaction.categoryId,
      type: transaction.type,
      amount: transaction.amount,
      date: transaction.date,
      description: transaction.description ?? '',
    });
  }

  protected cancelTransactionEdit(): void {
    this.editingTransactionId.set('');
    this.transactionForm.patchValue({ amount: 0, description: '' });
  }

  protected duplicateTransaction(transaction: Transaction): void {
    this.editTransaction(transaction);
    this.editingTransactionId.set('');
  }

  protected askPet(): void {
    const question = this.petQuestion().trim().toLowerCase();
    const dashboard = this.dashboard();

    if (!question) {
      this.petAnswer.set('Escreva uma pergunta curtinha para eu olhar seus dados.');
      return;
    }

    if (!dashboard) {
      this.petAnswer.set('Ainda estou carregando seus dados. Tente atualizar o dashboard.');
      return;
    }

    if (question.includes('saldo')) {
      this.petAnswer.set(`Seu saldo atual é ${this.money(dashboard.currentBalance)}.`);
    } else if (question.includes('receita')) {
      this.petAnswer.set(`Suas receitas do mês somam ${this.money(dashboard.totalIncome)}.`);
    } else if (question.includes('despesa') || question.includes('gastei') || question.includes('gasto')) {
      this.petAnswer.set(`Suas despesas do mês somam ${this.money(dashboard.totalExpense)}.`);
    } else if (question.includes('categoria')) {
      const topCategory = dashboard.expensesByCategory[0];
      this.petAnswer.set(topCategory ? `A principal categoria é ${topCategory.categoryName}: ${this.money(topCategory.total)}.` : 'Ainda não há despesas por categoria.');
    } else if (question.includes('lançamento') || question.includes('transa')) {
      this.petAnswer.set(`Encontrei ${this.transactionCount()} lançamentos neste mês.`);
    } else {
      this.petAnswer.set('Por enquanto eu respondo sobre saldo, receitas, despesas, categorias e lançamentos.');
    }
  }

  protected openView(view: View): void {
    if (view === 'admin' && !this.isAdmin()) {
      this.showMessage('Apenas administradores podem acessar esta tela.', 'error');
      return;
    }

    if (view !== 'reports') this.cancelReportLoad();
    this.activeView.set(view);
    if (view === 'reports') this.loadReport();
    this.userMenuOpen.set(false);

    if (view === 'admin') {
      this.loadLockedUsers();
    }

    if (view === 'status') {
      this.loadSystemStatus();
    }

    if (view === 'income' || view === 'expense') {
      this.editingTransactionId.set('');
      this.transactionForm.patchValue({ type: view === 'income' ? 'Income' : 'Expense' });
    }

    if (view !== 'accounts') {
      this.cancelAccountEdit();
    }

    if (view !== 'categories') {
      this.cancelCategoryEdit();
    }
  }

  protected currentMonthLabel(): string {
    const label = new Intl.DateTimeFormat('pt-BR', { month: 'long', year: 'numeric' }).format(new Date(`${this.selectedMonth()}-01T00:00:00`));
    return label[0].toUpperCase() + label.slice(1);
  }

  protected setCurrentMonth(month: string): void {
    if (!/^\d{4}-\d{2}$/.test(month)) {
      return;
    }

    this.selectedMonth.set(month);
    this.cancelReportLoad();

    if (this.isLoggedIn()) {
      this.loadAll(undefined, false);
    }
  }

  protected chartRangeLabel(): string {
    const total = this.chartMonths().length;
    return total === 1 ? this.currentMonthLabel() : `${total} meses`;
  }

  protected pageTitle(): string {
    const titles: Record<View, string> = {
      dashboard: 'Dashboard',
      income: 'Receitas',
      expense: 'Despesas',
      accounts: 'Contas',
      categories: 'Categorias',
      reports: 'Relatórios',
      categorySummary: 'Resumo por categoria',
      export: 'Exportação CSV',
      profile: 'Meu perfil',
      password: 'Alterar senha',
      admin: 'Administração',
      status: 'Status do sistema',
      preferences: 'Preferências',
      about: 'Sobre',
    };

    return titles[this.activeView()];
  }

  protected setTheme(theme: string): void {
    const nextTheme: Theme = theme === 'dark' ? 'dark' : 'light';
    localStorage.setItem(this.themeStorageKey(this.userEmail()), nextTheme);
    this.theme.set(nextTheme);
  }

  ngOnDestroy(): void {
    this.loadSubscription?.unsubscribe();
    this.cancelReportLoad();
  }

  protected loadAll(successMessage?: string, reloadCatalogs = true): void {
    this.cancelReportLoad();
    const token = this.token();
    reloadCatalogs ||= !this.catalogsLoaded;
    if (reloadCatalogs) this.catalogsLoaded = false;
    this.loadSubscription?.unsubscribe();
    this.isLoading.set(true);
    this.loadSubscription = forkJoin({
      accounts: reloadCatalogs ? this.api.getAccounts(token) : of(this.accounts()),
      categories: reloadCatalogs ? this.api.getCategories(token) : of(this.categories()),
      transactions: this.api.getTransactions(token, this.selectedYear(), this.selectedMonthNumber(), new URLSearchParams({ pageSize: '5' })),
      dashboard: this.api.getDashboard(token, this.selectedYear(), this.selectedMonthNumber()),
    }).pipe(finalize(() => this.isLoading.set(false))).subscribe({
      next: ({ accounts, categories, transactions, dashboard }) => {
        this.catalogsLoaded = true;
        this.accounts.set(accounts);
        this.categories.set(categories);
        this.transactions.set(transactions.items);
        this.transactionCount.set(transactions.totalCount);
        this.dashboard.set(dashboard);
        if (this.activeView() === 'reports') this.loadReport();
        if (successMessage) {
          this.showMessage(successMessage, 'success');
        }
      },
      error: (error) => this.showMessage(this.api.errorMessage(error, 'Não foi possível carregar seus dados.'), 'error'),
    });
  }

  protected refreshData(): void {
    this.loadAll('Dados atualizados.');
  }

  protected loadLockedUsers(): void {
    this.api.getLockedUsers(this.token()).subscribe({
      next: (users) => this.lockedUsers.set(users),
      error: (error) => this.showMessage(this.api.errorMessage(error, 'Não foi possível carregar usuários bloqueados.'), 'error'),
    });
  }

  protected loadSystemStatus(): void {
    this.api.getHealth().subscribe({
      next: (response) => {
        this.healthStatus.set(response.body);
        this.healthCorrelationId.set(response.headers.get('X-Correlation-Id') ?? '');
        this.healthCheckedAt.set(new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'medium' }).format(new Date()));
      },
      error: (error) => {
        this.healthStatus.set(null);
        this.healthCorrelationId.set(error.headers?.get('X-Correlation-Id') ?? '');
        this.healthCheckedAt.set(new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'medium' }).format(new Date()));
        this.showMessage(this.api.errorMessage(error, 'Não foi possível consultar o status do sistema.'), 'error');
      },
    });
  }

  protected saveProfile(): void {
    const name = this.profileForm.controls.name.value.trim();
    if (name.length < 2 || name.length > 80) {
      this.profileForm.markAllAsTouched();
      this.showMessage('Informe um nome entre 2 e 80 caracteres.', 'error');
      return;
    }

    this.api.updateProfile(this.token(), { name }).subscribe({
      next: (auth) => this.applyAuth(auth, 'Perfil atualizado com sucesso.'),
      error: (error) => this.showMessage(this.api.errorMessage(error, 'Não foi possível atualizar o perfil.'), 'error'),
    });
  }

  protected changePassword(): void {
    const currentPassword = this.passwordForm.controls.currentPassword.value;
    const newPassword = this.passwordForm.controls.newPassword.value;
    const confirmPassword = this.passwordForm.controls.confirmPassword.value;

    if (!currentPassword) {
      this.passwordForm.markAllAsTouched();
      this.showMessage('Informe a senha atual.', 'error');
      return;
    }

    if (!this.isStrongPassword(newPassword)) {
      this.passwordForm.markAllAsTouched();
      this.showMessage('A nova senha deve ter de 8 a 100 caracteres, com letra maiúscula, letra minúscula, número e caractere especial.', 'error');
      return;
    }

    if (newPassword !== confirmPassword) {
      this.passwordForm.markAllAsTouched();
      this.showMessage('A confirmação da senha não confere.', 'error');
      return;
    }

    this.api.changePassword(this.token(), { currentPassword, newPassword }).subscribe({
      next: () => {
        this.passwordForm.reset({ currentPassword: '', newPassword: '', confirmPassword: '' });
        this.showMessage('Senha alterada com sucesso.', 'success');
      },
      error: (error) => this.showMessage(this.api.errorMessage(error, 'Não foi possível alterar a senha.'), 'error'),
    });
  }

  protected resetLockedPassword(user: LockedUser): void {
    const newPassword = this.adminResetForm.controls.newPassword.value;
    if (!this.isStrongPassword(newPassword)) {
      this.adminResetForm.markAllAsTouched();
      this.showMessage('Informe uma senha temporária forte.', 'error');
      return;
    }

    this.api.resetLockedPassword(this.token(), user.id, { newPassword }).subscribe({
      next: () => {
        this.adminResetForm.reset({ newPassword: '' });
        this.loadLockedUsers();
        this.showMessage(`Senha de ${user.name} redefinida e conta desbloqueada.`, 'success');
      },
      error: (error) => this.showMessage(this.api.errorMessage(error, 'Não foi possível redefinir a senha.'), 'error'),
    });
  }

  protected showAllTransactions(): void {
    this.transactionSearch.set('');
    this.transactionTypeFilter.set('');
    this.transactionCategoryFilter.set('');
    this.transactionAccountFilter.set('');
    this.openView('reports');
  }

  protected showTransactionsByType(type: TransactionType): void {
    this.transactionSearch.set('');
    this.transactionTypeFilter.set(type);
    this.transactionCategoryFilter.set('');
    this.transactionAccountFilter.set('');
    this.openView('reports');
  }

  protected showTransactionsByCategory(categoryName: string): void {
    const categoryId = this.categories().find((category) => category.name === categoryName)?.id ?? '';
    this.transactionSearch.set('');
    this.transactionTypeFilter.set('Expense');
    this.transactionCategoryFilter.set(categoryId);
    this.transactionAccountFilter.set('');
    this.openView('reports');
  }

  protected openCategorySummary(): void {
    this.openView('categorySummary');
  }

  protected showAllAccounts(): void {
    this.loadAll();
    this.openView('accounts');
  }

  protected startSession(auth: AuthResponse): void {
    this.isEntering.set(true);
    this.applyAuth(auth);
    this.loadAll();
    setTimeout(() => this.isEntering.set(false), 1400);
  }

  private applyAuth(auth: AuthResponse, message?: string): void {
    this.api.saveSession(auth);
    this.token.set(auth.token);
    this.userName.set(auth.name);
    this.userEmail.set(auth.email);
    this.isAdmin.set(auth.isAdmin);
    this.theme.set(this.savedThemeFor(auth.email));
    this.profileForm.patchValue({ name: auth.name });
    this.message.set('');
    this.isMessageLeaving.set(false);
    if (message) {
      this.showMessage(message, 'success');
    }
  }

  protected closeMessage(): void {
    this.message.set('');
    this.isMessageLeaving.set(false);
  }

  protected messageIcon(): string {
    return { success: '✓', error: '!', info: 'i' }[this.messageKind()];
  }

  protected messageStyle(): string {
    return {
      success: 'background:#ecfdf3;border-color:#bbf7d0;color:#047857;box-shadow:0 24px 70px #0f172a30',
      error: 'background:#fef2f2;border-color:#fecaca;color:#b91c1c;box-shadow:0 24px 70px #0f172a30',
      info: 'background:#eff6ff;border-color:#bfdbfe;color:#1d4ed8;box-shadow:0 24px 70px #0f172a30',
    }[this.messageKind()];
  }

  protected showMessage(text: string, kind: 'success' | 'error' | 'info' = 'info'): void {
    this.messageKind.set(kind);
    if (this.messageTimeoutId) {
      clearTimeout(this.messageTimeoutId);
    }

    if (this.messageClearTimeoutId) {
      clearTimeout(this.messageClearTimeoutId);
    }

    this.isMessageLeaving.set(false);
    this.message.set(text);
    this.messageTimeoutId = setTimeout(() => {
      this.isMessageLeaving.set(true);
      this.messageTimeoutId = null;
      this.messageClearTimeoutId = setTimeout(() => {
        this.message.set('');
        this.isMessageLeaving.set(false);
        this.messageClearTimeoutId = null;
      }, 520);
    }, 3500);
  }

  private savedThemeFor(email: string): Theme {
    return (localStorage.getItem(this.themeStorageKey(email)) as Theme | null) ?? 'light';
  }

  private themeStorageKey(email: string): string {
    return `fintrack.theme.${email.trim().toLowerCase()}`;
  }

  private accountValidationMessage(): string | null {
    const name = this.accountForm.controls.name.value.trim();

    if (name.length < 2) {
      return 'Informe uma conta com pelo menos 2 caracteres.';
    }

    if (name.length > 80) {
      return 'O nome da conta deve ter no máximo 80 caracteres.';
    }

    return null;
  }

  private categoryValidationMessage(): string | null {
    const name = this.categoryForm.controls.name.value.trim();

    if (name.length < 2) {
      return 'Informe uma categoria com pelo menos 2 caracteres.';
    }

    if (name.length > 80) {
      return 'O nome da categoria deve ter no máximo 80 caracteres.';
    }

    return null;
  }

  private transactionValidationMessage(): string | null {
    const form = this.transactionForm.controls;
    const amount = Number(form.amount.value);
    const category = this.categories().find((item) => item.id === form.categoryId.value);
    const description = form.description.value.trim();

    if (!form.accountId.value) {
      return 'Selecione uma conta.';
    }

    if (!form.categoryId.value) {
      return 'Selecione uma categoria.';
    }

    if (!Number.isFinite(amount) || amount <= 0) {
      return 'Informe um valor maior que zero.';
    }

    if (!form.date.value) {
      return 'Informe a data.';
    }

    if (description.length > 160) {
      return 'A descrição deve ter no máximo 160 caracteres.';
    }

    if (category && category.type !== form.type.value) {
      return 'A categoria deve ser do mesmo tipo do lançamento.';
    }

    return null;
  }

  private shortMonthLabel(date: Date): string {
    const month = new Intl.DateTimeFormat('pt-BR', { month: 'short' }).format(date).replace('.', '');
    return `${month[0].toUpperCase()}${month.slice(1)}/${String(date.getFullYear()).slice(2)}`;
  }

  private currentMonthKey(): string {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
  }

  private today(): string {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
  }

  private cancelReportLoad(): void {
    clearTimeout(this.searchTimeout);
    this.reportSubscription?.unsubscribe();
  }

  private loadReport(page = 1): void {
    this.cancelReportLoad();
    if (!this.isLoggedIn()) return;
    const filters = new URLSearchParams({
      page: String(page), pageSize: '50',
      sort: this.transactionSortField(), direction: this.transactionSortDirection(),
    });
    if (this.transactionSearch().trim()) filters.set('search', this.transactionSearch().trim());
    if (this.transactionTypeFilter()) filters.set('type', this.transactionTypeFilter());
    if (this.transactionCategoryFilter()) filters.set('categoryId', this.transactionCategoryFilter());
    if (this.transactionAccountFilter()) filters.set('accountId', this.transactionAccountFilter());
    this.reportTransactions.set([]);
    this.reportPage.set(page);
    this.reportLoading.set(true);
    this.reportSubscription = this.api.getTransactions(this.token(), this.selectedYear(), this.selectedMonthNumber(), filters)
      .pipe(finalize(() => this.reportLoading.set(false))).subscribe({
        next: (result) => {
          this.reportTransactions.set(result.items);
          this.reportTotal.set(result.totalCount);
          if (page > 1 && result.items.length === 0) this.loadReport(Math.max(1, Math.ceil(result.totalCount / 50)));
        },
        error: (error) => {
          this.reportTotal.set(0);
          this.reportPage.set(1);
          this.showMessage(this.api.errorMessage(error, 'Não foi possível carregar os lançamentos.'), 'error');
        },
      });
  }

  private requestDelete(type: DeleteTarget['type'], id: string, title: string, description: string): void {
    this.deleteTarget.set({ type, id, title, description });
  }
}

import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

type AccountType = 'Wallet' | 'Checking' | 'Savings' | 'CreditCard';
type CategoryType = 'Income' | 'Expense';
type TransactionType = 'Income' | 'Expense';
type Theme = 'light' | 'dark';
type AuthMode = 'login' | 'register';
type PetColor = 'green' | 'blue' | 'orange' | 'purple';
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
  | 'preferences'
  | 'about';

interface AuthResponse {
  name: string;
  email: string;
  token: string;
}

interface Account {
  id: string;
  name: string;
  type: AccountType;
  initialBalance: number;
}

interface Category {
  id: string;
  name: string;
  type: CategoryType;
}

interface Transaction {
  id: string;
  accountId: string;
  categoryId: string;
  type: TransactionType;
  amount: number;
  date: string;
  description: string | null;
}

interface Dashboard {
  totalIncome: number;
  totalExpense: number;
  monthBalance: number;
  currentBalance: number;
  expensesByCategory: { categoryName: string; total: number }[];
}

@Component({
  selector: 'app-root',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);
  private readonly apiUrl = 'http://localhost:5080';
  private messageTimeoutId: ReturnType<typeof setTimeout> | null = null;
  private messageClearTimeoutId: ReturnType<typeof setTimeout> | null = null;

  protected readonly token = signal(sessionStorage.getItem('fintrack.token') ?? '');
  protected readonly userName = signal(sessionStorage.getItem('fintrack.name') ?? '');
  protected readonly userEmail = signal(sessionStorage.getItem('fintrack.email') ?? '');
  protected readonly message = signal('');
  protected readonly messageKind = signal<'success' | 'error' | 'info'>('info');
  protected readonly isMessageLeaving = signal(false);
  protected readonly accounts = signal<Account[]>([]);
  protected readonly categories = signal<Category[]>([]);
  protected readonly transactions = signal<Transaction[]>([]);
  protected readonly dashboard = signal<Dashboard | null>(null);
  protected readonly userMenuOpen = signal(false);
  protected readonly sidebarHidden = signal(false);
  protected readonly petOpen = signal(false);
  protected readonly petEnabled = signal(localStorage.getItem('fintrack.pet.enabled') !== 'false');
  protected readonly petColor = signal<PetColor>((localStorage.getItem('fintrack.pet.color') as PetColor | null) ?? 'green');
  protected readonly petQuestion = signal('');
  protected readonly petAnswer = signal('Oi, sou o Fin. Pergunte sobre saldo, receitas, despesas ou categorias.');
  protected readonly transactionSearch = signal('');
  protected readonly transactionTypeFilter = signal('');
  protected readonly transactionCategoryFilter = signal('');
  protected readonly transactionAccountFilter = signal('');
  protected readonly transactionSortField = signal<TransactionSortField>('date');
  protected readonly transactionSortDirection = signal<'asc' | 'desc'>('desc');
  protected readonly recentLimit = signal(5);
  protected readonly editingTransactionId = signal('');
  protected readonly authMode = signal<AuthMode>('login');
  protected readonly activeView = signal<View>('dashboard');
  protected readonly theme = signal<Theme>((localStorage.getItem('fintrack.theme') as Theme | null) ?? 'light');
  protected readonly selectedMonth = signal(localStorage.getItem('fintrack.month') ?? new Date().toISOString().slice(0, 7));

  protected readonly isLoggedIn = computed(() => this.token().length > 0);
  protected readonly selectedYear = computed(() => Number(this.selectedMonth().slice(0, 4)));
  protected readonly selectedMonthNumber = computed(() => Number(this.selectedMonth().slice(5, 7)));
  protected readonly filteredTransactions = computed(() => {
    const search = this.transactionSearch().trim().toLowerCase();
    const type = this.transactionTypeFilter();
    const categoryId = this.transactionCategoryFilter();
    const accountId = this.transactionAccountFilter();

    return this.transactions().filter((transaction) => {
      const searchable = `${transaction.date} ${transaction.description ?? ''} ${this.categoryName(transaction.categoryId)} ${this.accountName(transaction.accountId)}`.toLowerCase();
      return (!search || searchable.includes(search)) && (!type || transaction.type === type) && (!categoryId || transaction.categoryId === categoryId) && (!accountId || transaction.accountId === accountId);
    }).sort((left, right) => this.compareTransactions(left, right));
  });
  protected readonly recentTransactions = computed(() => this.transactions().slice(0, this.recentLimit()));
  protected readonly hasMoreTransactions = computed(() => this.transactions().length > this.recentLimit());
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
    const monthTotals = new Map<string, { label: string; incomeAmount: number; expenseAmount: number }>();

    for (const transaction of this.transactions()) {
      const date = new Date(`${transaction.date}T00:00:00`);
      const key = transaction.date.slice(0, 7);
      const current = monthTotals.get(key) ?? {
        label: this.shortMonthLabel(date),
        incomeAmount: 0,
        expenseAmount: 0,
      };

      if (transaction.type === 'Income') {
        current.incomeAmount += transaction.amount;
      } else {
        current.expenseAmount += transaction.amount;
      }

      monthTotals.set(key, current);
    }

    const rows = [...monthTotals.entries()]
      .sort(([left], [right]) => left.localeCompare(right))
      .map(([, value]) => value);
    const maxAmount = Math.max(...rows.flatMap((month) => [month.incomeAmount, month.expenseAmount]), 0);

    return rows.map((month) => ({
      label: month.label,
      income: maxAmount > 0 ? Math.max((month.incomeAmount / maxAmount) * 100, month.incomeAmount > 0 ? 12 : 0) : 0,
      expense: maxAmount > 0 ? Math.max((month.expenseAmount / maxAmount) * 100, month.expenseAmount > 0 ? 12 : 0) : 0,
      incomeAmount: month.incomeAmount,
      expenseAmount: month.expenseAmount,
    }));
  });

  protected readonly loginForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(120)]],
    password: ['', [Validators.required, Validators.maxLength(100)]],
  });

  protected readonly registerForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(80)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(120)]],
    password: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(100)]],
  });

  protected readonly accountForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(80)]],
    type: ['Checking' as AccountType, Validators.required],
    initialBalance: [0, Validators.required],
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
    date: [new Date().toISOString().slice(0, 10), Validators.required],
    description: ['', Validators.maxLength(160)],
  });

  constructor() {
    if (this.isLoggedIn()) {
      this.loadAll();
    }
  }

  protected login(): void {
    const validation = this.loginValidationMessage();
    if (validation) {
      this.loginForm.markAllAsTouched();
      this.showMessage(validation);
      return;
    }

    this.http.post<AuthResponse>(`${this.apiUrl}/auth/login`, this.loginForm.getRawValue()).subscribe({
      next: (auth) => this.startSession(auth),
      error: () => this.showMessage('Não foi possível entrar.'),
    });
  }

  protected register(): void {
    const validation = this.registerValidationMessage();
    if (validation) {
      this.registerForm.markAllAsTouched();
      this.showMessage(validation);
      return;
    }

    this.http.post<AuthResponse>(`${this.apiUrl}/auth/register`, this.registerForm.getRawValue()).subscribe({
      next: (auth) => this.startSession(auth),
      error: () => this.showMessage('Não foi possível criar a conta.'),
    });
  }

  protected showLogin(): void {
    this.authMode.set('login');
    this.message.set('');
    this.isMessageLeaving.set(false);
  }

  protected showRegister(): void {
    this.authMode.set('register');
    this.message.set('');
    this.isMessageLeaving.set(false);
  }

  protected logout(): void {
    localStorage.removeItem('fintrack.token');
    localStorage.removeItem('fintrack.name');
    localStorage.removeItem('fintrack.email');
    sessionStorage.removeItem('fintrack.token');
    sessionStorage.removeItem('fintrack.name');
    sessionStorage.removeItem('fintrack.email');
    this.token.set('');
    this.userName.set('');
    this.userEmail.set('');
    this.message.set('');
    this.isMessageLeaving.set(false);
    this.userMenuOpen.set(false);
  }

  protected createAccount(): void {
    const validation = this.accountValidationMessage();
    if (validation) {
      this.accountForm.markAllAsTouched();
      this.showMessage(validation);
      return;
    }

    this.http.post<Account>(`${this.apiUrl}/accounts`, this.accountForm.getRawValue(), this.options()).subscribe({
      next: () => {
        this.accountForm.reset({ name: '', type: 'Checking', initialBalance: 0 });
        this.loadAll();
        this.showMessage('Conta salva com sucesso.');
      },
      error: (error) => this.showMessage(this.errorMessage(error, 'Não foi possível salvar a conta.')),
    });
  }

  protected deleteAccount(id: string): void {
    if (!confirm('Excluir esta conta?')) {
      return;
    }

    this.http.delete(`${this.apiUrl}/accounts/${id}`, this.options()).subscribe({
      next: () => {
        this.loadAll();
        this.showMessage('Conta excluída.', 'success');
      },
      error: (error) => this.showMessage(this.errorMessage(error, 'Não foi possível excluir a conta.'), 'error'),
    });
  }

  protected createCategory(): void {
    const validation = this.categoryValidationMessage();
    if (validation) {
      this.categoryForm.markAllAsTouched();
      this.showMessage(validation);
      return;
    }

    this.http.post<Category>(`${this.apiUrl}/categories`, this.categoryForm.getRawValue(), this.options()).subscribe({
      next: () => {
        this.categoryForm.reset({ name: '', type: 'Expense' });
        this.loadAll();
        this.showMessage('Categoria salva com sucesso.');
      },
      error: (error) => this.showMessage(this.errorMessage(error, 'Não foi possível salvar a categoria.')),
    });
  }

  protected deleteCategory(id: string): void {
    this.http.delete(`${this.apiUrl}/categories/${id}`, this.options()).subscribe({
      next: () => this.loadAll(),
      error: (error) => this.showMessage(this.errorMessage(error, 'Não foi possível excluir a categoria.')),
    });
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
      ? this.http.put<Transaction>(`${this.apiUrl}/transactions/${id}`, this.transactionForm.getRawValue(), this.options())
      : this.http.post<Transaction>(`${this.apiUrl}/transactions`, this.transactionForm.getRawValue(), this.options());

    request.subscribe({
      next: () => {
        this.editingTransactionId.set('');
        this.transactionForm.patchValue({ amount: 0, description: '' });
        this.loadAll();
        this.showMessage(id ? 'Lançamento atualizado com sucesso.' : 'Lançamento salvo com sucesso.', 'success');
      },
      error: () => this.showMessage('Não foi possível salvar o lançamento.', 'error'),
    });
  }

  protected deleteTransaction(id: string): void {
    if (!confirm('Excluir este lançamento?')) {
      return;
    }

    this.http.delete(`${this.apiUrl}/transactions/${id}`, this.options()).subscribe({
      next: () => {
        this.loadAll();
        this.showMessage('Lançamento excluído.', 'success');
      },
      error: (error) => this.showMessage(this.errorMessage(error, 'Não foi possível excluir o lançamento.'), 'error'),
    });
  }

  protected exportCsv(): void {
    const params = new URLSearchParams({ year: String(this.selectedYear()), month: String(this.selectedMonthNumber()) });
    if (this.transactionTypeFilter()) params.set('type', this.transactionTypeFilter());
    if (this.transactionCategoryFilter()) params.set('categoryId', this.transactionCategoryFilter());
    if (this.transactionAccountFilter()) params.set('accountId', this.transactionAccountFilter());
    window.open(`${this.apiUrl}/exports/transactions.csv?${params}`, '_blank');
  }

  protected accountName(id: string): string {
    return this.accounts().find((account) => account.id === id)?.name ?? 'Conta';
  }

  protected categoryName(id: string): string {
    return this.categories().find((category) => category.id === id)?.name ?? 'Categoria';
  }

  protected accountTypeLabel(type: AccountType): string {
    const labels: Record<AccountType, string> = {
      Wallet: 'Carteira',
      Checking: 'Conta corrente',
      Savings: 'Poupança',
      CreditCard: 'Cartão de crédito',
    };

    return labels[type];
  }

  protected categoryTypeLabel(type: CategoryType): string {
    return type === 'Income' ? 'Receita' : 'Despesa';
  }

  protected transactionTypeLabel(type: TransactionType): string {
    return type === 'Income' ? 'Receita' : 'Despesa';
  }

  protected money(value: number): string {
    return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);
  }

  protected cashFlowTooltip(month: { label: string; incomeAmount: number; expenseAmount: number }): string {
    return `${month.label}\nReceitas: ${this.money(month.incomeAmount)}\nDespesas: ${this.money(month.expenseAmount)}\nSaldo: ${this.money(month.incomeAmount - month.expenseAmount)}`;
  }

  protected expenseDonutTooltip(): string {
    const total = this.dashboard()?.totalExpense ?? 0;

    return total > 0 ? `Total de despesas do mês: ${this.money(total)}` : 'Sem despesas neste mês';
  }

  protected categoryExpenseTooltip(item: { categoryName: string; total: number }): string {
    const total = this.dashboard()?.totalExpense ?? 0;
    const percent = total > 0 ? ` (${((item.total / total) * 100).toFixed(1).replace('.', ',')}%)` : '';

    return `${item.categoryName}: ${this.money(item.total)}${percent}\nClique para ver os lançamentos`;
  }

  protected sortTransactions(field: TransactionSortField): void {
    if (this.transactionSortField() === field) {
      this.transactionSortDirection.update((direction) => direction === 'asc' ? 'desc' : 'asc');
      return;
    }

    this.transactionSortField.set(field);
    this.transactionSortDirection.set(field === 'amount' ? 'desc' : 'asc');
  }

  protected sortLabel(field: TransactionSortField): string {
    return this.transactionSortField() === field ? (this.transactionSortDirection() === 'asc' ? ' ↑' : ' ↓') : '';
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

  protected petColorValue(): string {
    return {
      green: '#10b981',
      blue: '#3b82f6',
      orange: '#f97316',
      purple: '#8b5cf6',
    }[this.petColor()];
  }

  protected petBorderColor(): string {
    return {
      green: '#047857',
      blue: '#1d4ed8',
      orange: '#c2410c',
      purple: '#6d28d9',
    }[this.petColor()];
  }

  protected updatePetQuestion(value: string): void {
    this.petQuestion.set(value);
  }

  protected updateTransactionSearch(value: string): void {
    this.transactionSearch.set(value);
  }

  protected updateTransactionTypeFilter(value: string): void {
    this.transactionTypeFilter.set(value);
  }

  protected updateTransactionCategoryFilter(value: string): void {
    this.transactionCategoryFilter.set(value);
  }

  protected updateTransactionAccountFilter(value: string): void {
    this.transactionAccountFilter.set(value);
  }

  protected showMoreTransactions(): void {
    this.recentLimit.update((limit) => limit + 5);
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
      this.petAnswer.set(`Encontrei ${this.transactions().length} lançamentos neste mês.`);
    } else {
      this.petAnswer.set('Por enquanto eu respondo sobre saldo, receitas, despesas, categorias e lançamentos.');
    }
  }

  protected openView(view: View): void {
    this.activeView.set(view);
    this.userMenuOpen.set(false);

    if (view === 'income' || view === 'expense') {
      this.editingTransactionId.set('');
      this.transactionForm.patchValue({ type: view === 'income' ? 'Income' : 'Expense' });
    }
  }

  protected currentMonthValue(): string {
    return this.selectedMonth();
  }

  protected currentMonthLabel(): string {
    const label = new Intl.DateTimeFormat('pt-BR', { month: 'long', year: 'numeric' }).format(new Date(`${this.selectedMonth()}-01T00:00:00`));
    return label[0].toUpperCase() + label.slice(1);
  }

  protected setCurrentMonth(month: string): void {
    if (!/^\d{4}-\d{2}$/.test(month)) {
      return;
    }

    localStorage.setItem('fintrack.month', month);
    this.selectedMonth.set(month);
    this.recentLimit.set(5);

    if (this.isLoggedIn()) {
      this.loadAll();
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
      preferences: 'Preferências',
      about: 'Sobre',
    };

    return titles[this.activeView()];
  }

  protected setTheme(theme: string): void {
    const nextTheme: Theme = theme === 'dark' ? 'dark' : 'light';
    localStorage.setItem('fintrack.theme', nextTheme);
    this.theme.set(nextTheme);
  }

  protected loadAll(): void {
    const options = this.options();
    this.http.get<Account[]>(`${this.apiUrl}/accounts`, options).subscribe((accounts) => this.accounts.set(accounts));
    this.http.get<Category[]>(`${this.apiUrl}/categories`, options).subscribe((categories) => this.categories.set(categories));
    this.http
      .get<Transaction[]>(`${this.apiUrl}/transactions?year=${this.selectedYear()}&month=${this.selectedMonthNumber()}`, options)
      .subscribe((transactions) => this.transactions.set(transactions));
    this.http
      .get<Dashboard>(`${this.apiUrl}/dashboard/monthly?year=${this.selectedYear()}&month=${this.selectedMonthNumber()}`, options)
      .subscribe((dashboard) => this.dashboard.set(dashboard));
  }

  protected refreshData(): void {
    this.loadAll();
    this.showMessage('Dados atualizados.', 'success');
  }

  protected showUnavailableFeature(feature: string): void {
    this.showMessage(`${feature} ainda não está disponível no MVP.`);
  }

  protected showAllTransactions(): void {
    this.loadAll();
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

  private startSession(auth: AuthResponse): void {
    sessionStorage.setItem('fintrack.token', auth.token);
    sessionStorage.setItem('fintrack.name', auth.name);
    sessionStorage.setItem('fintrack.email', auth.email);
    this.token.set(auth.token);
    this.userName.set(auth.name);
    this.userEmail.set(auth.email);
    this.message.set('');
    this.isMessageLeaving.set(false);
    this.loadAll();
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
      success: 'background:#ecfdf3;border-color:#bbf7d0;color:#047857',
      error: 'background:#fef2f2;border-color:#fecaca;color:#b91c1c',
      info: 'background:#eff6ff;border-color:#bfdbfe;color:#1d4ed8',
    }[this.messageKind()];
  }

  private showMessage(text: string, kind: 'success' | 'error' | 'info' = 'info'): void {
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

  private errorMessage(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse && typeof error.error === 'string' && error.error.trim().length > 0) {
      return error.error;
    }

    return fallback;
  }

  private loginValidationMessage(): string | null {
    const email = this.loginForm.controls.email.value.trim();
    const password = this.loginForm.controls.password.value;

    if (!email) {
      return 'Informe o e-mail.';
    }

    if (this.loginForm.controls.email.invalid) {
      return 'Informe um e-mail válido.';
    }

    if (!password) {
      return 'Informe a senha.';
    }

    return null;
  }

  private registerValidationMessage(): string | null {
    const name = this.registerForm.controls.name.value.trim();
    const email = this.registerForm.controls.email.value.trim();
    const password = this.registerForm.controls.password.value;

    if (name.length < 2) {
      return 'Informe um nome com pelo menos 2 caracteres.';
    }

    if (name.length > 80) {
      return 'O nome deve ter no máximo 80 caracteres.';
    }

    if (!email) {
      return 'Informe o e-mail.';
    }

    if (this.registerForm.controls.email.invalid) {
      return 'Informe um e-mail válido.';
    }

    if (password.length < 6) {
      return 'A senha deve ter pelo menos 6 caracteres.';
    }

    if (password.length > 100) {
      return 'A senha deve ter no máximo 100 caracteres.';
    }

    return null;
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

  private options(): { headers: HttpHeaders } {
    return {
      headers: new HttpHeaders({ Authorization: `Bearer ${this.token()}` }),
    };
  }

  private compareTransactions(left: Transaction, right: Transaction): number {
    const field = this.transactionSortField();
    const direction = this.transactionSortDirection() === 'asc' ? 1 : -1;
    const leftValue = field === 'category' ? this.categoryName(left.categoryId) : field === 'amount' ? left.amount : left[field] ?? '';
    const rightValue = field === 'category' ? this.categoryName(right.categoryId) : field === 'amount' ? right.amount : right[field] ?? '';

    return (typeof leftValue === 'number' && typeof rightValue === 'number'
      ? leftValue - rightValue
      : String(leftValue).localeCompare(String(rightValue), 'pt-BR')) * direction;
  }
}

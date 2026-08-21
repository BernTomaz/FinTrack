import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

type AccountType = 'Wallet' | 'Checking' | 'Savings' | 'CreditCard';
type CategoryType = 'Income' | 'Expense';
type TransactionType = 'Income' | 'Expense';
type Theme = 'light' | 'dark';
type View =
  | 'dashboard'
  | 'income'
  | 'expense'
  | 'accounts'
  | 'categories'
  | 'reports'
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
  protected readonly isMessageLeaving = signal(false);
  protected readonly accounts = signal<Account[]>([]);
  protected readonly categories = signal<Category[]>([]);
  protected readonly transactions = signal<Transaction[]>([]);
  protected readonly dashboard = signal<Dashboard | null>(null);
  protected readonly userMenuOpen = signal(false);
  protected readonly activeView = signal<View>('dashboard');
  protected readonly theme = signal<Theme>((localStorage.getItem('fintrack.theme') as Theme | null) ?? 'light');

  protected readonly isLoggedIn = computed(() => this.token().length > 0);
  protected readonly accountTypes: AccountType[] = ['Wallet', 'Checking', 'Savings', 'CreditCard'];
  protected readonly categoryTypes: CategoryType[] = ['Income', 'Expense'];
  protected readonly transactionTypes: TransactionType[] = ['Income', 'Expense'];
  protected readonly chartMonths = [
    { label: 'Jan/26', income: 62, expense: 48 },
    { label: 'Fev/26', income: 70, expense: 52 },
    { label: 'Mar/26', income: 78, expense: 51 },
    { label: 'Abr/26', income: 80, expense: 55 },
    { label: 'Mai/26', income: 77, expense: 53 },
    { label: 'Jun/26', income: 78, expense: 51 },
    { label: 'Jul/26', income: 77, expense: 50 },
    { label: 'Ago/26', income: 72, expense: 49 },
  ];

  protected readonly loginForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  protected readonly registerForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  protected readonly accountForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    type: ['Checking' as AccountType, Validators.required],
    initialBalance: [0, Validators.required],
  });

  protected readonly categoryForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    type: ['Expense' as CategoryType, Validators.required],
  });

  protected readonly transactionForm = this.fb.nonNullable.group({
    accountId: ['', Validators.required],
    categoryId: ['', Validators.required],
    type: ['Expense' as TransactionType, Validators.required],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    date: [new Date().toISOString().slice(0, 10), Validators.required],
    description: [''],
  });

  constructor() {
    if (this.isLoggedIn()) {
      this.loadAll();
    }
  }

  protected login(): void {
    if (this.loginForm.invalid) {
      this.showMessage('Informe e-mail e senha.');
      return;
    }

    this.http.post<AuthResponse>(`${this.apiUrl}/auth/login`, this.loginForm.getRawValue()).subscribe({
      next: (auth) => this.startSession(auth),
      error: () => this.showMessage('Não foi possível entrar.'),
    });
  }

  protected register(): void {
    if (this.registerForm.invalid) {
      this.showMessage('Preencha nome, e-mail e senha.');
      return;
    }

    this.http.post<AuthResponse>(`${this.apiUrl}/auth/register`, this.registerForm.getRawValue()).subscribe({
      next: (auth) => this.startSession(auth),
      error: () => this.showMessage('Não foi possível criar a conta.'),
    });
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
    if (this.accountForm.invalid) {
      return;
    }

    this.http.post<Account>(`${this.apiUrl}/accounts`, this.accountForm.getRawValue(), this.options()).subscribe({
      next: () => {
        this.accountForm.reset({ name: '', type: 'Checking', initialBalance: 0 });
        this.loadAll();
      },
      error: (error) => this.showMessage(this.errorMessage(error, 'Não foi possível salvar a conta.')),
    });
  }

  protected deleteAccount(id: string): void {
    this.http.delete(`${this.apiUrl}/accounts/${id}`, this.options()).subscribe({
      next: () => this.loadAll(),
      error: (error) => this.showMessage(this.errorMessage(error, 'Não foi possível excluir a conta.')),
    });
  }

  protected createCategory(): void {
    if (this.categoryForm.invalid) {
      return;
    }

    this.http.post<Category>(`${this.apiUrl}/categories`, this.categoryForm.getRawValue(), this.options()).subscribe({
      next: () => {
        this.categoryForm.reset({ name: '', type: 'Expense' });
        this.loadAll();
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
    if (this.transactionForm.invalid) {
      return;
    }

    this.http
      .post<Transaction>(`${this.apiUrl}/transactions`, this.transactionForm.getRawValue(), this.options())
      .subscribe({
        next: () => {
          this.transactionForm.patchValue({ amount: 0, description: '' });
          this.loadAll();
        },
        error: () => this.showMessage('Não foi possível salvar o lançamento.'),
      });
  }

  protected deleteTransaction(id: string): void {
    this.http.delete(`${this.apiUrl}/transactions/${id}`, this.options()).subscribe({
      next: () => this.loadAll(),
      error: (error) => this.showMessage(this.errorMessage(error, 'Não foi possível excluir o lançamento.')),
    });
  }

  protected exportCsv(): void {
    window.open(`${this.apiUrl}/exports/transactions.csv?year=2026&month=8`, '_blank');
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

  protected initial(): string {
    return this.userName().trim()[0]?.toUpperCase() ?? 'U';
  }

  protected toggleUserMenu(): void {
    this.userMenuOpen.update((open) => !open);
  }

  protected openView(view: View): void {
    this.activeView.set(view);
    this.userMenuOpen.set(false);

    if (view === 'income' || view === 'expense') {
      this.transactionForm.patchValue({ type: view === 'income' ? 'Income' : 'Expense' });
    }
  }

  protected currentMonthValue(): string {
    return new Date().toISOString().slice(0, 7);
  }

  protected currentMonthLabel(): string {
    const label = new Intl.DateTimeFormat('pt-BR', { month: 'long', year: 'numeric' }).format(new Date());
    return label[0].toUpperCase() + label.slice(1);
  }

  protected pageTitle(): string {
    const titles: Record<View, string> = {
      dashboard: 'Dashboard',
      income: 'Receitas',
      expense: 'Despesas',
      accounts: 'Contas',
      categories: 'Categorias',
      reports: 'Relatórios',
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
      .get<Transaction[]>(`${this.apiUrl}/transactions?year=2026&month=8`, options)
      .subscribe((transactions) => this.transactions.set(transactions));
    this.http
      .get<Dashboard>(`${this.apiUrl}/dashboard/monthly?year=2026&month=8`, options)
      .subscribe((dashboard) => this.dashboard.set(dashboard));
  }

  protected showAllTransactions(): void {
    this.loadAll();
    this.openView('reports');
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

  private showMessage(text: string): void {
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

  private options(): { headers: HttpHeaders } {
    return {
      headers: new HttpHeaders({ Authorization: `Bearer ${this.token()}` }),
    };
  }
}

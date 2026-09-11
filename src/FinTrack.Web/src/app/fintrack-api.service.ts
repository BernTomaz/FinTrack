import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

export type AccountType = 'Wallet' | 'Checking' | 'Savings' | 'CreditCard';
export type CategoryType = 'Income' | 'Expense';
export type TransactionType = 'Income' | 'Expense';

export interface AuthResponse {
  name: string;
  email: string;
  token: string;
}

export interface Account {
  id: string;
  name: string;
  type: AccountType;
  initialBalance: number;
  openingDate: string;
}

export interface Category {
  id: string;
  name: string;
  type: CategoryType;
}

export interface Transaction {
  id: string;
  accountId: string;
  categoryId: string;
  type: TransactionType;
  amount: number;
  date: string;
  description: string | null;
}

export interface Dashboard {
  totalIncome: number;
  totalExpense: number;
  monthBalance: number;
  currentBalance: number;
  expensesByCategory: { categoryName: string; total: number }[];
}

@Injectable({ providedIn: 'root' })
export class FinTrackApiService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5080';

  login(request: { email: string; password: string }) {
    return this.http.post<AuthResponse>(`${this.apiUrl}/auth/login`, request);
  }

  register(request: { name: string; email: string; password: string }) {
    return this.http.post<AuthResponse>(`${this.apiUrl}/auth/register`, request);
  }

  updateProfile(token: string, request: { name: string }) {
    return this.http.put<AuthResponse>(`${this.apiUrl}/auth/me`, request, this.options(token));
  }

  changePassword(token: string, request: { currentPassword: string; newPassword: string }) {
    return this.http.put(`${this.apiUrl}/auth/password`, request, this.options(token));
  }

  getAccounts(token: string) {
    return this.http.get<Account[]>(`${this.apiUrl}/accounts`, this.options(token));
  }

  createAccount(token: string, request: { name: string; type: AccountType; initialBalance: number; openingDate: string }) {
    return this.http.post<Account>(`${this.apiUrl}/accounts`, request, this.options(token));
  }

  updateAccount(token: string, id: string, request: { name: string; type: AccountType; initialBalance: number; openingDate: string }) {
    return this.http.put<Account>(`${this.apiUrl}/accounts/${id}`, request, this.options(token));
  }

  deleteAccount(token: string, id: string) {
    return this.http.delete(`${this.apiUrl}/accounts/${id}`, this.options(token));
  }

  getCategories(token: string) {
    return this.http.get<Category[]>(`${this.apiUrl}/categories`, this.options(token));
  }

  createCategory(token: string, request: { name: string; type: CategoryType }) {
    return this.http.post<Category>(`${this.apiUrl}/categories`, request, this.options(token));
  }

  updateCategory(token: string, id: string, request: { name: string; type: CategoryType }) {
    return this.http.put<Category>(`${this.apiUrl}/categories/${id}`, request, this.options(token));
  }

  deleteCategory(token: string, id: string) {
    return this.http.delete(`${this.apiUrl}/categories/${id}`, this.options(token));
  }

  getTransactions(token: string, year: number, month: number) {
    return this.http.get<Transaction[]>(`${this.apiUrl}/transactions?year=${year}&month=${month}`, this.options(token));
  }

  createTransaction(token: string, request: Omit<Transaction, 'id'>) {
    return this.http.post<Transaction>(`${this.apiUrl}/transactions`, request, this.options(token));
  }

  updateTransaction(token: string, id: string, request: Omit<Transaction, 'id'>) {
    return this.http.put<Transaction>(`${this.apiUrl}/transactions/${id}`, request, this.options(token));
  }

  deleteTransaction(token: string, id: string) {
    return this.http.delete(`${this.apiUrl}/transactions/${id}`, this.options(token));
  }

  getDashboard(token: string, year: number, month: number) {
    return this.http.get<Dashboard>(`${this.apiUrl}/dashboard/monthly?year=${year}&month=${month}`, this.options(token));
  }

  exportTransactions(token: string, params: URLSearchParams) {
    return this.http.get(`${this.apiUrl}/exports/transactions.csv?${params}`, {
      ...this.options(token),
      responseType: 'blob',
      observe: 'response',
    });
  }

  saveSession(auth: AuthResponse): void {
    sessionStorage.setItem('fintrack.token', auth.token);
    sessionStorage.setItem('fintrack.name', auth.name);
    sessionStorage.setItem('fintrack.email', auth.email);
  }

  clearSession(): void {
    sessionStorage.removeItem('fintrack.token');
    sessionStorage.removeItem('fintrack.name');
    sessionStorage.removeItem('fintrack.email');
  }

  errorMessage(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse && typeof error.error === 'string' && error.error.trim().length > 0) {
      return error.error;
    }

    return fallback;
  }

  private options(token: string): { headers: HttpHeaders } {
    return {
      headers: new HttpHeaders({ Authorization: `Bearer ${token}` }),
    };
  }
}

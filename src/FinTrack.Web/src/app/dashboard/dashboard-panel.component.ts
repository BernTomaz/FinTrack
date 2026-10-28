import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Account, AccountType, Category, CategoryType, Dashboard, Transaction, TransactionType } from '../fintrack-api.service';
import { formatMoney } from '../shared/format-money';

@Component({
  selector: 'app-dashboard-panel',
  imports: [ReactiveFormsModule],
  templateUrl: './dashboard-panel.component.html',
  styleUrl: './dashboard-panel.component.css',
})
export class DashboardPanelComponent {
  @Input() accounts: Account[] = [];
  @Input() categories: Category[] = [];
  @Input() transactions: Transaction[] = [];
  @Input() recentTransactions: Transaction[] = [];
  @Input() dashboard: Dashboard | null = null;
  @Input() accountTypes: AccountType[] = [];
  @Input() categoryTypes: CategoryType[] = [];
  @Input() transactionTypes: TransactionType[] = [];
  @Input({ required: true }) accountForm!: FormGroup;
  @Input({ required: true }) categoryForm!: FormGroup;
  @Input({ required: true }) transactionForm!: FormGroup;
  @Input() month = '';
  @Input() monthLabel = '';
  @Input() hasMoreTransactions = false;
  @Input() editingTransaction = false;
  @Output() monthChange = new EventEmitter<string>();
  @Output() showTransactionsByType = new EventEmitter<TransactionType>();
  @Output() showTransactionsByCategory = new EventEmitter<string>();
  @Output() showAllTransactions = new EventEmitter<void>();
  @Output() showAllAccounts = new EventEmitter<void>();
  @Output() showMoreTransactions = new EventEmitter<void>();
  @Output() addExpense = new EventEmitter<void>();
  @Output() editTransaction = new EventEmitter<Transaction>();
  @Output() duplicateTransaction = new EventEmitter<Transaction>();
  @Output() deleteTransaction = new EventEmitter<string>();
  @Output() deleteAccount = new EventEmitter<string>();
  @Output() createAccount = new EventEmitter<void>();
  @Output() createCategory = new EventEmitter<void>();
  @Output() createTransaction = new EventEmitter<void>();
  @Output() cancelTransactionEdit = new EventEmitter<void>();

  accountName(id: string): string {
    return this.accounts.find((account) => account.id === id)?.name ?? 'Conta';
  }

  categoryName(id: string): string {
    return this.categories.find((category) => category.id === id)?.name ?? 'Categoria';
  }

  accountTypeLabel(type: AccountType): string {
    return {
      Wallet: 'Carteira',
      Checking: 'Conta corrente',
      Savings: 'Poupança',
      CreditCard: 'Cartão de crédito',
    }[type];
  }

  categoryTypeLabel(type: CategoryType): string {
    return type === 'Income' ? 'Receita' : 'Despesa';
  }

  transactionTypeLabel(type: TransactionType): string {
    return type === 'Income' ? 'Receita' : 'Despesa';
  }

  readonly money = formatMoney;

  selectedDay = 1;
  readonly compactMoney = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL', notation: 'compact', maximumFractionDigits: 1 }).format;

  dailyChart() {
    const rows = this.dashboard?.dailyCashFlow;
    const period = /^(\d{4})-(\d{2})$/.exec(this.month);
    if (!rows?.length || !period) return null;
    const year = Number(period[1]);
    const month = Number(period[2]);
    if (year < 1 || year > 9999 || month < 1 || month > 12) return null;
    const dayCount = new Date(year, month, 0).getDate();
    const byDate = new Map(rows.map(row => [row.date, row]));
    let balance = 0;
    const days = Array.from({ length: dayCount }, (_, index) => {
      const day = index + 1;
      const date = this.month + '-' + String(day).padStart(2, '0');
      const row = byDate.get(date);
      if (row) balance = row.balance;
      return { day, date, income: row?.income ?? 0, expense: row?.expense ?? 0, balance };
    });
    const minimum = Math.min(0, ...days.map(day => day.balance)) * 1.1;
    const maximum = Math.max(1, ...days.flatMap(day => [day.income, day.expense, day.balance])) * 1.1;
    const y = (amount: number) => 18 + (maximum - amount) / (maximum - minimum) * 190;
    const zeroY = y(0);
    const points = days.map((day, index) => ({
      ...day, x: 78 + index * 600 / (dayCount - 1),
      incomeY: y(day.income), expenseY: y(day.expense), balanceY: y(day.balance),
      incomeHeight: zeroY - y(day.income), expenseHeight: zeroY - y(day.expense),
      label: day.day === 1 || day.day % 5 === 0 || day.day === dayCount,
    }));
    return {
      points, zeroY,
      selected: points[Math.min(Math.max(this.selectedDay, 1), dayCount) - 1],
      line: points.map(day => day.x + ',' + day.balanceY).join(' '),
      ticks: Array.from({ length: 5 }, (_, index) => {
        const amount = minimum + (maximum - minimum) * index / 4;
        return { y: y(amount), label: this.compactMoney(amount) };
      }),
    };
  }

  dailyTooltip(day: { date: string; income: number; expense: number; balance: number }): string {
    return day.date.split('-').reverse().join('/') + '\nReceitas: ' + this.money(day.income) + '\nDespesas: ' + this.money(day.expense) + '\nSaldo acumulado do mês: ' + this.money(day.balance);
  }

  expenseDonutTooltip(): string {
    const total = this.dashboard?.totalExpense ?? 0;

    return total > 0 ? `Total de despesas do mês: ${this.money(total)}` : 'Sem despesas neste mês';
  }

  expenseDonutSlices(): { categoryName: string; color: string; path: string; tooltip: string; total: number; percentage: string }[] {
    const items = this.dashboard?.expensesByCategory ?? [];
    const total = this.dashboard?.totalExpense ?? 0;
    const colors = ['#2f80ed', '#35b66a', '#f59e0b', '#8b5cf6', '#22b8cf', '#f97316', '#64748b', '#db2777', '#0d9488', '#a16207'];

    if (total <= 0 || items.length === 0) {
      return [];
    }

    let start = 0;
    return items.map((item, index) => {
      const end = start + (item.total / total) * 100;
      const slice = {
        categoryName: item.categoryName,
        total: item.total,
        percentage: item.total / total < 0.001 ? '<0,1%' : ((item.total / total) * 100).toFixed(1).replace('.', ',') + '%',
        color: colors[index % colors.length],
        path: this.donutSlicePath(start, end),
        tooltip: this.categoryExpenseTooltip(item),
      };
      start = end;
      return slice;
    });
  }

  private donutSlicePath(startPercent: number, endPercent: number): string {
    if (endPercent - startPercent >= 100) {
      // Um arco SVG com início e fim iguais não desenha uma circunferência completa.
      return 'M 60 12 A 48 48 0 1 1 60 108 A 48 48 0 1 1 60 12 L 60 35 A 25 25 0 1 0 60 85 A 25 25 0 1 0 60 35 Z';
    }
    const center = 60;
    const outer = 48;
    const inner = 25;
    const start = (startPercent / 100) * Math.PI * 2 - Math.PI / 2;
    const end = (endPercent / 100) * Math.PI * 2 - Math.PI / 2;
    const largeArc = endPercent - startPercent > 50 ? 1 : 0;
    const point = (radius: number, angle: number) => ({
      x: center + radius * Math.cos(angle),
      y: center + radius * Math.sin(angle),
    });
    const outerStart = point(outer, start);
    const outerEnd = point(outer, end);
    const innerStart = point(inner, start);
    const innerEnd = point(inner, end);

    return [
      `M ${outerStart.x} ${outerStart.y}`,
      `A ${outer} ${outer} 0 ${largeArc} 1 ${outerEnd.x} ${outerEnd.y}`,
      `L ${innerEnd.x} ${innerEnd.y}`,
      `A ${inner} ${inner} 0 ${largeArc} 0 ${innerStart.x} ${innerStart.y}`,
      'Z',
    ].join(' ');
  }

  categoryExpenseTooltip(item: { categoryName: string; total: number }): string {
    const total = this.dashboard?.totalExpense ?? 0;
    const percent = total > 0 ? ` (${((item.total / total) * 100).toFixed(1).replace('.', ',')}%)` : '';

    return `${item.categoryName}: ${this.money(item.total)}${percent}\nClique para ver os lançamentos`;
  }
}

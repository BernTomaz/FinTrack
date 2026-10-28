import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Account, AccountType, Category, CategoryType, Dashboard, Transaction, TransactionType } from '../fintrack-api.service';
import { formatMoney } from '../shared/format-money';

@Component({
  selector: 'app-dashboard-panel',
  imports: [ReactiveFormsModule],
  templateUrl: './dashboard-panel.component.html',
})
export class DashboardPanelComponent {
  @Input() accounts: Account[] = [];
  @Input() categories: Category[] = [];
  @Input() transactions: Transaction[] = [];
  @Input() recentTransactions: Transaction[] = [];
  @Input() dashboard: Dashboard | null = null;
  @Input() chartMonths: { label: string; income: number; expense: number; incomeAmount: number; expenseAmount: number }[] = [];
  @Input() accountTypes: AccountType[] = [];
  @Input() categoryTypes: CategoryType[] = [];
  @Input() transactionTypes: TransactionType[] = [];
  @Input({ required: true }) accountForm!: FormGroup;
  @Input({ required: true }) categoryForm!: FormGroup;
  @Input({ required: true }) transactionForm!: FormGroup;
  @Input() month = '';
  @Input() monthLabel = '';
  @Input() chartRangeLabel = '';
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

  cashFlowTooltip(month: { label: string; incomeAmount: number; expenseAmount: number }): string {
    return `${month.label}\nReceitas: ${this.money(month.incomeAmount)}\nDespesas: ${this.money(month.expenseAmount)}\nSaldo: ${this.money(month.incomeAmount - month.expenseAmount)}`;
  }

  expenseDonutTooltip(): string {
    const total = this.dashboard?.totalExpense ?? 0;

    return total > 0 ? `Total de despesas do mês: ${this.money(total)}` : 'Sem despesas neste mês';
  }

  expenseDonutSlices(): { categoryName: string; color: string; path: string; tooltip: string }[] {
    const items = this.dashboard?.expensesByCategory ?? [];
    const total = this.dashboard?.totalExpense ?? 0;
    const colors = ['#2f80ed', '#35b66a', '#f59e0b', '#8b5cf6', '#22b8cf', '#f97316', '#98a2b3'];

    if (total <= 0 || items.length === 0) {
      return [];
    }

    let start = 0;
    return items.map((item, index) => {
      const end = start + (item.total / total) * 100;
      const slice = {
        categoryName: item.categoryName,
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

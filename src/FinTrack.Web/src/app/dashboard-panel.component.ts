import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Account, AccountType, Category, CategoryType, Dashboard, Transaction, TransactionType } from './fintrack-api.service';

@Component({
  selector: 'app-dashboard-panel',
  imports: [ReactiveFormsModule],
  template: `
    <section class="toolbar">
      <input class="select-like" type="month" [value]="month" (change)="monthChange.emit($any($event.target).value)" title="Selecionar mês" />
    </section>

    <section id="dashboard" class="summary" aria-label="Resumo mensal">
      <button type="button" class="metric income" style="text-align:left" (click)="showTransactionsByType.emit('Income')" title="Ver receitas do mês">
        <span>↗</span>
        <div>
          <p>Receitas</p>
          <strong>{{ money(dashboard?.totalIncome ?? 0) }}</strong>
          <small>{{ monthLabel }}</small>
        </div>
      </button>
      <button type="button" class="metric expense" style="text-align:left" (click)="showTransactionsByType.emit('Expense')" title="Ver despesas do mês">
        <span>↘</span>
        <div>
          <p>Despesas</p>
          <strong>{{ money(dashboard?.totalExpense ?? 0) }}</strong>
          <small>{{ monthLabel }}</small>
        </div>
      </button>
      <button type="button" class="metric balance" style="text-align:left" (click)="showAllTransactions.emit()" title="Ver lançamentos do mês">
        <span>▣</span>
        <div>
          <p>Saldo</p>
          <strong>{{ money(dashboard?.monthBalance ?? 0) }}</strong>
          <small>{{ monthLabel }}</small>
        </div>
      </button>
      <button type="button" class="metric bills" style="text-align:left" (click)="showAllAccounts.emit()" title="Ver contas">
        <span>▤</span>
        <div>
          <p>Saldo atual</p>
          <strong>{{ money(dashboard?.currentBalance ?? 0) }}</strong>
          <small>Contas e lançamentos atualizados</small>
        </div>
      </button>
    </section>

    <section class="grid">
      <article class="panel chart-panel">
        <div class="panel-head">
          <h2>Fluxo de caixa mensal</h2>
          <button type="button" class="select-like">{{ chartRangeLabel }}</button>
        </div>
        @if (transactions.length > 0) {
          <div class="bar-chart" aria-label="Gráfico de fluxo de caixa mensal">
            @for (month of chartMonths; track month.label) {
              <div class="bar-group" [title]="cashFlowTooltip(month)">
                <span class="income-bar" [style.height.%]="month.income"></span>
                <span class="expense-bar" [style.height.%]="month.expense"></span>
                <small>{{ month.label }}</small>
              </div>
            }
          </div>
        } @else {
          <div class="empty-state">
            <strong>Nenhum lançamento cadastrado</strong>
            <span>Cadastre receitas e despesas para visualizar o fluxo de caixa.</span>
            <button type="button" class="primary" style="justify-self:center" (click)="addExpense.emit()">Adicionar lançamento</button>
          </div>
        }
      </article>

      @if ((dashboard?.expensesByCategory?.length ?? 0) > 0) {
        <article id="categorias" class="panel">
          <div class="panel-head">
            <h2>Despesas por categoria</h2>
            <button type="button" class="select-like">{{ monthLabel }}</button>
          </div>
          <div class="category-card">
            <div class="donut" [title]="expenseDonutTooltip()"><span>Total<br />{{ money(dashboard?.totalExpense ?? 0) }}</span></div>
            <div class="list compact">
              @for (item of dashboard?.expensesByCategory ?? []; track item.categoryName) {
                <button type="button" class="line" style="background:transparent;color:inherit;text-align:left;width:100%" (click)="showTransactionsByCategory.emit(item.categoryName)" [title]="categoryExpenseTooltip(item)">
                  <span>{{ item.categoryName }}</span>
                  <strong>{{ money(item.total) }}</strong>
                </button>
              }
            </div>
          </div>
        </article>
      } @else {
        <article id="categorias" class="panel">
          <div class="panel-head">
            <h2>Despesas por categoria</h2>
          </div>
          <div class="empty-state">
            <strong>Sem despesas por enquanto</strong>
            <span>As categorias aparecem aqui depois dos primeiros lançamentos.</span>
            <button type="button" class="primary" style="justify-self:center" (click)="addExpense.emit()">Adicionar despesa</button>
          </div>
        </article>
      }

      <article id="relatorios" class="panel table-panel">
        <div class="panel-head">
          <h2>Transações recentes</h2>
          <button type="button" class="link strong" (click)="showAllTransactions.emit()">Ver todas →</button>
        </div>
        <div class="table">
          <div class="table-row table-head">
            <span>Data</span>
            <span>Descrição</span>
            <span>Categoria</span>
            <span>Tipo</span>
            <span>Valor</span>
            <span>Conta</span>
            <span>Ações</span>
          </div>
          @for (transaction of recentTransactions; track transaction.id) {
            <div class="table-row">
              <span>{{ transaction.date }}</span>
              <span>{{ transaction.description || 'Lançamento' }}</span>
              <span>{{ categoryName(transaction.categoryId) }}</span>
              <span [class.positive]="transaction.type === 'Income'" [class.negative]="transaction.type === 'Expense'">
                {{ transactionTypeLabel(transaction.type) }}
              </span>
              <strong [class.positive]="transaction.type === 'Income'" [class.negative]="transaction.type === 'Expense'">
                {{ money(transaction.amount) }}
              </strong>
              <span>{{ accountName(transaction.accountId) }}</span>
              <span style="display:flex;gap:8px">
                <button type="button" class="icon" title="Editar lançamento" (click)="editTransaction.emit(transaction)">✎</button>
                <button type="button" class="icon danger-icon" title="Excluir lançamento" (click)="deleteTransaction.emit(transaction.id)">×</button>
              </span>
            </div>
          } @empty {
            <div class="empty-state">
              <strong>Nenhum lançamento ainda</strong>
              <span>Comece adicionando uma receita ou despesa.</span>
              <button type="button" class="primary" style="justify-self:center" (click)="addExpense.emit()">Adicionar lançamento</button>
            </div>
          }
        </div>
        @if (hasMoreTransactions) {
          <button type="button" class="ghost" style="justify-self:center" (click)="showMoreTransactions.emit()">Ver mais</button>
        }
      </article>

      <article id="contas" class="panel accounts-panel">
        <div class="panel-head">
          <h2>Saldos das contas</h2>
          <button type="button" class="link strong" (click)="showAllAccounts.emit()">Ver todas →</button>
        </div>
        <div class="list">
          @for (account of accounts; track account.id) {
            <div class="account-line">
              <span class="coin">▣</span>
              <div>
                <strong>{{ account.name }}</strong>
                <small>{{ accountTypeLabel(account.type) }}</small>
              </div>
              <button type="button" class="icon" title="Excluir conta" (click)="deleteAccount.emit(account.id)">×</button>
            </div>
          } @empty {
            <div class="empty-state">
              <strong>Nenhuma conta cadastrada</strong>
              <span>Cadastre uma conta para começar a organizar seus lançamentos.</span>
              <button type="button" class="primary" style="justify-self:center" (click)="showAllAccounts.emit()">Criar primeira conta</button>
            </div>
          }
        </div>
      </article>
    </section>

    <section class="forms-grid">
      <form id="contas" class="panel" [formGroup]="accountForm" (ngSubmit)="createAccount.emit()">
        <h2>Nova conta</h2>
        <label>Nome<input type="text" formControlName="name" /></label>
        <label>Tipo
          <select formControlName="type">
            @for (type of accountTypes; track type) {
              <option [value]="type">{{ accountTypeLabel(type) }}</option>
            }
          </select>
        </label>
        <label>Saldo inicial<input type="number" formControlName="initialBalance" /></label>
        <button type="submit" class="primary">Salvar conta</button>
      </form>

      <form class="panel" [formGroup]="categoryForm" (ngSubmit)="createCategory.emit()">
        <h2>Nova categoria</h2>
        <label>Nome<input type="text" formControlName="name" /></label>
        <label>Tipo
          <select formControlName="type">
            @for (type of categoryTypes; track type) {
              <option [value]="type">{{ categoryTypeLabel(type) }}</option>
            }
          </select>
        </label>
        <button type="submit" class="primary">Salvar categoria</button>
      </form>

      <form id="receitas" class="panel transaction-form" [formGroup]="transactionForm" (ngSubmit)="createTransaction.emit()">
        <h2>{{ editingTransaction ? 'Editar lançamento' : 'Novo lançamento' }}</h2>
        <div class="form-grid">
          <label>Conta
            <select formControlName="accountId">
              <option value="">Selecione</option>
              @for (account of accounts; track account.id) {
                <option [value]="account.id">{{ account.name }}</option>
              }
            </select>
          </label>
          <label>Categoria
            <select formControlName="categoryId">
              <option value="">Selecione</option>
              @for (category of categories; track category.id) {
                <option [value]="category.id">{{ category.name }}</option>
              }
            </select>
          </label>
          <label>Tipo
            <select formControlName="type">
              @for (type of transactionTypes; track type) {
                <option [value]="type">{{ transactionTypeLabel(type) }}</option>
              }
            </select>
          </label>
          <label>Valor<input type="number" formControlName="amount" min="0.01" step="0.01" /></label>
          <label>Data<input type="date" formControlName="date" /></label>
          <label>Descrição<input type="text" formControlName="description" /></label>
        </div>
        <div style="display:flex;gap:10px;flex-wrap:wrap">
          <button type="submit" class="primary">{{ editingTransaction ? 'Atualizar lançamento' : 'Salvar lançamento' }}</button>
          @if (editingTransaction) {
            <button type="button" class="ghost" (click)="cancelTransactionEdit.emit()">Cancelar edição</button>
          }
        </div>
      </form>
    </section>
  `,
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

  money(value: number): string {
    return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);
  }

  cashFlowTooltip(month: { label: string; incomeAmount: number; expenseAmount: number }): string {
    return `${month.label}\nReceitas: ${this.money(month.incomeAmount)}\nDespesas: ${this.money(month.expenseAmount)}\nSaldo: ${this.money(month.incomeAmount - month.expenseAmount)}`;
  }

  expenseDonutTooltip(): string {
    const total = this.dashboard?.totalExpense ?? 0;

    return total > 0 ? `Total de despesas do mês: ${this.money(total)}` : 'Sem despesas neste mês';
  }

  categoryExpenseTooltip(item: { categoryName: string; total: number }): string {
    const total = this.dashboard?.totalExpense ?? 0;
    const percent = total > 0 ? ` (${((item.total / total) * 100).toFixed(1).replace('.', ',')}%)` : '';

    return `${item.categoryName}: ${this.money(item.total)}${percent}\nClique para ver os lançamentos`;
  }
}

import { Component, EventEmitter, Input, Output } from '@angular/core';
import { Account, Category, Transaction, TransactionType } from './fintrack-api.service';

type TransactionSortField = 'date' | 'description' | 'category' | 'type' | 'amount';

@Component({
  selector: 'app-reports-panel',
  template: `
    <section class="settings-page" style="place-items:start center">
      <article class="panel settings-card" style="max-width:min(1180px, calc(100vw - 64px))">
        <div class="panel-head">
          <h2>Relatórios</h2>
          <button type="button" class="ghost" (click)="back.emit()">Voltar</button>
        </div>
        <div class="form-grid">
          <label>Mês<input type="month" [value]="month" (change)="monthChange.emit($any($event.target).value)" /></label>
          <label>Buscar<input type="search" [value]="search" (input)="searchChange.emit($any($event.target).value)" placeholder="Descrição, conta ou categoria" /></label>
          <label>Tipo
            <select [value]="typeFilter" (change)="typeFilterChange.emit($any($event.target).value)">
              <option value="">Todos</option>
              @for (type of transactionTypes; track type) {
                <option [value]="type">{{ transactionTypeLabel(type) }}</option>
              }
            </select>
          </label>
          <label>Categoria
            <select [value]="categoryFilter" (change)="categoryFilterChange.emit($any($event.target).value)">
              <option value="">Todas</option>
              @for (category of categories; track category.id) {
                <option [value]="category.id">{{ category.name }}</option>
              }
            </select>
          </label>
          <label>Conta
            <select [value]="accountFilter" (change)="accountFilterChange.emit($any($event.target).value)">
              <option value="">Todas</option>
              @for (account of accounts; track account.id) {
                <option [value]="account.id">{{ account.name }}</option>
              }
            </select>
          </label>
        </div>
        <div class="table">
          <div class="table-row table-head">
            <button type="button" style="background:transparent;color:inherit;text-align:left" (click)="sort.emit('date')">Data{{ sortLabel('date') }}</button>
            <button type="button" style="background:transparent;color:inherit;text-align:left" (click)="sort.emit('description')">Descrição{{ sortLabel('description') }}</button>
            <button type="button" style="background:transparent;color:inherit;text-align:left" (click)="sort.emit('category')">Categoria{{ sortLabel('category') }}</button>
            <button type="button" style="background:transparent;color:inherit;text-align:left" (click)="sort.emit('type')">Tipo{{ sortLabel('type') }}</button>
            <button type="button" style="background:transparent;color:inherit;text-align:left" (click)="sort.emit('amount')">Valor{{ sortLabel('amount') }}</button>
            <span>Conta</span>
            <span>Ações</span>
          </div>
          @for (transaction of transactions; track transaction.id) {
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
                <button type="button" class="icon" title="Editar lançamento" (click)="edit.emit(transaction)">✎</button>
                <button type="button" class="icon danger-icon" title="Excluir lançamento" (click)="delete.emit(transaction.id)">×</button>
              </span>
            </div>
          } @empty {
            <div class="empty-state">
              <strong>Nenhum lançamento encontrado</strong>
              <span>Ajuste os filtros ou cadastre receitas e despesas para acompanhar seus relatórios.</span>
              <button type="button" class="primary" style="justify-self:center" (click)="addTransaction.emit()">Adicionar lançamento</button>
            </div>
          }
        </div>
        @if (hasMore) {
          <button type="button" class="ghost" style="justify-self:center" (click)="showMore.emit()">Ver mais</button>
        }
      </article>
    </section>
  `,
})
export class ReportsPanelComponent {
  @Input() accounts: Account[] = [];
  @Input() categories: Category[] = [];
  @Input() transactions: Transaction[] = [];
  @Input() transactionTypes: TransactionType[] = [];
  @Input() month = '';
  @Input() search = '';
  @Input() typeFilter = '';
  @Input() categoryFilter = '';
  @Input() accountFilter = '';
  @Input() sortField: TransactionSortField = 'date';
  @Input() sortDirection: 'asc' | 'desc' = 'desc';
  @Input() hasMore = false;
  @Output() monthChange = new EventEmitter<string>();
  @Output() searchChange = new EventEmitter<string>();
  @Output() typeFilterChange = new EventEmitter<string>();
  @Output() categoryFilterChange = new EventEmitter<string>();
  @Output() accountFilterChange = new EventEmitter<string>();
  @Output() sort = new EventEmitter<TransactionSortField>();
  @Output() edit = new EventEmitter<Transaction>();
  @Output() delete = new EventEmitter<string>();
  @Output() showMore = new EventEmitter<void>();
  @Output() addTransaction = new EventEmitter<void>();
  @Output() back = new EventEmitter<void>();

  accountName(id: string): string {
    return this.accounts.find((account) => account.id === id)?.name ?? 'Conta';
  }

  categoryName(id: string): string {
    return this.categories.find((category) => category.id === id)?.name ?? 'Categoria';
  }

  transactionTypeLabel(type: TransactionType): string {
    return type === 'Income' ? 'Receita' : 'Despesa';
  }

  money(value: number): string {
    return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);
  }

  sortLabel(field: TransactionSortField): string {
    return this.sortField === field ? (this.sortDirection === 'asc' ? ' ↑' : ' ↓') : '';
  }
}

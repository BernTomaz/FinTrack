import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Account, AccountType } from './fintrack-api.service';

@Component({
  selector: 'app-accounts-panel',
  imports: [ReactiveFormsModule],
  template: `
    <section class="settings-page">
      <div class="panel settings-card">
        <div class="panel-head">
          <h2>Contas</h2>
          <button type="button" class="ghost" (click)="back.emit()">Voltar</button>
        </div>
        <div class="list">
          @for (account of accounts; track account.id) {
            <div class="account-line">
              <span class="coin">▣</span>
              <div>
                <strong>{{ account.name }}</strong>
                <small>{{ accountTypeLabel(account.type) }}</small>
                <small>Saldo inicial: {{ money(account.initialBalance) }}</small>
                <small>Inicio: {{ account.openingDate }}</small>
              </div>
              <button type="button" class="icon" title="Excluir conta" (click)="delete.emit(account.id)">×</button>
            </div>
          } @empty {
            <div class="empty-state">
              <strong>Nenhuma conta cadastrada</strong>
              <span>Cadastre uma conta para começar a organizar seus lançamentos.</span>
            </div>
          }
        </div>
      </div>

      <form class="panel settings-card" [formGroup]="form" (ngSubmit)="save.emit()">
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
        <label>Inicio da conta<input type="date" formControlName="openingDate" /></label>
        <button type="submit" class="primary">Salvar conta</button>
      </form>
    </section>
  `,
})
export class AccountsPanelComponent {
  @Input() accounts: Account[] = [];
  @Input() accountTypes: AccountType[] = [];
  @Input({ required: true }) form!: FormGroup;
  @Output() save = new EventEmitter<void>();
  @Output() delete = new EventEmitter<string>();
  @Output() back = new EventEmitter<void>();

  accountTypeLabel(type: AccountType): string {
    return {
      Wallet: 'Carteira',
      Checking: 'Conta corrente',
      Savings: 'Poupança',
      CreditCard: 'Cartão de crédito',
    }[type];
  }

  money(value: number): string {
    return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);
  }
}

import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Account, Category } from './fintrack-api.service';

@Component({
  selector: 'app-transaction-form-panel',
  imports: [ReactiveFormsModule],
  template: `
    <section class="settings-page">
      <form class="panel settings-card" [formGroup]="form" (ngSubmit)="save.emit()">
        <div class="panel-head">
          <h2>{{ title }}</h2>
          <button type="button" class="ghost" (click)="back.emit()">Voltar</button>
        </div>
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
        <label>Valor<input type="number" formControlName="amount" min="0.01" step="0.01" /></label>
        <label>Data<input type="date" formControlName="date" /></label>
        <label>Descrição<input type="text" formControlName="description" /></label>
        <div style="display:flex;gap:10px;flex-wrap:wrap">
          <button type="submit" class="primary">{{ editing ? 'Atualizar lançamento' : 'Salvar lançamento' }}</button>
          @if (editing) {
            <button type="button" class="ghost" (click)="cancelEdit.emit()">Cancelar edição</button>
          }
        </div>
      </form>
    </section>
  `,
})
export class TransactionFormPanelComponent {
  @Input() accounts: Account[] = [];
  @Input() categories: Category[] = [];
  @Input({ required: true }) form!: FormGroup;
  @Input() title = 'Novo lançamento';
  @Input() editing = false;
  @Output() save = new EventEmitter<void>();
  @Output() cancelEdit = new EventEmitter<void>();
  @Output() back = new EventEmitter<void>();
}

import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Category, CategoryType } from './fintrack-api.service';

@Component({
  selector: 'app-categories-panel',
  imports: [ReactiveFormsModule],
  template: `
    <section class="settings-page">
      <div class="panel settings-card">
        <div class="panel-head">
          <h2>Categorias</h2>
          <button type="button" class="ghost" (click)="back.emit()">Voltar</button>
        </div>
        <div class="list">
          @for (category of categories; track category.id) {
            <div class="account-line">
              <span class="coin">◇</span>
              <div>
                <strong>{{ category.name }}</strong>
                <small>{{ categoryTypeLabel(category.type) }}</small>
              </div>
              <button type="button" class="icon" title="Excluir categoria" (click)="delete.emit(category.id)">×</button>
            </div>
          } @empty {
            <div class="empty-state">
              <strong>Nenhuma categoria cadastrada</strong>
              <span>Cadastre categorias para organizar receitas e despesas.</span>
            </div>
          }
        </div>
      </div>

      <form class="panel settings-card" [formGroup]="form" (ngSubmit)="save.emit()">
        <div class="panel-head">
          <h2>Nova categoria</h2>
        </div>
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
    </section>
  `,
})
export class CategoriesPanelComponent {
  @Input() categories: Category[] = [];
  @Input() categoryTypes: CategoryType[] = [];
  @Input({ required: true }) form!: FormGroup;
  @Output() save = new EventEmitter<void>();
  @Output() delete = new EventEmitter<string>();
  @Output() back = new EventEmitter<void>();

  categoryTypeLabel(type: CategoryType): string {
    return type === 'Income' ? 'Receita' : 'Despesa';
  }
}

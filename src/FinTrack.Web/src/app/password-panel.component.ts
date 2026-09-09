import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-password-panel',
  imports: [ReactiveFormsModule],
  template: `
    <section class="settings-page">
      <form class="panel settings-card" style="max-width:920px" [formGroup]="form" (ngSubmit)="save.emit()">
        <div class="panel-head">
          <h2>Alterar senha</h2>
          <button type="button" class="ghost" (click)="back.emit()">Voltar</button>
        </div>
        <label>Senha atual<input type="password" formControlName="currentPassword" autocomplete="current-password" placeholder="Informe sua senha atual" /></label>
        <label>Nova senha<input type="password" formControlName="newPassword" autocomplete="new-password" placeholder="Informe a nova senha" /></label>
        <label>Confirmar nova senha<input type="password" formControlName="confirmPassword" autocomplete="new-password" placeholder="Repita a nova senha" /></label>
        <button type="submit" class="primary">Salvar alteração</button>
      </form>
    </section>
  `,
})
export class PasswordPanelComponent {
  @Input({ required: true }) form!: FormGroup;
  @Output() save = new EventEmitter<void>();
  @Output() back = new EventEmitter<void>();
}

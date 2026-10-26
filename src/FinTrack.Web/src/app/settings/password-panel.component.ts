import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { passwordStrength, passwordStrengthLabel, passwordStrengthColor, passwordRequirementColor, passwordRequirementIcon, isStrongPassword } from '../shared/password-rules';

@Component({
  selector: 'app-password-panel',
  imports: [ReactiveFormsModule],
  template: `
    <section class="settings-page">
      <form class="panel settings-card" style="max-width:620px" [formGroup]="form" (ngSubmit)="save.emit()">
        <div class="panel-head">
          <h2>Alterar senha</h2>
          <button type="button" class="ghost" (click)="back.emit()">Voltar</button>
        </div>
        <label>Senha atual<input type="password" formControlName="currentPassword" autocomplete="current-password" placeholder="Informe sua senha atual" /></label>
        <label>
          Nova senha
          <input type="password" formControlName="newPassword" autocomplete="new-password" placeholder="Crie sua nova senha" />
          @if (newPassword()) {
            <span style="display:grid;gap:8px">
              <span style="background:#e5e7eb;border-radius:999px;height:7px;overflow:hidden">
                <i style="display:block;height:100%;transition:width 160ms ease" [style.background]="passwordStrengthColor(newPassword())" [style.width.%]="passwordStrength(newPassword()) * 20"></i>
              </span>
              <small style="font-size:0.82rem;font-weight:800;letter-spacing:0" [style.color]="passwordStrengthColor(newPassword())">
                Força da senha: {{ passwordStrengthLabel(newPassword()) }}
              </small>
              <span style="display:grid;font-size:0.78rem;font-weight:700;gap:4px;grid-template-columns:repeat(2,minmax(0,1fr));line-height:1.25">
                <small [style.color]="passwordRequirementColor(newPassword(), 'length')">{{ passwordRequirementIcon(newPassword(), 'length') }} 8+ caracteres</small>
                <small [style.color]="passwordRequirementColor(newPassword(), 'upper')">{{ passwordRequirementIcon(newPassword(), 'upper') }} Letra maiúscula</small>
                <small [style.color]="passwordRequirementColor(newPassword(), 'lower')">{{ passwordRequirementIcon(newPassword(), 'lower') }} Letra minúscula</small>
                <small [style.color]="passwordRequirementColor(newPassword(), 'numberSymbol')">{{ passwordRequirementIcon(newPassword(), 'numberSymbol') }} Número e símbolo</small>
              </span>
            </span>
          }
        </label>
        <label>Confirmar nova senha<input type="password" formControlName="confirmPassword" autocomplete="new-password" placeholder="Repita a nova senha" /></label>
        <button type="submit" class="primary" [disabled]="!isStrongPassword(newPassword())" [style.opacity]="isStrongPassword(newPassword()) ? 1 : 0.38">Salvar alteração</button>
      </form>
    </section>
  `,
})
export class PasswordPanelComponent {
  protected readonly passwordStrength = passwordStrength;
  protected readonly passwordStrengthLabel = passwordStrengthLabel;
  protected readonly passwordStrengthColor = passwordStrengthColor;
  protected readonly passwordRequirementColor = passwordRequirementColor;
  protected readonly passwordRequirementIcon = passwordRequirementIcon;
  protected readonly isStrongPassword = isStrongPassword;

  @Input({ required: true }) form!: FormGroup;
  @Output() save = new EventEmitter<void>();
  @Output() back = new EventEmitter<void>();

  protected newPassword(): string {
    return String(this.form.get('newPassword')?.value ?? '');
  }
}

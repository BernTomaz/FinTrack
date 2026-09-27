import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';

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
  @Input({ required: true }) form!: FormGroup;
  @Output() save = new EventEmitter<void>();
  @Output() back = new EventEmitter<void>();

  protected newPassword(): string {
    return String(this.form.get('newPassword')?.value ?? '');
  }

  protected passwordStrength(password: string): number {
    return this.passwordRequirements(password).filter(Boolean).length;
  }

  protected passwordStrengthLabel(password: string): string {
    const strength = this.passwordStrength(password);

    if (strength < 3) {
      return 'fraca';
    }

    if (strength < 5) {
      return 'média';
    }

    return 'forte';
  }

  protected passwordStrengthColor(password: string): string {
    const strength = this.passwordStrength(password);

    if (strength < 3) {
      return '#dc2626';
    }

    if (strength < 5) {
      return '#d97706';
    }

    return '#059669';
  }

  protected passwordRequirementColor(password: string, requirement: PasswordRequirement): string {
    return this.passwordRequirementMet(password, requirement) ? '#059669' : '#dc2626';
  }

  protected passwordRequirementIcon(password: string, requirement: PasswordRequirement): string {
    return this.passwordRequirementMet(password, requirement) ? '✓' : '•';
  }

  protected isStrongPassword(password: string): boolean {
    return password.length <= 100 && this.passwordStrength(password) === 5;
  }

  private passwordRequirements(password: string): boolean[] {
    return [
      password.length >= 8,
      /[A-Z]/.test(password),
      /[a-z]/.test(password),
      /\d/.test(password),
      /[^A-Za-z0-9]/.test(password),
    ];
  }

  private passwordRequirementMet(password: string, requirement: PasswordRequirement): boolean {
    switch (requirement) {
      case 'length':
        return password.length >= 8;
      case 'upper':
        return /[A-Z]/.test(password);
      case 'lower':
        return /[a-z]/.test(password);
      case 'numberSymbol':
        return /\d/.test(password) && /[^A-Za-z0-9]/.test(password);
    }
  }
}

type PasswordRequirement = 'length' | 'upper' | 'lower' | 'numberSymbol';

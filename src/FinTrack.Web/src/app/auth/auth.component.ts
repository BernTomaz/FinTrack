import { Component, EventEmitter, Input, Output, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { AuthResponse, FinTrackApiService } from '../fintrack-api.service';
import { passwordStrength, passwordStrengthLabel, passwordStrengthColor, passwordRequirementColor, passwordRequirementIcon, isStrongPassword } from '../shared/password-rules';
type AuthMode = 'login' | 'register';

@Component({
  selector: 'app-auth',
  imports: [ReactiveFormsModule],
  templateUrl: './auth.component.html',
})
export class AuthComponent {
  @Input()
  customCursorEnabled = false;
  @Output()
  authenticated = new EventEmitter<AuthResponse>();
  @Output()
  notification = new EventEmitter<{
    text: string;
    kind: 'success' | 'error' | 'info';
  }>();
  @Output()
  clearMessage = new EventEmitter<void>();
  private readonly api = inject(FinTrackApiService);
  private readonly fb = inject(FormBuilder);
  protected readonly isStrongPassword = isStrongPassword;
  protected readonly passwordStrength = passwordStrength;
  protected readonly passwordStrengthLabel = passwordStrengthLabel;
  protected readonly passwordStrengthColor = passwordStrengthColor;
  protected readonly passwordRequirementColor = passwordRequirementColor;
  protected readonly passwordRequirementIcon = passwordRequirementIcon;
  protected readonly loginError = signal('');
  protected readonly loginAttemptsRemaining = signal(3);
  protected readonly loginPasswordVisible = signal(false);
  protected readonly registerPasswordVisible = signal(false);
  protected readonly authPending = signal(false);
  protected readonly authMode = signal<AuthMode>('login');
  protected readonly loginForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(120)]],
    password: ['', [Validators.required, Validators.maxLength(100)]],
  });
  protected readonly registerForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(80)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(120)]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
  });
  protected login(): void {
    if (this.authPending())
      return;
    this.loginError.set('');
    const validation = this.loginValidationMessage();
    if (validation) {
      this.loginForm.markAllAsTouched();
      this.loginError.set(validation);
      this.showMessage(validation, 'error');
      return;
    }
    this.authPending.set(true);
    this.api.login(this.loginForm.getRawValue()).pipe(finalize(() => this.authPending.set(false))).subscribe({
      next: (auth) => this.authenticated.emit(auth),
      error: (error) => {
        const attempts = Math.max(this.loginAttemptsRemaining() - 1, 0);
        const text = this.api.errorMessage(error, `E-mail ou senha incorretos. Tentativas restantes: ${attempts}.`);
        this.loginAttemptsRemaining.set(attempts);
        this.loginError.set(text);
        this.showMessage(text, 'error');
      },
    });
  }
  protected register(): void {
    if (this.authPending())
      return;
    const validation = this.registerValidationMessage();
    if (validation) {
      this.registerForm.markAllAsTouched();
      this.showMessage(validation, 'error');
      return;
    }
    this.authPending.set(true);
    this.api.register(this.registerForm.getRawValue()).pipe(finalize(() => this.authPending.set(false))).subscribe({
      next: () => {
        const email = this.registerForm.controls.email.value.trim();
        this.registerForm.reset({ name: '', email: '', password: '' });
        this.loginForm.patchValue({ email, password: '' });
        this.authMode.set('login');
        this.loginError.set('');
        this.showMessage('Usuário criado com sucesso. Entre com seu e-mail e senha.', 'success');
      },
      error: (error) => this.showMessage(this.api.errorMessage(error, 'Não foi possível criar a conta.'), 'error'),
    });
  }
  protected toggleLoginPassword(): void {
    this.loginPasswordVisible.update((visible) => !visible);
  }
  protected toggleRegisterPassword(): void {
    this.registerPasswordVisible.update((visible) => !visible);
  }
  protected canCreateAccount(): boolean {
    return this.isStrongPassword(this.registerForm.controls.password.value);
  }
  protected showLogin(): void {
    this.authMode.set('login');
    this.loginError.set('');
    this.clearMessage.emit();
  }
  protected showRegister(): void {
    this.authMode.set('register');
    this.clearMessage.emit();
  }
  private loginValidationMessage(): string | null {
    const email = this.loginForm.controls.email.value.trim();
    const password = this.loginForm.controls.password.value;
    if (!email) {
      return 'Informe o e-mail.';
    }
    if (this.loginForm.controls.email.invalid) {
      return 'Informe um e-mail válido.';
    }
    if (!password) {
      return 'Informe a senha.';
    }
    return null;
  }
  private registerValidationMessage(): string | null {
    const name = this.registerForm.controls.name.value.trim();
    const email = this.registerForm.controls.email.value.trim();
    const password = this.registerForm.controls.password.value;
    if (name.length < 2) {
      return 'Informe um nome com pelo menos 2 caracteres.';
    }
    if (name.length > 80) {
      return 'O nome deve ter no máximo 80 caracteres.';
    }
    if (!email) {
      return 'Informe o e-mail.';
    }
    if (this.registerForm.controls.email.invalid) {
      return 'Informe um e-mail válido.';
    }
    if (!this.isStrongPassword(password)) {
      return 'A senha deve ter de 8 a 100 caracteres, com letra maiúscula, letra minúscula, número e caractere especial.';
    }
    return null;
  }
  private showMessage(text: string, kind: 'success' | 'error' | 'info'): void {
    this.notification.emit({ text, kind });
  }
}

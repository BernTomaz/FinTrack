import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-profile-panel',
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <section class="settings-page">
      <form class="panel settings-card" [formGroup]="form" (ngSubmit)="save.emit()">
        <div class="panel-head">
          <h2>Meu perfil</h2>
          <button type="button" class="ghost" (click)="back.emit()">Voltar</button>
        </div>
        <div class="profile-box">
          <span class="avatar large">{{ initial }}</span>
          <div>
            <strong>{{ name }}</strong>
            <small>Usuário do FinTrack</small>
          </div>
        </div>
        <label>Nome<input type="text" formControlName="name" /></label>
        <label>E-mail<input type="email" [value]="email" readonly /></label>
        <button type="submit" class="primary">Salvar perfil</button>
      </form>
    </section>
  `,
})
export class ProfilePanelComponent {
  @Input({ required: true }) form!: FormGroup;
  @Input() initial = 'U';
  @Input() name = '';
  @Input() email = '';
  @Output() save = new EventEmitter<void>();
  @Output() back = new EventEmitter<void>();
}

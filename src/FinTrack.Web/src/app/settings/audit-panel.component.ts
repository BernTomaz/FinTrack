import { DatePipe } from '@angular/common';
import { Component, EventEmitter, Input, OnDestroy, OnInit, Output, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { AuditDetail, AuditEvent, AuditPage, FinTrackApiService } from '../fintrack-api.service';
import { formatMoney } from '../shared/format-money';

@Component({
  selector: 'app-audit-panel',
  imports: [DatePipe, ReactiveFormsModule],
  templateUrl: './audit-panel.component.html',
  styleUrl: './audit-panel.component.css',
})
export class AuditPanelComponent implements OnInit, OnDestroy {
  @Input({ required: true }) token!: string;
  @Output() back = new EventEmitter<void>();
  private readonly api = inject(FinTrackApiService);
  private listRequest?: Subscription;
  private detailRequest?: Subscription;
  protected readonly filters = inject(FormBuilder).nonNullable.group({ entityType: '', action: '', from: '', to: '' });
  protected readonly data = signal<AuditPage | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal('');
  protected readonly detail = signal<AuditDetail | null>(null);
  protected readonly selectedId = signal('');
  protected readonly detailLoading = signal(false);
  protected readonly actions: Record<string, string> = {
    Created: 'Criação', Updated: 'Edição', Deleted: 'Exclusão',
    PasswordChanged: 'Troca de senha', AdminPasswordReset: 'Redefinição por administrador',
  };
  protected readonly entities: Record<string, string> = { Account: 'Conta', Category: 'Categoria', Transaction: 'Lançamento', User: 'Segurança da conta' };
  protected readonly actionKeys = Object.keys(this.actions);
  protected readonly entityKeys = Object.keys(this.entities);

  ngOnInit(): void { this.load(); }

  protected load(page = 1): void {
    this.listRequest?.unsubscribe();
    this.detailRequest?.unsubscribe();
    this.detail.set(null);
    this.selectedId.set('');
    this.detailLoading.set(false);
    this.error.set('');
    const values = this.filters.getRawValue();
    if (values.from && values.to && values.from > values.to) {
      this.error.set('A data inicial deve ser anterior ou igual à data final.');
      this.loading.set(false);
      this.data.set(null);
      return;
    }
    const params = new URLSearchParams({ page: String(page), pageSize: '20' });
    for (const [key, value] of Object.entries(values)) if (value) params.set(key, value);
    this.loading.set(true);
    this.data.set(null);
    this.listRequest = this.api.getAuditEvents(this.token, params).subscribe({
      next: data => { this.data.set(data); this.loading.set(false); },
      error: () => { this.error.set('Não foi possível carregar o histórico. Tente atualizar.'); this.loading.set(false); },
    });
  }

  protected clear(): void {
    this.filters.reset();
    this.load();
  }

  protected toggle(item: AuditEvent): void {
    this.detailRequest?.unsubscribe();
    this.detail.set(null);
    this.error.set('');
    if (this.selectedId() === item.id) {
      this.selectedId.set('');
      this.detailLoading.set(false);
      return;
    }
    this.selectedId.set(item.id);
    this.detailLoading.set(true);
    this.detailRequest = this.api.getAuditEvent(this.token, item.id).subscribe({
      next: detail => { this.detail.set(detail); this.detailLoading.set(false); },
      error: () => { this.selectedId.set(''); this.detailLoading.set(false); this.error.set('Não foi possível abrir o evento. Ele pode ter sido removido pela retenção.'); },
    });
  }

  protected changes(detail: AuditDetail): { label: string; before: string; after: string }[] {
    const labels: Record<string, string> = { Name: 'Nome', Type: 'Tipo', Amount: 'Valor', Date: 'Data', Description: 'Descrição', InitialBalance: 'Saldo inicial', OpeningDate: 'Data de abertura', AccountId: 'Identificador da conta', CategoryId: 'Identificador da categoria' };
    try {
      const before = detail.beforeJson ? JSON.parse(detail.beforeJson) : {};
      const after = detail.afterJson ? JSON.parse(detail.afterJson) : {};
      return Object.entries(labels).filter(([key]) => key in before || key in after)
        .map(([key, label]) => ({ label, before: this.value(key, before[key]), after: this.value(key, after[key]) }));
    } catch { return []; }
  }

  private value(key: string, value: unknown): string {
    if (value === undefined || value === null || value === '') return '—';
    if ((key === 'Amount' || key === 'InitialBalance') && typeof value === 'number') return formatMoney(value);
    const types: Record<string, string> = { Income: 'Receita', Expense: 'Despesa', Wallet: 'Carteira', Checking: 'Conta corrente', Savings: 'Poupança', CreditCard: 'Cartão de crédito' };
    if (key === 'Type' && typeof value === 'string') return types[value] ?? value;
    return String(value);
  }

  ngOnDestroy(): void {
    this.listRequest?.unsubscribe();
    this.detailRequest?.unsubscribe();
  }
}

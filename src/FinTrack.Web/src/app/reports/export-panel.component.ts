import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-export-panel',
  template: `
    <section class="settings-page">
      <article class="panel settings-card">
        <div class="panel-head">
          <h2>Exportação CSV</h2>
          <button type="button" class="ghost" (click)="back.emit()">Voltar</button>
        </div>
        <div class="form-grid">
          <label>De<input type="date" [value]="startDate" (change)="startDateChange.emit($any($event.target).value)" /></label>
          <label>Até<input type="date" [value]="endDate" (change)="endDateChange.emit($any($event.target).value)" /></label>
        </div>
        <div class="empty-state">
          <strong>Baixar lançamentos filtrados</strong>
          <span>Sem período livre, usa o mês selecionado. Com datas, exporta o intervalo escolhido.</span>
          <button type="button" class="primary" style="justify-self: center" (click)="export.emit()">Baixar CSV</button>
        </div>
      </article>
    </section>
  `,
})
export class ExportPanelComponent {
  @Input() startDate = '';
  @Input() endDate = '';
  @Output() startDateChange = new EventEmitter<string>();
  @Output() endDateChange = new EventEmitter<string>();
  @Output() export = new EventEmitter<void>();
  @Output() back = new EventEmitter<void>();
}

import { Component } from '@angular/core';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly summaryCards = [
    { label: 'Receitas', value: 'R$ 0,00' },
    { label: 'Despesas', value: 'R$ 0,00' },
    { label: 'Saldo do mês', value: 'R$ 0,00' },
  ];

  protected readonly nextSteps = ['Contas', 'Categorias', 'Lançamentos'];
}

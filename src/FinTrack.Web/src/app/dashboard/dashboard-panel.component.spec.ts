import { DashboardPanelComponent } from './dashboard-panel.component';

describe('DashboardPanelComponent expense chart', () => {
  it('draws a full ring for a single expense category', () => {
    const component = new DashboardPanelComponent();
    component.dashboard = { totalIncome: 0, totalExpense: 200, monthBalance: -200, currentBalance: 300, expensesByCategory: [{ categoryName: 'Mercado', total: 200 }] };
    const slice = component.expenseDonutSlices()[0];
    const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    path.setAttribute('d', slice.path);
    svg.append(path);
    document.body.append(svg);
    try {
      expect(path.getBBox().width).toBeCloseTo(96);
      expect(path.getBBox().height).toBeCloseTo(96);
      expect(slice.tooltip).toContain('100,0%');
    } finally {
      svg.remove();
    }
  });

  it('keeps separate slices for multiple categories and none for an empty month', () => {
    const component = new DashboardPanelComponent();
    expect(component.expenseDonutSlices()).toEqual([]);
    component.dashboard = { totalIncome: 0, totalExpense: 100, monthBalance: -100, currentBalance: 0, expensesByCategory: [{ categoryName: 'Mercado', total: 75 }, { categoryName: 'Transporte', total: 25 }] };
    const slices = component.expenseDonutSlices();
    expect(slices.length).toBe(2);
    expect(slices[0].path).not.toBe(slices[1].path);
    expect(slices[0].tooltip).toContain('75,0%');
    expect(slices[1].tooltip).toContain('25,0%');
  });
});

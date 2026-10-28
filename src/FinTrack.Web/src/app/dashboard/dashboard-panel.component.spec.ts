import { DashboardPanelComponent } from './dashboard-panel.component';

describe('DashboardPanelComponent expense chart', () => {
  it('draws a full ring for a single expense category', () => {
    const component = new DashboardPanelComponent();
    component.dashboard = { totalIncome: 0, totalExpense: 200, monthBalance: -200, currentBalance: 300, dailyCashFlow: [], expensesByCategory: [{ categoryName: 'Mercado', total: 200 }] };
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
    component.dashboard = { totalIncome: 0, totalExpense: 100, monthBalance: -100, currentBalance: 0, dailyCashFlow: [], expensesByCategory: [{ categoryName: 'Mercado', total: 75 }, { categoryName: 'Transporte', total: 25 }] };
    const slices = component.expenseDonutSlices();
    expect(slices.length).toBe(2);
    expect(slices[0].path).not.toBe(slices[1].path);
    expect(slices[0].tooltip).toContain('75,0%');
    expect(slices[1].tooltip).toContain('25,0%');
    expect(slices.map(slice => slice.total)).toEqual([75, 25]);
    expect(slices.map(slice => slice.percentage)).toEqual(['75,0%', '25,0%']);
  });

  it('fills a leap month without inventing activity and carries the server balance across empty days', () => {
    const component = new DashboardPanelComponent();
    component.month = '2024-02';
    component.selectedDay = 31;
    component.dashboard = {
      totalIncome: 10, totalExpense: 40, monthBalance: -30, currentBalance: 970,
      expensesByCategory: [{ categoryName: 'Mercado', total: 40 }],
      dailyCashFlow: [
        { date: '2024-02-01', income: 10, expense: 0, balance: 10 },
        { date: '2024-02-03', income: 0, expense: 40, balance: -30 },
      ],
    };
    const chart = component.dailyChart()!;
    expect(chart.points).toHaveLength(29);
    expect(chart.points[1]).toMatchObject({ date: '2024-02-02', income: 0, expense: 0, balance: 10, incomeHeight: 0, expenseHeight: 0 });
    expect(chart.points[28]).toMatchObject({ date: '2024-02-29', balance: -30 });
    expect(chart.selected.day).toBe(29);
    expect(chart.zeroY).toBeGreaterThan(18);
    expect(chart.zeroY).toBeLessThan(208);
    expect(chart.points.every(day => day.incomeHeight >= 0 && day.expenseHeight >= 0 && Number.isFinite(day.balanceY))).toBe(true);
    expect(chart.points[2].balanceY).toBeGreaterThan(chart.zeroY);
    expect(component.dailyTooltip(chart.points[2])).toContain('03/02/2024');
    expect(component.dailyTooltip(chart.points[2])).toContain('-R$\u00a030,00');
  });

  it('does not use the recent transaction page to invent a daily chart', () => {
    const component = new DashboardPanelComponent();
    component.month = '2026-10';
    component.transactions = [{ id: 'recent', accountId: 'account', categoryId: 'category', type: 'Expense', amount: 10, date: '2026-10-10', description: null }];
    expect(component.dailyChart()).toBeNull();
    component.dashboard = { totalIncome: 0, totalExpense: 0, monthBalance: 0, currentBalance: 500, expensesByCategory: [], dailyCashFlow: [] };
    expect(component.dailyChart()).toBeNull();
  });
});

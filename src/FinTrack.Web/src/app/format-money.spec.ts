import { formatMoney } from './format-money';

describe('formatMoney', () => {
  it('formats zero, decimals and negative balances in BRL', () => {
    expect(formatMoney(0)).toBe('R$\u00a00,00');
    expect(formatMoney(1234.56)).toBe('R$\u00a01.234,56');
    expect(formatMoney(-10.5)).toBe('-R$\u00a010,50');
  });
});

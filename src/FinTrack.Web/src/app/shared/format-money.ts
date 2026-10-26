const formatter = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

export function formatMoney(value: number): string {
  return formatter.format(value);
}

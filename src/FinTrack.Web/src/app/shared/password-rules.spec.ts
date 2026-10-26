import { isStrongPassword, passwordStrength, passwordStrengthLabel, passwordStrengthColor, passwordRequirementColor, passwordRequirementIcon } from './password-rules';

describe('password rules', () => {
  it('requires all character classes and between 8 and 100 characters', () => {
    expect(isStrongPassword('Aa1!aaaa')).toBe(true);
    expect(isStrongPassword('Aa1!' + 'a'.repeat(96))).toBe(true);
    for (const password of ['Aa1!aaa', 'Aa1!' + 'a'.repeat(97), 'aa1!aaaa', 'AA1!AAAA', 'Aaa!aaaa', 'Aa11aaaa']) {
      expect(isStrongPassword(password)).toBe(false);
    }
  });

  it('preserves strength feedback and requirement indicators', () => {
    expect(passwordStrength('Aa1!aaaa')).toBe(5);
    expect(['a', 'Aaaaaaaa', 'Aa1!aaaa'].map(passwordStrengthLabel)).toEqual(['fraca', 'média', 'forte']);
    expect(['a', 'Aaaaaaaa', 'Aa1!aaaa'].map(passwordStrengthColor)).toEqual(['#dc2626', '#d97706', '#059669']);
    for (const requirement of ['length', 'upper', 'lower', 'numberSymbol'] as const) {
      expect(passwordRequirementIcon('Aa1!aaaa', requirement)).toBe('✓');
      expect(passwordRequirementColor('Aa1!aaaa', requirement)).toBe('#059669');
      expect(passwordRequirementIcon('', requirement)).toBe('•');
      expect(passwordRequirementColor('', requirement)).toBe('#dc2626');
    }
    expect(passwordRequirementIcon('Aa1aaaaa', 'numberSymbol')).toBe('•');
    expect(passwordRequirementIcon('Aa!aaaaa', 'numberSymbol')).toBe('•');
  });
});

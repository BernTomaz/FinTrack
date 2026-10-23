type PasswordRequirement = 'length' | 'upper' | 'lower' | 'numberSymbol';

export function passwordStrength(password: string): number {
  return passwordRequirements(password).filter(Boolean).length;
}

export function passwordStrengthLabel(password: string): string {
  const strength = passwordStrength(password);

  if (strength < 3) {
    return 'fraca';
  }

  if (strength < 5) {
    return 'média';
  }

  return 'forte';
}

export function passwordStrengthColor(password: string): string {
  const strength = passwordStrength(password);

  if (strength < 3) {
    return '#dc2626';
  }

  if (strength < 5) {
    return '#d97706';
  }

  return '#059669';
}

export function passwordRequirementColor(password: string, requirement: PasswordRequirement): string {
  return passwordRequirementMet(password, requirement) ? '#059669' : '#dc2626';
}

export function passwordRequirementIcon(password: string, requirement: PasswordRequirement): string {
  return passwordRequirementMet(password, requirement) ? '✓' : '•';
}

export function isStrongPassword(password: string): boolean {
  return password.length <= 100 && passwordStrength(password) === 5;
}

function passwordRequirements(password: string): boolean[] {
  return [
    password.length >= 8,
    /[A-Z]/.test(password),
    /[a-z]/.test(password),
    /\d/.test(password),
    /[^A-Za-z0-9]/.test(password),
  ];
}

function passwordRequirementMet(password: string, requirement: PasswordRequirement): boolean {
  switch (requirement) {
    case 'length':
      return password.length >= 8;
    case 'upper':
      return /[A-Z]/.test(password);
    case 'lower':
      return /[a-z]/.test(password);
    case 'numberSymbol':
      return /\d/.test(password) && /[^A-Za-z0-9]/.test(password);
  }
}

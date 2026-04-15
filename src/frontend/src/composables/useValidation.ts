export function validateEmail(email: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())
}

/** Vietnamese phone: 10 digits, starts with 03/05/07/08/09 */
export function validatePhone(phone: string): boolean {
  return /^(03|05|07|08|09)\d{8}$/.test(phone.trim())
}

export type PasswordStrength = 'weak' | 'medium' | 'strong'

export function validatePasswordStrength(password: string): PasswordStrength {
  if (password.length < 8) return 'weak'

  let score = 0
  if (password.length >= 12) score++
  if (/[a-z]/.test(password)) score++
  if (/[A-Z]/.test(password)) score++
  if (/\d/.test(password)) score++
  if (/[^a-zA-Z0-9]/.test(password)) score++

  if (score <= 2) return 'weak'
  if (score <= 3) return 'medium'
  return 'strong'
}

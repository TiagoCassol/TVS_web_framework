export type Perfil = 'Gestor'

export interface Usuario {
  id: number
  nome: string
  email: string
  perfil: Perfil
}

export interface Credenciais {
  email: string
  senha: string
}

export function validarCredenciais({ email, senha }: Credenciais): string | null {
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) return 'Informe um e-mail válido.'
  if (!senha) return 'Informe a senha.'
  return null
}

export interface Cliente {
  id: number
  nome: string
  /** Somente dígitos (11). */
  cpf: string
  /** Somente dígitos (DDD + número). */
  telefone: string
  /** Opcional: o mínimo para reservar é nome, CPF e telefone. */
  email: string
  ativo: boolean
}

export interface NovoCliente {
  nome: string
  cpf: string
  telefone: string
  email?: string
}

export const somenteDigitos = (valor: string) => valor.replace(/\D/g, '')

/** Valida formato e dígitos verificadores do CPF. */
export function cpfValido(valor: string): boolean {
  const cpf = somenteDigitos(valor)
  if (cpf.length !== 11 || /^(\d)\1{10}$/.test(cpf)) return false

  const digito = (tamanho: number) => {
    let soma = 0
    for (let i = 0; i < tamanho; i++) soma += Number(cpf[i]) * (tamanho + 1 - i)
    const resto = (soma * 10) % 11
    return resto === 10 ? 0 : resto
  }
  return digito(9) === Number(cpf[9]) && digito(10) === Number(cpf[10])
}

export function telefoneValido(valor: string): boolean {
  const n = somenteDigitos(valor).length
  return n === 10 || n === 11
}

/** Validação de formulário. Retorna a lista de mensagens de erro (vazia = válido). */
export function validarNovoCliente(dados: NovoCliente): string[] {
  const erros: string[] = []
  if (!dados.nome.trim()) erros.push('Nome é obrigatório.')
  if (!cpfValido(dados.cpf)) erros.push('CPF inválido.')
  if (!telefoneValido(dados.telefone)) erros.push('Telefone deve ter DDD + número.')
  if (dados.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(dados.email))
    erros.push('E-mail inválido.')
  return erros
}

/** Normaliza antes de enviar: remove máscara e espaços extras. */
export function normalizarNovoCliente(dados: NovoCliente): NovoCliente {
  return {
    nome: dados.nome.trim(),
    cpf: somenteDigitos(dados.cpf),
    telefone: somenteDigitos(dados.telefone),
    email: dados.email?.trim() || undefined,
  }
}

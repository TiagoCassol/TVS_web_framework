/**
 * Erro único que a camada de apresentação conhece.
 * Os adaptadores traduzem o que vier (status HTTP, ProblemDetails,
 * falha de rede, regra do mock) para um destes tipos.
 */
export type TipoErro =
  | 'validacao' // 400
  | 'nao_autorizado' // 401 — login inválido ou sessão expirada
  | 'nao_encontrado' // 404
  | 'conflito' // 409 — ex.: horário indisponível
  | 'regra_negocio' // 422
  | 'rede' // backend fora do ar
  | 'inesperado' // 500 e afins

export class AppError extends Error {
  constructor(
    public readonly tipo: TipoErro,
    message: string,
    public readonly detalhes: string[] = [],
  ) {
    super(message)
    this.name = 'AppError'
  }
}

export function mensagemDeErro(erro: unknown): string {
  if (erro instanceof AppError) return erro.message
  return 'Ocorreu um erro inesperado.'
}

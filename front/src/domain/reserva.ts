export type StatusReserva =
  | 'PendentePagamento'
  | 'Confirmada'
  | 'Cancelada'
  | 'Expirada'
  | 'Concluida'

/** Substitui os "números mágicos" (TipoCliente == 1) por nomes legíveis. */
export type TipoCliente = 'Padrao' | 'Socio' | 'Aluno'

export const TIPOS_CLIENTE: { valor: TipoCliente; rotulo: string }[] = [
  { valor: 'Padrao', rotulo: 'Padrão' },
  { valor: 'Socio', rotulo: 'Sócio' },
  { valor: 'Aluno', rotulo: 'Aluno' },
]

export const DURACAO_MINIMA_MINUTOS = 60

export interface Intervalo {
  inicio: Date
  fim: Date
}

export interface Reserva extends Intervalo {
  id: number
  clienteId: number
  quadraId: number
  valorBruto: number
  desconto: number
  valorFinal: number
  status: StatusReserva
}

export interface NovaReserva extends Intervalo {
  clienteId: number
  quadraId: number
  tipoCliente: TipoCliente
}

export interface HorarioDisponivel extends Intervalo {
  disponivel: boolean
}

/** Reservas nesses status ainda ocupam o horário da quadra. */
export function ocupaHorario(status: StatusReserva): boolean {
  return status === 'PendentePagamento' || status === 'Confirmada'
}

/** novoInicio < existenteFim AND novoFim > existenteInicio */
export function haConflito(a: Intervalo, b: Intervalo): boolean {
  return a.inicio < b.fim && a.fim > b.inicio
}

export const DURACOES_MINUTOS = [60, 90, 120]

/**
 * Durações que cabem a partir de `inicio` sem invadir horário ocupado
 * nem passar do último horário da grade.
 */
export function duracoesPossiveis(horarios: HorarioDisponivel[], inicio: Date): number[] {
  const fimDaGrade = horarios.at(-1)?.fim
  if (!fimDaGrade) return []
  return DURACOES_MINUTOS.filter((minutos) => {
    const pedido = { inicio, fim: new Date(inicio.getTime() + minutos * 60_000) }
    if (pedido.fim > fimDaGrade) return false
    return horarios.every((h) => h.disponivel || !haConflito(h, pedido))
  })
}

/**
 * Validação antecipada para feedback rápido na tela.
 * O backend continua sendo a fonte da verdade e valida de novo.
 */
export function validarIntervalo({ inicio, fim }: Intervalo): string | null {
  if (fim <= inicio) return 'O fim deve ser depois do início.'
  const minutos = (fim.getTime() - inicio.getTime()) / 60_000
  if (minutos < DURACAO_MINIMA_MINUTOS)
    return `A reserva deve ter no mínimo ${DURACAO_MINIMA_MINUTOS} minutos.`
  return null
}

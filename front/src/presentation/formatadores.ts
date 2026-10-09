import { somenteDigitos } from '@/domain/cliente'
import type { StatusReserva } from '@/domain/reserva'

const moeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })
const hora = new Intl.DateTimeFormat('pt-BR', { hour: '2-digit', minute: '2-digit' })
const diaCompleto = new Intl.DateTimeFormat('pt-BR', { weekday: 'long', day: '2-digit', month: 'long' })
const diaSemanaCurto = new Intl.DateTimeFormat('pt-BR', { weekday: 'short' })

export const formatarMoeda = (valor: number) => moeda.format(valor)
export const formatarHora = (data: Date) => hora.format(data)
export const formatarDia = (data: Date) => diaCompleto.format(data)
export const formatarDiaSemanaCurto = (data: Date) => diaSemanaCurto.format(data).replace('.', '')

export function formatarDuracao(minutos: number): string {
  const h = Math.floor(minutos / 60)
  const m = minutos % 60
  return m ? `${h}h${m}` : `${h}h`
}

/** Aplica a máscara 000.000.000-00 enquanto o usuário digita. */
export function mascaraCpf(valor: string): string {
  const d = somenteDigitos(valor).slice(0, 11)
  return d
    .replace(/^(\d{3})(\d)/, '$1.$2')
    .replace(/^(\d{3})\.(\d{3})(\d)/, '$1.$2.$3')
    .replace(/\.(\d{3})(\d{1,2})$/, '.$1-$2')
}

/** Aplica a máscara (00) 00000-0000 ou (00) 0000-0000 enquanto o usuário digita. */
export function mascaraTelefone(valor: string): string {
  const d = somenteDigitos(valor).slice(0, 11)
  if (d.length <= 2) return d.length ? `(${d}` : ''
  const meio = d.length === 11 ? 7 : 6
  return d.length <= meio
    ? `(${d.slice(0, 2)}) ${d.slice(2)}`
    : `(${d.slice(0, 2)}) ${d.slice(2, meio)}-${d.slice(meio)}`
}

export const ROTULO_STATUS: Record<StatusReserva, string> = {
  PendentePagamento: 'Pendente de pagamento',
  Confirmada: 'Confirmada',
  Cancelada: 'Cancelada',
  Expirada: 'Expirada',
  Concluida: 'Concluída',
}

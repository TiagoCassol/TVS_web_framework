import { somenteDigitos, type Cliente } from '@/domain/cliente'
import type { Quadra } from '@/domain/quadra'
import type {
  HorarioDisponivel,
  NovaReserva,
  Reserva,
  StatusReserva,
  TipoCliente,
} from '@/domain/reserva'
import type { Usuario } from '@/domain/usuario'

/*
 * DTOs = formato exato do JSON da API.
 * Mapeadores = única parte do front que conhece esse formato.
 * Se o backend renomear um campo, só este arquivo muda.
 */

export interface QuadraDto {
  id: number
  nome?: string
  /** O backend atual ainda usa `Name` na entidade Quadra. */
  name?: string
  ativa?: boolean
}

export interface ClienteDto {
  id: number
  nome?: string
  name?: string
  cpf?: string
  email?: string
  telefone?: string
  ativo?: boolean
}

export interface ReservaDto {
  id: number
  clienteId: number
  quadraId: number
  inicio: string
  fim: string
  valorBruto?: number
  desconto?: number
  valorFinal?: number
  /** Atual: `valor`. Proposto: valorBruto/desconto/valorFinal. */
  valor?: number
  status: number | string
}

export interface UsuarioDto {
  id: number
  nome: string
  email: string
  perfil: string
}

export interface CriarReservaDto {
  clienteId: number
  quadraId: number
  tipoCliente: number
  inicio: string
  fim: string
}

export interface HorarioDto {
  inicio: string
  fim: string
  disponivel: boolean
}

const STATUS_POR_CODIGO: Record<number, StatusReserva> = {
  1: 'PendentePagamento',
  2: 'Confirmada',
  3: 'Cancelada',
  4: 'Expirada',
  5: 'Concluida',
}

// Mesmo significado do Program.cs: 1 = 20% de desconto, 2 = 30%.
const CODIGO_TIPO_CLIENTE: Record<TipoCliente, number> = {
  Padrao: 0,
  Socio: 1,
  Aluno: 2,
}

export function paraQuadra(dto: QuadraDto): Quadra {
  return { id: dto.id, nome: dto.nome ?? dto.name ?? '', ativa: dto.ativa ?? true }
}

export function paraCliente(dto: ClienteDto): Cliente {
  return {
    id: dto.id,
    nome: dto.nome ?? dto.name ?? '',
    cpf: somenteDigitos(dto.cpf ?? ''),
    email: dto.email ?? '',
    telefone: somenteDigitos(dto.telefone ?? ''),
    ativo: dto.ativo ?? true,
  }
}

export function paraReserva(dto: ReservaDto): Reserva {
  const valorFinal = dto.valorFinal ?? dto.valor ?? 0
  return {
    id: dto.id,
    clienteId: dto.clienteId,
    quadraId: dto.quadraId,
    inicio: new Date(dto.inicio),
    fim: new Date(dto.fim),
    valorBruto: dto.valorBruto ?? valorFinal,
    desconto: dto.desconto ?? 0,
    valorFinal,
    status:
      typeof dto.status === 'number'
        ? (STATUS_POR_CODIGO[dto.status] ?? 'PendentePagamento')
        : (dto.status as StatusReserva),
  }
}

export function paraUsuario(dto: UsuarioDto): Usuario {
  return { id: dto.id, nome: dto.nome, email: dto.email, perfil: 'Gestor' }
}

export function paraHorario(dto: HorarioDto): HorarioDisponivel {
  return { inicio: new Date(dto.inicio), fim: new Date(dto.fim), disponivel: dto.disponivel }
}

export function deNovaReserva(dados: NovaReserva): CriarReservaDto {
  return {
    clienteId: dados.clienteId,
    quadraId: dados.quadraId,
    tipoCliente: CODIGO_TIPO_CLIENTE[dados.tipoCliente],
    inicio: dados.inicio.toISOString(),
    fim: dados.fim.toISOString(),
  }
}

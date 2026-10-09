import type { Cliente, NovoCliente } from '@/domain/cliente'
import type { NovaQuadra, Quadra } from '@/domain/quadra'
import type { HorarioDisponivel, NovaReserva, Reserva } from '@/domain/reserva'
import type { Credenciais, Usuario } from '@/domain/usuario'

/*
 * Portas: contratos que a aplicação espera de uma fonte de dados.
 * As telas só conhecem estas interfaces; quem as implementa
 * (HTTP real ou mock em memória) é decidido em infrastructure/container.ts.
 */

export interface QuadraGateway {
  listar(): Promise<Quadra[]>
  criar(dados: NovaQuadra): Promise<Quadra>
}

export interface ClienteGateway {
  listar(): Promise<Cliente[]>
  /** CPF somente com dígitos. Retorna null se não houver cadastro. */
  buscarPorCpf(cpf: string): Promise<Cliente | null>
  criar(dados: NovoCliente): Promise<Cliente>
}

export interface FiltroReservas {
  /** Data no formato AAAA-MM-DD. */
  data?: string
  clienteId?: number
}

export interface ReservaGateway {
  listar(filtro: FiltroReservas): Promise<Reserva[]>
  criar(dados: NovaReserva): Promise<Reserva>
  cancelar(id: number): Promise<void>
  /** Data no formato AAAA-MM-DD. */
  disponibilidade(quadraId: number, data: string): Promise<HorarioDisponivel[]>
}

export interface AuthGateway {
  /** Lança AppError 'nao_autorizado' se e-mail ou senha estiverem errados. */
  entrar(credenciais: Credenciais): Promise<Usuario>
  sair(): Promise<void>
  /** Usuário da sessão salva (ex.: após recarregar a página), ou null. */
  usuarioAtual(): Usuario | null
}

export interface Gateways {
  auth: AuthGateway
  quadras: QuadraGateway
  clientes: ClienteGateway
  reservas: ReservaGateway
}

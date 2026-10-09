import { AppError } from '@/application/erros'
import type { Gateways } from '@/application/ports'
import type { Cliente } from '@/domain/cliente'
import { combinarDataHora, paraDataIso, somarMinutos } from '@/domain/datas'
import type { Quadra } from '@/domain/quadra'
import {
  haConflito,
  ocupaHorario,
  validarIntervalo,
  type HorarioDisponivel,
  type Reserva,
  type TipoCliente,
} from '@/domain/reserva'
import { criarMockAuth } from './mockAuth'

/*
 * Implementação em memória das portas. Simula as regras que o backend
 * deve aplicar (funcionamento, conflito, preço) para o front poder ser
 * desenvolvido e demonstrado sem depender da API.
 */

const ABRE_AS = 7
const FECHA_AS = 22
const PRECO_HORA = 100
const DESCONTO: Record<TipoCliente, number> = { Padrao: 0, Socio: 0.2, Aluno: 0.3 }

export interface OpcoesMock {
  /** Latência artificial em ms, para enxergar estados de carregamento. */
  latencia?: number
}

export function criarMockGateways({ latencia = 250 }: OpcoesMock = {}): Gateways {
  const quadras: Quadra[] = [
    { id: 1, nome: 'Quadra 1', ativa: true },
    { id: 2, nome: 'Quadra 2', ativa: true },
    { id: 3, nome: 'Quadra 3 (coberta)', ativa: true },
  ]
  const clientes: Cliente[] = [
    // CPFs fictícios, válidos só pelos dígitos verificadores.
    { id: 1, nome: 'Ana Souza', cpf: '52998224725', telefone: '51999990001', email: 'ana@exemplo.com', ativo: true },
    { id: 2, nome: 'Bruno Lima', cpf: '11144477735', telefone: '51999990002', email: '', ativo: true },
  ]
  const reservas: Reserva[] = []
  let proximoId = 100

  const esperar = <T>(valor: T): Promise<T> =>
    new Promise((resolve) => setTimeout(() => resolve(structuredClone(valor)), latencia))

  return {
    auth: criarMockAuth(latencia),

    quadras: {
      listar: () => esperar(quadras),
      async criar({ nome }) {
        const quadra: Quadra = { id: ++proximoId, nome: nome.trim(), ativa: true }
        quadras.push(quadra)
        return esperar(quadra)
      },
    },

    clientes: {
      listar: () => esperar(clientes),
      buscarPorCpf: (cpf) => esperar(clientes.find((c) => c.cpf === cpf) ?? null),
      async criar(dados) {
        if (clientes.some((c) => c.cpf === dados.cpf))
          throw new AppError('conflito', 'Já existe um cliente com este CPF.')
        const email = dados.email ?? ''
        if (email && clientes.some((c) => c.email.toLowerCase() === email.toLowerCase()))
          throw new AppError('conflito', 'Já existe um cliente com este e-mail.')
        const cliente: Cliente = { id: ++proximoId, ativo: true, ...dados, email }
        clientes.push(cliente)
        return esperar(cliente)
      },
    },

    reservas: {
      listar({ data, clienteId }) {
        return esperar(
          reservas
            .filter((r) => !data || paraDataIso(r.inicio) === data)
            .filter((r) => !clienteId || r.clienteId === clienteId)
            .sort((a, b) => a.inicio.getTime() - b.inicio.getTime()),
        )
      },

      async criar(dados) {
        const invalido = validarIntervalo(dados)
        if (invalido) throw new AppError('validacao', invalido)
        if (dados.inicio <= new Date())
          throw new AppError('regra_negocio', 'Não é possível reservar um horário que já passou.')

        const cliente = clientes.find((c) => c.id === dados.clienteId)
        if (!cliente) throw new AppError('nao_encontrado', 'Cliente não encontrado.')
        if (!cliente.ativo) throw new AppError('regra_negocio', 'Cliente inativo.')

        const quadra = quadras.find((q) => q.id === dados.quadraId)
        if (!quadra) throw new AppError('nao_encontrado', 'Quadra não encontrada.')
        if (!quadra.ativa) throw new AppError('regra_negocio', 'Quadra inativa.')

        const dia = paraDataIso(dados.inicio)
        if (
          dados.inicio < combinarDataHora(dia, ABRE_AS) ||
          dados.fim > combinarDataHora(dia, FECHA_AS)
        )
          throw new AppError(
            'regra_negocio',
            `Fora do horário de funcionamento (${ABRE_AS}h às ${FECHA_AS}h).`,
          )

        const conflito = reservas.some(
          (r) => r.quadraId === dados.quadraId && ocupaHorario(r.status) && haConflito(r, dados),
        )
        if (conflito) throw new AppError('conflito', 'Horário indisponível para esta quadra.')

        const horas = (dados.fim.getTime() - dados.inicio.getTime()) / 3_600_000
        const valorBruto = horas * PRECO_HORA
        const desconto = valorBruto * DESCONTO[dados.tipoCliente]

        const reserva: Reserva = {
          id: ++proximoId,
          clienteId: dados.clienteId,
          quadraId: dados.quadraId,
          inicio: dados.inicio,
          fim: dados.fim,
          valorBruto,
          desconto,
          valorFinal: valorBruto - desconto,
          status: 'PendentePagamento',
        }
        reservas.push(reserva)
        return esperar(reserva)
      },

      async cancelar(id) {
        const reserva = reservas.find((r) => r.id === id)
        if (!reserva) throw new AppError('nao_encontrado', 'Reserva não encontrada.')
        if (!ocupaHorario(reserva.status))
          throw new AppError('regra_negocio', 'Esta reserva não pode mais ser cancelada.')
        reserva.status = 'Cancelada'
        await esperar(undefined)
      },

      disponibilidade(quadraId, data) {
        const horarios: HorarioDisponivel[] = []
        for (let h = ABRE_AS; h < FECHA_AS; h++) {
          const inicio = combinarDataHora(data, h)
          const slot = { inicio, fim: somarMinutos(inicio, 60) }
          const ocupado = reservas.some(
            (r) => r.quadraId === quadraId && ocupaHorario(r.status) && haConflito(r, slot),
          )
          horarios.push({ ...slot, disponivel: !ocupado })
        }
        return esperar(horarios)
      },
    },
  }
}

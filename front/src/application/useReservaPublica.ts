import { ref } from 'vue'
import { normalizarNovoCliente, validarNovoCliente, type NovoCliente } from '@/domain/cliente'
import { somarMinutos } from '@/domain/datas'
import type { Quadra } from '@/domain/quadra'
import { validarIntervalo, type HorarioDisponivel, type Reserva } from '@/domain/reserva'
import { useGateways } from './injecao'
import { useAsync } from './useAsync'

export interface AgendaQuadra {
  quadra: Quadra
  horarios: HorarioDisponivel[]
}

/**
 * Caso de uso da área pública: o próprio cliente consulta a agenda e reserva.
 * Ele não escolhe um cliente de uma lista; informa os seus dados mínimos.
 */
export function useReservaPublica() {
  const gateways = useGateways()
  const { carregando, erro, executar } = useAsync()
  const agenda = ref<AgendaQuadra[]>([])

  async function carregarAgenda(data: string) {
    const resultado = await executar(async () => {
      const quadras = (await gateways.quadras.listar()).filter((q) => q.ativa)
      const agora = new Date()
      return Promise.all(
        quadras.map(async (quadra) => {
          const horarios = await gateways.reservas.disponibilidade(quadra.id, data)
          // Horário que já começou não pode ser reservado, mesmo que a API o devolva livre.
          return {
            quadra,
            horarios: horarios.map((h) => ({ ...h, disponivel: h.disponivel && h.inicio > agora })),
          }
        }),
      )
    })
    agenda.value = resultado ?? []
  }

  /** Reaproveita o cadastro pelo CPF; se não existir, cadastra. Depois cria a reserva. */
  async function reservar(
    dadosCliente: NovoCliente,
    quadraId: number,
    inicio: Date,
    duracaoMinutos: number,
  ): Promise<Reserva | undefined> {
    const errosCliente = validarNovoCliente(dadosCliente)
    if (errosCliente.length) {
      erro.value = errosCliente.join(' ')
      return undefined
    }
    const intervalo = { inicio, fim: somarMinutos(inicio, duracaoMinutos) }
    const invalido = validarIntervalo(intervalo)
    if (invalido) {
      erro.value = invalido
      return undefined
    }

    const dados = normalizarNovoCliente(dadosCliente)
    return executar(async () => {
      const cliente =
        (await gateways.clientes.buscarPorCpf(dados.cpf)) ?? (await gateways.clientes.criar(dados))
      return gateways.reservas.criar({
        clienteId: cliente.id,
        quadraId,
        tipoCliente: 'Padrao',
        ...intervalo,
      })
    })
  }

  return { agenda, carregando, erro, carregarAgenda, reservar }
}

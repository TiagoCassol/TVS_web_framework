import { ref } from 'vue'
import {
  validarIntervalo,
  type HorarioDisponivel,
  type NovaReserva,
  type Reserva,
} from '@/domain/reserva'
import { useGateways } from './injecao'
import { useAsync } from './useAsync'

export function useReservas() {
  const { reservas: gateway } = useGateways()
  const { carregando, erro, executar } = useAsync()
  const reservas = ref<Reserva[]>([])
  const horarios = ref<HorarioDisponivel[]>([])

  async function carregarDoDia(data: string) {
    reservas.value = (await executar(() => gateway.listar({ data }))) ?? []
  }

  async function carregarDisponibilidade(quadraId: number, data: string) {
    horarios.value = (await executar(() => gateway.disponibilidade(quadraId, data))) ?? []
  }

  async function criar(dados: NovaReserva): Promise<Reserva | undefined> {
    const invalido = validarIntervalo(dados)
    if (invalido) {
      erro.value = invalido
      return undefined
    }
    return executar(() => gateway.criar(dados))
  }

  async function cancelar(id: number): Promise<boolean> {
    let ok = false
    await executar(async () => {
      await gateway.cancelar(id)
      ok = true
    })
    return ok
  }

  return {
    reservas,
    horarios,
    carregando,
    erro,
    carregarDoDia,
    carregarDisponibilidade,
    criar,
    cancelar,
  }
}

import type { Gateways } from '@/application/ports'
import {
  deNovaReserva,
  paraCliente,
  paraHorario,
  paraQuadra,
  paraReserva,
  paraUsuario,
  type ClienteDto,
  type HorarioDto,
  type QuadraDto,
  type ReservaDto,
  type UsuarioDto,
} from './contratos'
import { lerUsuario, salvarUsuario } from '../sessaoArmazenada'
import type { HttpClient } from './httpClient'

export function criarHttpGateways(http: HttpClient): Gateways {
  return {
    // Sem JWT por enquanto: a API só confere e-mail/senha e devolve o usuário.
    auth: {
      async entrar(credenciais) {
        const usuario = paraUsuario(await http.post<UsuarioDto>('/api/auth/login', credenciais))
        salvarUsuario(usuario)
        return usuario
      },
      async sair() {
        salvarUsuario(null)
      },
      usuarioAtual: lerUsuario,
    },

    quadras: {
      listar: async () => (await http.get<QuadraDto[]>('/api/quadras')).map(paraQuadra),
      criar: async (dados) => paraQuadra(await http.post<QuadraDto>('/api/quadras', dados)),
    },

    clientes: {
      listar: async () => (await http.get<ClienteDto[]>('/api/clientes')).map(paraCliente),
      async buscarPorCpf(cpf) {
        const lista = await http.get<ClienteDto[]>(`/api/clientes?cpf=${cpf}`)
        return lista.length ? paraCliente(lista[0]) : null
      },
      criar: async (dados) => paraCliente(await http.post<ClienteDto>('/api/clientes', dados)),
    },

    reservas: {
      async listar(filtro) {
        const query = new URLSearchParams()
        if (filtro.data) query.set('data', filtro.data)
        if (filtro.clienteId) query.set('clienteId', String(filtro.clienteId))
        const lista = await http.get<ReservaDto[]>(`/api/reservas?${query}`)
        return lista.map(paraReserva)
      },
      criar: async (dados) =>
        paraReserva(await http.post<ReservaDto>('/api/reservas', deNovaReserva(dados))),
      async cancelar(id) {
        await http.post(`/api/reservas/${id}/cancelamento`)
      },
      async disponibilidade(quadraId, data) {
        const lista = await http.get<HorarioDto[]>(
          `/api/quadras/${quadraId}/disponibilidade?data=${data}`,
        )
        return lista.map(paraHorario)
      },
    },
  }
}

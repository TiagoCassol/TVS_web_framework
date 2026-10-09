import { describe, expect, it } from 'vitest'
import { AppError } from '@/application/erros'
import { combinarDataHora } from '@/domain/datas'
import { haConflito, validarIntervalo } from '@/domain/reserva'
import { deNovaReserva, paraQuadra, paraReserva } from '@/infrastructure/http/contratos'
import { criarMockGateways } from '@/infrastructure/mock/mockGateways'

const dia = '2099-10-10' // sempre no futuro
const as = (h: number, m = 0) => combinarDataHora(dia, h, m)

describe('domínio: reserva', () => {
  it('detecta conflito parcial e completo', () => {
    expect(haConflito({ inicio: as(10), fim: as(11) }, { inicio: as(10, 30), fim: as(11, 30) })).toBe(true)
    expect(haConflito({ inicio: as(10), fim: as(12) }, { inicio: as(10, 30), fim: as(11) })).toBe(true)
  })

  it('horários encostados não conflitam', () => {
    expect(haConflito({ inicio: as(10), fim: as(11) }, { inicio: as(11), fim: as(12) })).toBe(false)
  })

  it('valida intervalo invertido e duração mínima', () => {
    expect(validarIntervalo({ inicio: as(11), fim: as(10) })).not.toBeNull()
    expect(validarIntervalo({ inicio: as(10), fim: as(10, 30) })).not.toBeNull()
    expect(validarIntervalo({ inicio: as(10), fim: as(11) })).toBeNull()
  })
})

describe('adaptador mock', () => {
  const novo = () => criarMockGateways({ latencia: 0 })

  it('aplica desconto de sócio', async () => {
    const { reservas } = novo()
    const r = await reservas.criar({ clienteId: 1, quadraId: 1, tipoCliente: 'Socio', inicio: as(10), fim: as(11) })
    expect(r.valorBruto).toBe(100)
    expect(r.valorFinal).toBe(80)
  })

  it('recusa reserva em horário ocupado e libera após cancelamento', async () => {
    const { reservas } = novo()
    const base = { clienteId: 1, quadraId: 1, tipoCliente: 'Padrao' as const }
    const r = await reservas.criar({ ...base, inicio: as(10), fim: as(11) })

    await expect(reservas.criar({ ...base, inicio: as(10, 30), fim: as(11, 30) })).rejects.toMatchObject({
      tipo: 'conflito',
    })

    await reservas.cancelar(r.id)
    await expect(reservas.criar({ ...base, inicio: as(10, 30), fim: as(11, 30) })).resolves.toBeDefined()
  })

  it('recusa fora do horário de funcionamento', async () => {
    const { reservas } = novo()
    const erro = await reservas
      .criar({ clienteId: 1, quadraId: 1, tipoCliente: 'Padrao', inicio: as(21, 30), fim: as(22, 30) })
      .catch((e) => e)
    expect(erro).toBeInstanceOf(AppError)
    expect(erro.tipo).toBe('regra_negocio')
  })
})

describe('contratos HTTP', () => {
  it('aceita o campo `name` do backend atual', () => {
    expect(paraQuadra({ id: 1, name: 'Central' }).nome).toBe('Central')
  })

  it('converte status numérico e tipo de cliente', () => {
    const r = paraReserva({ id: 1, clienteId: 1, quadraId: 1, inicio: as(10).toISOString(), fim: as(11).toISOString(), valor: 50, status: 3 })
    expect(r.status).toBe('Cancelada')
    expect(r.valorFinal).toBe(50)
    expect(deNovaReserva({ clienteId: 1, quadraId: 1, tipoCliente: 'Aluno', inicio: as(10), fim: as(11) }).tipoCliente).toBe(2)
  })
})

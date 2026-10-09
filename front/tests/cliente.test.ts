import { describe, expect, it } from 'vitest'
import { cpfValido, normalizarNovoCliente, validarNovoCliente } from '@/domain/cliente'
import { combinarDataHora } from '@/domain/datas'
import { duracoesPossiveis, type HorarioDisponivel } from '@/domain/reserva'
import { criarMockGateways } from '@/infrastructure/mock/mockGateways'
import { mascaraCpf, mascaraTelefone } from '@/presentation/formatadores'

const dia = '2099-10-10'
const as = (h: number, m = 0) => combinarDataHora(dia, h, m)

describe('CPF e dados mínimos do cliente', () => {
  it('valida dígitos verificadores', () => {
    expect(cpfValido('529.982.247-25')).toBe(true)
    expect(cpfValido('52998224724')).toBe(false)
    expect(cpfValido('111.111.111-11')).toBe(false)
    expect(cpfValido('123')).toBe(false)
  })

  it('exige nome, CPF e telefone; e-mail é opcional', () => {
    expect(validarNovoCliente({ nome: 'Ana', cpf: '529.982.247-25', telefone: '(51) 99999-0001' })).toEqual([])
    expect(validarNovoCliente({ nome: '', cpf: '1', telefone: '1' })).toHaveLength(3)
    expect(validarNovoCliente({ nome: 'Ana', cpf: '52998224725', telefone: '51999990001', email: 'x' })).toEqual([
      'E-mail inválido.',
    ])
  })

  it('remove máscaras antes de enviar', () => {
    expect(normalizarNovoCliente({ nome: ' Ana ', cpf: '529.982.247-25', telefone: '(51) 99999-0001', email: '' })).toEqual({
      nome: 'Ana',
      cpf: '52998224725',
      telefone: '51999990001',
      email: undefined,
    })
  })

  it('aplica máscaras de digitação', () => {
    expect(mascaraCpf('52998224725')).toBe('529.982.247-25')
    expect(mascaraCpf('5299')).toBe('529.9')
    expect(mascaraTelefone('51999990001')).toBe('(51) 99999-0001')
    expect(mascaraTelefone('5133334444')).toBe('(51) 3333-4444')
  })
})

describe('durações possíveis na grade', () => {
  const grade = (ocupados: number[]): HorarioDisponivel[] =>
    [18, 19, 20, 21].map((h) => ({ inicio: as(h), fim: as(h + 1), disponivel: !ocupados.includes(h) }))

  it('oferece só o que cabe antes de um horário ocupado ou do fechamento', () => {
    expect(duracoesPossiveis(grade([]), as(18))).toEqual([60, 90, 120])
    expect(duracoesPossiveis(grade([19]), as(18))).toEqual([60])
    expect(duracoesPossiveis(grade([]), as(21))).toEqual([60])
  })
})

describe('mock: cliente por CPF', () => {
  it('encontra pelo CPF e impede CPF duplicado', async () => {
    const { clientes } = criarMockGateways({ latencia: 0 })
    expect((await clientes.buscarPorCpf('52998224725'))?.nome).toBe('Ana Souza')
    expect(await clientes.buscarPorCpf('00000000000')).toBeNull()
    await expect(clientes.criar({ nome: 'Outra', cpf: '52998224725', telefone: '51988887777' })).rejects.toMatchObject({
      tipo: 'conflito',
    })
  })
})

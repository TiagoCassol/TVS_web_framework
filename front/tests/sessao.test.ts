import { describe, expect, it } from 'vitest'
import { criarSessao } from '@/application/sessao'
import { criarMockAuth, GESTOR_DEMO } from '@/infrastructure/mock/mockAuth'

describe('sessão do gestor', () => {
  const nova = () => criarSessao(criarMockAuth(0))

  it('começa deslogada', () => {
    expect(nova().autenticado.value).toBe(false)
  })

  it('valida o formulário antes de chamar a autenticação', async () => {
    await expect(nova().entrar({ email: 'sem-arroba', senha: 'x' })).rejects.toMatchObject({ tipo: 'validacao' })
    await expect(nova().entrar({ email: GESTOR_DEMO.email, senha: '' })).rejects.toMatchObject({ tipo: 'validacao' })
  })

  it('recusa senha errada com mensagem genérica', async () => {
    const sessao = nova()
    await expect(sessao.entrar({ email: GESTOR_DEMO.email, senha: 'errada' })).rejects.toMatchObject({
      tipo: 'nao_autorizado',
      message: 'E-mail ou senha incorretos.',
    })
    expect(sessao.autenticado.value).toBe(false)
  })

  it('entra e sai', async () => {
    const sessao = nova()
    await sessao.entrar({ email: `  ${GESTOR_DEMO.email.toUpperCase()} `, senha: GESTOR_DEMO.senha })
    expect(sessao.autenticado.value).toBe(true)
    expect(sessao.usuario.value?.perfil).toBe('Gestor')

    await sessao.sair()
    expect(sessao.autenticado.value).toBe(false)
  })
})

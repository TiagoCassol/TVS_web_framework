import { computed, inject, readonly, ref, type InjectionKey } from 'vue'
import { validarCredenciais, type Credenciais } from '@/domain/usuario'
import { AppError } from './erros'
import type { AuthGateway } from './ports'

/**
 * Estado global de autenticação. É criado uma vez em main.ts porque
 * o guarda de rotas (fora de componentes) também precisa dele.
 */
export function criarSessao(auth: AuthGateway) {
  const usuario = ref(auth.usuarioAtual())

  async function entrar(credenciais: Credenciais) {
    const invalido = validarCredenciais(credenciais)
    if (invalido) throw new AppError('validacao', invalido)
    usuario.value = await auth.entrar({ ...credenciais, email: credenciais.email.trim() })
  }

  async function sair() {
    await auth.sair()
    usuario.value = null
  }

  return {
    usuario: readonly(usuario),
    autenticado: computed(() => usuario.value !== null),
    entrar,
    sair,
  }
}

export type Sessao = ReturnType<typeof criarSessao>

export const SESSAO: InjectionKey<Sessao> = Symbol('sessao')

export function useSessao(): Sessao {
  const sessao = inject(SESSAO)
  if (!sessao) throw new Error('Sessão não foi fornecida (app.provide(SESSAO, ...)).')
  return sessao
}

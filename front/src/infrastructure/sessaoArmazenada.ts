import type { Usuario } from '@/domain/usuario'

/*
 * Guarda o usuário logado na aba do navegador (sessionStorage), para a sessão
 * sobreviver a um F5. A senha nunca é guardada. Quando houver JWT, o token
 * também ficará aqui, e só este arquivo e o adaptador de auth mudam.
 */

const CHAVE = 'beach-tennis:sessao'

export function lerUsuario(): Usuario | null {
  try {
    const salvo = sessionStorage.getItem(CHAVE)
    return salvo ? (JSON.parse(salvo) as Usuario) : null
  } catch {
    return null // sem storage (modo privado, testes em Node)
  }
}

export function salvarUsuario(usuario: Usuario | null): void {
  try {
    if (usuario) sessionStorage.setItem(CHAVE, JSON.stringify(usuario))
    else sessionStorage.removeItem(CHAVE)
  } catch {
    // sem storage: a sessão dura até recarregar a página
  }
}

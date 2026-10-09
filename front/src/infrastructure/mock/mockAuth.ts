import { AppError } from '@/application/erros'
import type { AuthGateway } from '@/application/ports'
import type { Usuario } from '@/domain/usuario'
import { lerUsuario, salvarUsuario } from '../sessaoArmazenada'

/** Acesso de demonstração, válido somente no modo mock. */
export const GESTOR_DEMO = {
  email: 'gestor@beachtennis.local',
  senha: 'demo1234',
}

const USUARIO_DEMO: Usuario = { id: 1, nome: 'Gestor do Clube', email: GESTOR_DEMO.email, perfil: 'Gestor' }

export function criarMockAuth(latencia = 400): AuthGateway {
  const esperar = () => new Promise((r) => setTimeout(r, latencia))

  return {
    async entrar({ email, senha }) {
      await esperar()
      if (email.toLowerCase() !== GESTOR_DEMO.email || senha !== GESTOR_DEMO.senha)
        // Mensagem genérica de propósito: não revela se o e-mail existe.
        throw new AppError('nao_autorizado', 'E-mail ou senha incorretos.')
      salvarUsuario(USUARIO_DEMO)
      return { ...USUARIO_DEMO }
    },
    async sair() {
      salvarUsuario(null)
    },
    usuarioAtual: lerUsuario,
  }
}

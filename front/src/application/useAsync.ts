import { ref } from 'vue'
import { mensagemDeErro } from './erros'

/** Envolve uma operação assíncrona com estado de carregamento e erro. */
export function useAsync() {
  const carregando = ref(false)
  const erro = ref<string | null>(null)

  async function executar<T>(operacao: () => Promise<T>): Promise<T | undefined> {
    carregando.value = true
    erro.value = null
    try {
      return await operacao()
    } catch (e) {
      erro.value = mensagemDeErro(e)
      return undefined
    } finally {
      carregando.value = false
    }
  }

  return { carregando, erro, executar }
}

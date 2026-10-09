import { ref } from 'vue'
import type { NovaQuadra, Quadra } from '@/domain/quadra'
import { useGateways } from './injecao'
import { useAsync } from './useAsync'

export function useQuadras() {
  const { quadras: gateway } = useGateways()
  const { carregando, erro, executar } = useAsync()
  const quadras = ref<Quadra[]>([])

  async function carregar() {
    quadras.value = (await executar(() => gateway.listar())) ?? []
  }

  async function criar(dados: NovaQuadra): Promise<boolean> {
    if (!dados.nome.trim()) {
      erro.value = 'Nome da quadra é obrigatório.'
      return false
    }
    const criada = await executar(() => gateway.criar(dados))
    if (criada) quadras.value.push(criada)
    return !!criada
  }

  return { quadras, carregando, erro, carregar, criar }
}

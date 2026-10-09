import { ref } from 'vue'
import {
  normalizarNovoCliente,
  validarNovoCliente,
  type Cliente,
  type NovoCliente,
} from '@/domain/cliente'
import { useGateways } from './injecao'
import { useAsync } from './useAsync'

export function useClientes() {
  const { clientes: gateway } = useGateways()
  const { carregando, erro, executar } = useAsync()
  const clientes = ref<Cliente[]>([])

  async function carregar() {
    clientes.value = (await executar(() => gateway.listar())) ?? []
  }

  async function criar(dados: NovoCliente): Promise<boolean> {
    const erros = validarNovoCliente(dados)
    if (erros.length) {
      erro.value = erros.join(' ')
      return false
    }
    const criado = await executar(() => gateway.criar(normalizarNovoCliente(dados)))
    if (criado) clientes.value.push(criado)
    return !!criado
  }

  return { clientes, carregando, erro, carregar, criar }
}

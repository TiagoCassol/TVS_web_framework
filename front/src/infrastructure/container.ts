import type { Gateways } from '@/application/ports'
import { criarHttpClient } from './http/httpClient'
import { criarHttpGateways } from './http/httpGateways'
import { criarMockGateways } from './mock/mockGateways'

export type ModoApi = 'mock' | 'http'

/** Composition root: o único lugar que decide qual implementação das portas usar. */
export function criarGateways(modo: ModoApi, urlApi: string): Gateways {
  return modo === 'http' ? criarHttpGateways(criarHttpClient(urlApi)) : criarMockGateways()
}

import { inject, type InjectionKey } from 'vue'
import type { Gateways } from './ports'

export const GATEWAYS: InjectionKey<Gateways> = Symbol('gateways')

export function useGateways(): Gateways {
  const gateways = inject(GATEWAYS)
  if (!gateways) throw new Error('Gateways não foram fornecidos (app.provide(GATEWAYS, ...)).')
  return gateways
}

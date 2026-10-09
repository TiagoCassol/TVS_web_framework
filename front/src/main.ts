import { createApp } from 'vue'
import { GATEWAYS } from './application/injecao'
import { criarSessao, SESSAO } from './application/sessao'
import { criarGateways, type ModoApi } from './infrastructure/container'
import { GESTOR_DEMO } from './infrastructure/mock/mockAuth'
import App from './presentation/App.vue'
import { criarRouter } from './presentation/router'
import './presentation/estilos.css'

const modo = (import.meta.env.VITE_API_MODE ?? 'mock') as ModoApi
const urlApi = import.meta.env.VITE_API_URL ?? ''

const gateways = criarGateways(modo, urlApi)
const sessao = criarSessao(gateways.auth)

createApp(App)
  .provide(GATEWAYS, gateways)
  .provide(SESSAO, sessao)
  .provide('modoApi', modo)
  .provide('acessoDemo', modo === 'mock' ? GESTOR_DEMO : null)
  .use(criarRouter(sessao))
  .mount('#app')

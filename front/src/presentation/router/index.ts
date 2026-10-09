import { createRouter, createWebHistory } from 'vue-router'
import type { Sessao } from '@/application/sessao'

declare module 'vue-router' {
  interface RouteMeta {
    requerLogin?: boolean
  }
}

export function criarRouter(sessao: Sessao) {
  const router = createRouter({
    history: createWebHistory(),
    routes: [
      // Área pública: o cliente consulta horários e reserva.
      {
        path: '/',
        component: () => import('../layouts/LayoutPublico.vue'),
        children: [{ path: '', component: () => import('../views/publico/AgendaView.vue') }],
      },
      { path: '/entrar', component: () => import('../views/LoginView.vue') },
      // Área do gestor: exige login.
      {
        path: '/gestao',
        component: () => import('../layouts/LayoutGestao.vue'),
        meta: { requerLogin: true },
        children: [
          { path: '', redirect: '/gestao/reservas' },
          { path: 'reservas', component: () => import('../views/gestao/ReservasView.vue') },
          { path: 'quadras', component: () => import('../views/gestao/QuadrasView.vue') },
          { path: 'clientes', component: () => import('../views/gestao/ClientesView.vue') },
        ],
      },
      { path: '/:caminho(.*)*', redirect: '/' },
    ],
  })

  router.beforeEach((destino) => {
    const logado = sessao.autenticado.value
    if (destino.meta.requerLogin && !logado)
      return { path: '/entrar', query: { voltar: destino.fullPath } }
    if (destino.path === '/entrar' && logado) return '/gestao'
  })

  return router
}

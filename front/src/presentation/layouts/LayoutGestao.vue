<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useSessao } from '@/application/sessao'

const { usuario, sair } = useSessao()
const router = useRouter()

async function encerrar() {
  await sair()
  router.push('/entrar')
}
</script>

<template>
  <header class="topo">
    <strong class="marca">🎾 Gestão</strong>
    <nav>
      <RouterLink to="/gestao/reservas">Reservas</RouterLink>
      <RouterLink to="/gestao/quadras">Quadras</RouterLink>
      <RouterLink to="/gestao/clientes">Clientes</RouterLink>
    </nav>
    <div class="usuario">
      <RouterLink to="/" class="link-discreto">Ver site do cliente</RouterLink>
      <span class="avatar" :title="usuario?.email">{{ usuario?.nome.charAt(0) }}</span>
      <span class="usuario-nome">{{ usuario?.nome }}</span>
      <button type="button" class="secundario sair" @click="encerrar">Sair</button>
    </div>
  </header>
  <main class="conteudo">
    <RouterView />
  </main>
</template>

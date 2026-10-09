<script setup lang="ts">
import { inject, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useSessao } from '@/application/sessao'
import { useAsync } from '@/application/useAsync'
import AlertaErro from '../components/AlertaErro.vue'

const { entrar } = useSessao()
const { carregando, erro, executar } = useAsync()
const router = useRouter()
const route = useRoute()
// Fornecido pelo main.ts só no modo mock; null quando a API real está em uso.
const acessoDemo = inject<{ email: string; senha: string } | null>('acessoDemo', null)

const form = reactive({ email: '', senha: '' })
const mostrarSenha = ref(false)

function preencherDemo() {
  if (acessoDemo) Object.assign(form, acessoDemo)
}

async function enviar() {
  await executar(() => entrar(form))
  if (erro.value) {
    form.senha = ''
    return
  }
  // Só aceita destino interno, para o ?voltar= não virar redirecionamento externo.
  const voltar = route.query.voltar
  const interno = typeof voltar === 'string' && voltar.startsWith('/') && !voltar.startsWith('//')
  router.replace(interno ? voltar : '/gestao')
}
</script>

<template>
  <main class="login">
    <form class="cartao login-cartao" novalidate @submit.prevent="enviar">
      <p class="login-marca">🎾</p>
      <h1>Área do gestor</h1>
      <p class="login-sub">Entre para gerenciar quadras, clientes e reservas.</p>

      <label>
        E-mail
        <input
          v-model="form.email"
          type="email"
          autocomplete="username"
          placeholder="voce@clube.com"
          autofocus
        />
      </label>

      <label>
        Senha
        <span class="campo-senha">
          <input
            v-model="form.senha"
            :type="mostrarSenha ? 'text' : 'password'"
            autocomplete="current-password"
          />
          <button
            type="button"
            class="ver-senha"
            :aria-label="mostrarSenha ? 'Ocultar senha' : 'Mostrar senha'"
            @click="mostrarSenha = !mostrarSenha"
          >
            {{ mostrarSenha ? 'Ocultar' : 'Mostrar' }}
          </button>
        </span>
      </label>

      <AlertaErro :mensagem="erro" />

      <button class="login-botao" :disabled="carregando">
        {{ carregando ? 'Entrando…' : 'Entrar' }}
      </button>

      <p v-if="acessoDemo" class="nota login-demo">
        Modo demonstração.
        <button type="button" class="link" @click="preencherDemo">Usar acesso de teste</button>
      </p>

      <RouterLink to="/" class="link-discreto login-voltar">← Voltar para o site</RouterLink>
    </form>
  </main>
</template>

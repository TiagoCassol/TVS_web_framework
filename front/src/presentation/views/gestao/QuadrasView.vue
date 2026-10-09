<script setup lang="ts">
import { onMounted, reactive } from 'vue'
import { useQuadras } from '@/application/useQuadras'
import AlertaErro from '../../components/AlertaErro.vue'

const { quadras, carregando, erro, carregar, criar } = useQuadras()
const form = reactive({ nome: '' })

async function salvar() {
  if (await criar({ nome: form.nome })) form.nome = ''
}

onMounted(carregar)
</script>

<template>
  <h1>Quadras</h1>

  <form class="cartao form-linha" @submit.prevent="salvar">
    <label>
      Nome da quadra
      <input v-model="form.nome" placeholder="Ex.: Quadra 4" />
    </label>
    <button :disabled="carregando">Cadastrar</button>
  </form>

  <AlertaErro :mensagem="erro" />

  <table class="tabela">
    <thead>
      <tr><th>#</th><th>Nome</th><th>Situação</th></tr>
    </thead>
    <tbody>
      <tr v-for="q in quadras" :key="q.id">
        <td>{{ q.id }}</td>
        <td>{{ q.nome }}</td>
        <td>{{ q.ativa ? 'Ativa' : 'Inativa' }}</td>
      </tr>
      <tr v-if="!quadras.length && !carregando">
        <td colspan="3" class="vazio">Nenhuma quadra cadastrada.</td>
      </tr>
    </tbody>
  </table>
</template>

<script setup lang="ts">
import { onMounted, reactive } from 'vue'
import { useClientes } from '@/application/useClientes'
import AlertaErro from '../../components/AlertaErro.vue'
import { mascaraCpf, mascaraTelefone } from '../../formatadores'

const { clientes, carregando, erro, carregar, criar } = useClientes()
const vazio = () => ({ nome: '', cpf: '', telefone: '', email: '' })
const form = reactive(vazio())

async function salvar() {
  if (await criar({ ...form })) Object.assign(form, vazio())
}

onMounted(carregar)
</script>

<template>
  <h1>Clientes</h1>

  <form class="cartao form-linha" @submit.prevent="salvar">
    <label>Nome <input v-model="form.nome" autocomplete="off" /></label>
    <label>
      CPF
      <input
        :value="form.cpf"
        inputmode="numeric"
        placeholder="000.000.000-00"
        @input="form.cpf = mascaraCpf(($event.target as HTMLInputElement).value)"
      />
    </label>
    <label>
      Telefone
      <input
        :value="form.telefone"
        inputmode="tel"
        placeholder="(51) 99999-0000"
        @input="form.telefone = mascaraTelefone(($event.target as HTMLInputElement).value)"
      />
    </label>
    <label>E-mail (opcional) <input v-model="form.email" type="email" /></label>
    <button :disabled="carregando">Cadastrar</button>
  </form>

  <AlertaErro :mensagem="erro" />

  <table class="tabela">
    <thead>
      <tr><th>#</th><th>Nome</th><th>CPF</th><th>Telefone</th><th>E-mail</th></tr>
    </thead>
    <tbody>
      <tr v-for="c in clientes" :key="c.id">
        <td>{{ c.id }}</td>
        <td>{{ c.nome }}</td>
        <td>{{ mascaraCpf(c.cpf) }}</td>
        <td>{{ mascaraTelefone(c.telefone) }}</td>
        <td>{{ c.email || '—' }}</td>
      </tr>
      <tr v-if="!clientes.length && !carregando">
        <td colspan="5" class="vazio">Nenhum cliente cadastrado.</td>
      </tr>
    </tbody>
  </table>
</template>

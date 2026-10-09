<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useClientes } from '@/application/useClientes'
import { useQuadras } from '@/application/useQuadras'
import { useReservas } from '@/application/useReservas'
import { paraDataIso, somarMinutos } from '@/domain/datas'
import { ocupaHorario, TIPOS_CLIENTE, type TipoCliente } from '@/domain/reserva'
import AlertaErro from '../../components/AlertaErro.vue'
import GradeHorarios from '../../components/GradeHorarios.vue'
import { formatarHora, formatarMoeda, ROTULO_STATUS } from '../../formatadores'

const { quadras, carregar: carregarQuadras } = useQuadras()
const { clientes, carregar: carregarClientes } = useClientes()
const {
  reservas,
  horarios,
  carregando,
  erro,
  carregarDoDia,
  carregarDisponibilidade,
  criar,
  cancelar,
} = useReservas()

const data = ref(paraDataIso(new Date()))
const quadraId = ref<number | null>(null)
const clienteId = ref<number | null>(null)
const tipoCliente = ref<TipoCliente>('Padrao')
const duracao = ref(60)
const inicio = ref<Date | null>(null)
const sucesso = ref<string | null>(null)

const nomeQuadra = (id: number) => quadras.value.find((q) => q.id === id)?.nome ?? `#${id}`
const nomeCliente = (id: number) => clientes.value.find((c) => c.id === id)?.nome ?? `#${id}`
const podeReservar = computed(() => !!(quadraId.value && clienteId.value && inicio.value))

async function atualizar() {
  inicio.value = null
  if (quadraId.value) await carregarDisponibilidade(quadraId.value, data.value)
  await carregarDoDia(data.value)
}

async function reservar() {
  if (!podeReservar.value) return
  sucesso.value = null
  const reserva = await criar({
    clienteId: clienteId.value!,
    quadraId: quadraId.value!,
    tipoCliente: tipoCliente.value,
    inicio: inicio.value!,
    fim: somarMinutos(inicio.value!, duracao.value),
  })
  if (reserva) {
    sucesso.value = `Reserva #${reserva.id} criada — ${formatarMoeda(reserva.valorFinal)}.`
    await atualizar()
  }
}

async function cancelarReserva(id: number) {
  if (!confirm(`Cancelar a reserva #${id}?`)) return
  if (await cancelar(id)) await atualizar()
}

watch([data, quadraId], atualizar)

onMounted(async () => {
  await Promise.all([carregarQuadras(), carregarClientes()])
  quadraId.value = quadras.value[0]?.id ?? null // dispara o watch
  clienteId.value = clientes.value[0]?.id ?? null
})
</script>

<template>
  <h1>Reservas</h1>

  <section class="cartao">
    <div class="form-linha">
      <label>Data <input v-model="data" type="date" /></label>
      <label>
        Quadra
        <select v-model="quadraId">
          <option v-for="q in quadras" :key="q.id" :value="q.id">{{ q.nome }}</option>
        </select>
      </label>
    </div>

    <h2>Horário de início</h2>
    <GradeHorarios :horarios="horarios" :selecionado="inicio" @selecionar="inicio = $event" />

    <form class="form-linha" @submit.prevent="reservar">
      <label>
        Duração
        <select v-model.number="duracao">
          <option :value="60">1h</option>
          <option :value="90">1h30</option>
          <option :value="120">2h</option>
        </select>
      </label>
      <label>
        Cliente
        <select v-model="clienteId">
          <option v-for="c in clientes" :key="c.id" :value="c.id">{{ c.nome }}</option>
        </select>
      </label>
      <label>
        Tipo de cliente
        <select v-model="tipoCliente">
          <option v-for="t in TIPOS_CLIENTE" :key="t.valor" :value="t.valor">{{ t.rotulo }}</option>
        </select>
      </label>
      <button :disabled="!podeReservar || carregando">Reservar</button>
    </form>
  </section>

  <AlertaErro :mensagem="erro" />
  <p v-if="sucesso" class="alerta alerta-ok">{{ sucesso }}</p>

  <h2>Reservas do dia</h2>
  <table class="tabela">
    <thead>
      <tr>
        <th>#</th><th>Quadra</th><th>Cliente</th><th>Horário</th>
        <th>Valor</th><th>Status</th><th></th>
      </tr>
    </thead>
    <tbody>
      <tr v-for="r in reservas" :key="r.id">
        <td>{{ r.id }}</td>
        <td>{{ nomeQuadra(r.quadraId) }}</td>
        <td>{{ nomeCliente(r.clienteId) }}</td>
        <td>{{ formatarHora(r.inicio) }}–{{ formatarHora(r.fim) }}</td>
        <td>
          {{ formatarMoeda(r.valorFinal) }}
          <small v-if="r.desconto" class="riscado">{{ formatarMoeda(r.valorBruto) }}</small>
        </td>
        <td><span class="status" :data-status="r.status">{{ ROTULO_STATUS[r.status] }}</span></td>
        <td>
          <button
            v-if="ocupaHorario(r.status)"
            class="secundario"
            :disabled="carregando"
            @click="cancelarReserva(r.id)"
          >
            Cancelar
          </button>
        </td>
      </tr>
      <tr v-if="!reservas.length && !carregando">
        <td colspan="7" class="vazio">Nenhuma reserva neste dia.</td>
      </tr>
    </tbody>
  </table>
</template>

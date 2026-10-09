<script setup lang="ts">
import { computed, nextTick, reactive, ref, watch } from 'vue'
import { useReservaPublica } from '@/application/useReservaPublica'
import { paraDataIso } from '@/domain/datas'
import type { Quadra } from '@/domain/quadra'
import { duracoesPossiveis, type Reserva } from '@/domain/reserva'
import AlertaErro from '../../components/AlertaErro.vue'
import GradeHorarios from '../../components/GradeHorarios.vue'
import {
  formatarDia,
  formatarDiaSemanaCurto,
  formatarDuracao,
  formatarHora,
  formatarMoeda,
  mascaraCpf,
  mascaraTelefone,
  ROTULO_STATUS,
} from '../../formatadores'

const DIAS_A_FRENTE = 7

const { agenda, carregando, erro, carregarAgenda, reservar } = useReservaPublica()

const dias = Array.from({ length: DIAS_A_FRENTE }, (_, i) => {
  const d = new Date()
  d.setDate(d.getDate() + i)
  return d
})
const data = ref(paraDataIso(dias[0]))

const selecao = ref<{ quadra: Quadra; inicio: Date } | null>(null)
const duracao = ref(60)
const form = reactive({ nome: '', cpf: '', telefone: '', email: '' })
const confirmada = ref<{ reserva: Reserva; quadra: Quadra } | null>(null)
const painel = ref<HTMLElement | null>(null)

const duracoes = computed(() => {
  if (!selecao.value) return []
  const item = agenda.value.find((a) => a.quadra.id === selecao.value!.quadra.id)
  return item ? duracoesPossiveis(item.horarios, selecao.value.inicio) : []
})

const livres = (quadraId: number) =>
  agenda.value.find((a) => a.quadra.id === quadraId)?.horarios.filter((h) => h.disponivel).length ?? 0

async function selecionar(quadra: Quadra, inicio: Date) {
  confirmada.value = null
  selecao.value = { quadra, inicio }
  duracao.value = duracoes.value[0] ?? 60
  await nextTick()
  painel.value?.scrollIntoView({ behavior: 'smooth', block: 'nearest' })
}

async function confirmar() {
  if (!selecao.value) return
  const { quadra, inicio } = selecao.value
  const reserva = await reservar({ ...form }, quadra.id, inicio, duracao.value)
  if (!reserva) return
  confirmada.value = { reserva, quadra }
  selecao.value = null
  await carregarAgenda(data.value)
}

watch(
  data,
  (dia) => {
    selecao.value = null
    carregarAgenda(dia)
  },
  { immediate: true },
)
</script>

<template>
  <section class="chamada">
    <h1>Reserve sua quadra</h1>
    <p>Escolha o dia, toque em um horário livre e informe seus dados. Leva menos de um minuto.</p>
  </section>

  <div class="dias" role="tablist" aria-label="Dia da reserva">
    <button
      v-for="d in dias"
      :key="paraDataIso(d)"
      type="button"
      role="tab"
      class="dia"
      :class="{ ativo: data === paraDataIso(d) }"
      :aria-selected="data === paraDataIso(d)"
      @click="data = paraDataIso(d)"
    >
      <small>{{ formatarDiaSemanaCurto(d) }}</small>
      <strong>{{ d.getDate() }}</strong>
    </button>
  </div>

  <div class="agenda-layout">
    <div class="agenda">
      <p v-if="carregando && !agenda.length" class="vazio">Carregando horários…</p>

      <article v-for="item in agenda" :key="item.quadra.id" class="cartao">
        <header class="cartao-titulo">
          <h2>{{ item.quadra.nome }}</h2>
          <span class="contador">{{ livres(item.quadra.id) }} livres</span>
        </header>
        <GradeHorarios
          :horarios="item.horarios"
          :selecionado="selecao?.quadra.id === item.quadra.id ? selecao.inicio : null"
          @selecionar="selecionar(item.quadra, $event)"
        />
      </article>

      <p v-if="!carregando && !agenda.length" class="vazio">Nenhuma quadra disponível no momento.</p>
    </div>

    <aside ref="painel" class="painel">
      <div v-if="confirmada" class="cartao confirmacao">
        <h2>Reserva solicitada ✔</h2>
        <dl>
          <dt>Código</dt><dd>#{{ confirmada.reserva.id }}</dd>
          <dt>Quadra</dt><dd>{{ confirmada.quadra.nome }}</dd>
          <dt>Dia</dt><dd>{{ formatarDia(confirmada.reserva.inicio) }}</dd>
          <dt>Horário</dt>
          <dd>{{ formatarHora(confirmada.reserva.inicio) }}–{{ formatarHora(confirmada.reserva.fim) }}</dd>
          <dt>Valor</dt><dd>{{ formatarMoeda(confirmada.reserva.valorFinal) }}</dd>
          <dt>Situação</dt><dd>{{ ROTULO_STATUS[confirmada.reserva.status] }}</dd>
        </dl>
        <p class="nota">Guarde o código da reserva. O clube entrará em contato pelo telefone informado.</p>
      </div>

      <form v-else-if="selecao" class="cartao" @submit.prevent="confirmar">
        <h2>Sua reserva</h2>
        <p class="resumo">
          <strong>{{ selecao.quadra.nome }}</strong><br />
          {{ formatarDia(selecao.inicio) }}, às {{ formatarHora(selecao.inicio) }}
        </p>

        <label>
          Duração
          <select v-model.number="duracao">
            <option v-for="m in duracoes" :key="m" :value="m">{{ formatarDuracao(m) }}</option>
          </select>
        </label>
        <label>Nome completo <input v-model="form.nome" autocomplete="name" required /></label>
        <label>
          CPF
          <input
            :value="form.cpf"
            inputmode="numeric"
            placeholder="000.000.000-00"
            required
            @input="form.cpf = mascaraCpf(($event.target as HTMLInputElement).value)"
          />
        </label>
        <label>
          Telefone / WhatsApp
          <input
            :value="form.telefone"
            inputmode="tel"
            autocomplete="tel"
            placeholder="(51) 99999-0000"
            required
            @input="form.telefone = mascaraTelefone(($event.target as HTMLInputElement).value)"
          />
        </label>
        <label>
          E-mail (opcional)
          <input v-model="form.email" type="email" autocomplete="email" />
        </label>

        <AlertaErro :mensagem="erro" />

        <div class="acoes">
          <button type="button" class="secundario" @click="selecao = null">Voltar</button>
          <button :disabled="carregando || !duracoes.length">Confirmar reserva</button>
        </div>
        <p class="nota">Usamos seus dados apenas para identificar a reserva e entrar em contato.</p>
      </form>

      <div v-else class="cartao dica">
        <h2>Como funciona</h2>
        <ol>
          <li>Escolha o dia acima.</li>
          <li>Toque em um horário livre de uma quadra.</li>
          <li>Informe nome, CPF e telefone e confirme.</li>
        </ol>
        <AlertaErro :mensagem="erro" />
      </div>
    </aside>
  </div>
</template>

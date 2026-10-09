<script setup lang="ts">
import type { HorarioDisponivel } from '@/domain/reserva'
import { formatarHora } from '../formatadores'

defineProps<{ horarios: HorarioDisponivel[]; selecionado: Date | null }>()
defineEmits<{ selecionar: [inicio: Date] }>()
</script>

<template>
  <div class="grade">
    <button
      v-for="h in horarios"
      :key="h.inicio.getTime()"
      type="button"
      class="slot"
      :class="{ ativo: selecionado?.getTime() === h.inicio.getTime() }"
      :disabled="!h.disponivel"
      @click="$emit('selecionar', h.inicio)"
    >
      {{ formatarHora(h.inicio) }}
    </button>
    <p v-if="!horarios.length" class="vazio">Nenhum horário para exibir.</p>
  </div>
</template>

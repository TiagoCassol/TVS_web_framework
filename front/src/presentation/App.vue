<script setup lang="ts">
import { inject } from 'vue'

const modoApi = inject<string>('modoApi')
</script>

<template>
  <RouterView />
  <span v-if="modoApi === 'mock'" class="selo" title="Dados em memória — nada é enviado ao backend">
    modo mock
  </span>
</template>

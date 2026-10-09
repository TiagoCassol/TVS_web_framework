<script setup lang="ts">
defineProps<{ mensagem: string | null }>()
</script>

<template>
  <p v-if="mensagem" class="alerta alerta-erro" role="alert">{{ mensagem }}</p>
</template>

<template>
  <div class="status-label" :class="statusClass">
    <div class="status-circle" :class="circleClass"></div>
    <span class="status-text">{{ statusText }}</span>
  </div>
</template>

<script setup lang="ts">
import extensionClient from '@/BrowserExtensionClient';
import { StatusType } from '@/types';
import { HubConnectionState } from '@microsoft/signalr';

const signalrStatus = computed(() => {
  const state = extensionClient.connectionState;
  if (state === HubConnectionState.Connected) return StatusType.WORKING;
  return StatusType.MAINTENANCE;
});

const statusClass = computed(() =>
  signalrStatus.value === StatusType.WORKING
    ? 'status-working'
    : 'status-maintenance'
);

const circleClass = computed(() =>
  signalrStatus.value === StatusType.WORKING ? 'green' : 'yellow'
);

const statusText = computed(() =>
  signalrStatus.value === StatusType.WORKING ? 'Работаем' : 'Ведем тех. работы'
);
</script>

<style scoped>
.status-label {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  font-weight: 500;
}

.status-circle {
  width: 12px;
  height: 12px;
  border-radius: 50%;
  flex-shrink: 0;
}

.status-circle.green {
  background-color: #059669;
  box-shadow: 0 0 0 2px rgba(74, 222, 128, 0.3);
}

.status-circle.yellow {
  background-color: #f8f671;
  box-shadow: 0 0 0 2px rgba(248, 113, 113, 0.3);
}
</style>

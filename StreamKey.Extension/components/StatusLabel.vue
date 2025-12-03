<template>
  <div class="status-label" :class="statusClass">
    <div class="status-circle" :class="circleClass"></div>
    <span class="status-text">{{ statusText }}</span>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { sendMessage } from '@/messaging';
import { StatusType } from '@/types';
import { HubConnectionState } from '@microsoft/signalr';

const signalrStatus = ref<StatusType>(StatusType.MAINTENANCE);

async function updateStatus() {
  try {
    const state = await sendMessage('getConnectionState');
    console.log('state', state);
    signalrStatus.value =
      state === HubConnectionState.Connected
        ? StatusType.WORKING
        : StatusType.MAINTENANCE;
  } catch (error) {
    signalrStatus.value = StatusType.MAINTENANCE;
  }
}

onMounted(async () => {
  await updateStatus();
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

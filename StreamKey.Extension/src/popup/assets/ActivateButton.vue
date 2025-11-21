<template>
<button class="activate-button">
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="margin-right:6px;">
    <polyline points="20 6 9 17 4 12"></polyline>
  </svg>
  Активировать
</button>
</template>

<style>
.activate-button {
  display: flex;
  align-items: center;
  background: #0e9472;
  border: none;
  border-radius: 7px;
  padding: 0 10px;
  height: 28px;
  color: white;
  font-size: 12px;
  transition: all 0.2s ease-in-out;
}

.activate-button:hover {
  background: #10a882 !important;
  cursor: pointer;
}

.activate-button:active {
  background: #0b755b !important;
}
</style>

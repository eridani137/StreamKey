import { createApp } from 'vue';
import '../assets/styles/global.css';
import PopupApp from './PopupApp.vue';

createApp(PopupApp).mount('#app');

console.log('Popup приложение инициализировано');

import { createApp } from 'vue';
import '../assets/styles/global.css';
import MainApp from './MainApp.vue';

createApp(MainApp).mount('#app');

console.log('Popup приложение инициализировано');

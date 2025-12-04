import { TelegramUser } from '@/types';
import { defineStore } from 'pinia';

export const useUserStore = defineStore('user', {
  state: () => ({
    profile: null as TelegramUser | null,
  }),
  getters: {
    isLoggedIn: (state) => !!state.profile,
  },
  actions: {
    setProfile(user: TelegramUser) {
      this.profile = user;
    },
    clearProfile() {
      this.profile = null;
    },
  },
});
<template>
  <div class="popup">
    <div class="circle-logo" @click="onLogoClick">
      <template v-if="!showVideo">
        <Logo/>
      </template>
      <template v-else>
        <video
            ref="videoPlayer"
            class="logo-video"
            :src="currentVideo"
            :loop="isVideoLooped"
            autoplay
            muted
            playsinline
            @ended="onVideoEnded"
        ></video>
      </template>
    </div>
    <h1 class="title">STREAM KEY</h1>
    <p class="subtitle">Твой ключ от мира стриминга</p>
    <button class="tg-button" @click="openTelegram">
      <svg
          width="24"
          height="24"
          viewBox="0 0 24 24"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
      >
        <path
            d="M12 2C6.48 2 2 6.48 2 12C2 17.52 6.48 22 12 22C17.52 22 22 17.52 22 12C22 6.48 17.52 2 12 2ZM16.64 8.8C16.49 10.38 15.84 14.22 15.51 15.99C15.37 16.74 15.09 16.99 14.83 17.02C14.25 17.07 13.81 16.64 13.25 16.27C12.37 15.69 11.87 15.33 11.02 14.77C10.03 14.12 10.67 13.76 11.24 13.18C11.39 13.03 13.95 10.7 14 10.49C14.0069 10.4582 14.006 10.4252 13.9973 10.3938C13.9886 10.3624 13.9724 10.3337 13.95 10.31C13.89 10.26 13.81 10.28 13.74 10.29C13.65 10.31 12.25 11.24 9.52 13.08C9.12 13.35 8.76 13.49 8.44 13.48C8.08 13.47 7.4 13.28 6.89 13.11C6.26 12.91 5.77 12.8 5.81 12.45C5.83 12.27 6.08 12.09 6.55 11.9C9.47 10.63 11.41 9.79 12.38 9.39C15.16 8.23 15.73 8.03 16.11 8.03C16.19 8.03 16.38 8.05 16.5 8.15C16.6 8.23 16.63 8.34 16.64 8.42C16.63 8.48 16.65 8.66 16.64 8.8Z"
            fill="#9A9A9A"
        />
      </svg>
    </button>
  </div>
</template>

<script>
import { ref, onMounted, computed } from 'vue';
import Logo from './Logo.vue';
import EnableVideo from '/assets/enable.webm';
import EnabledVideo from '/assets/enabled.webm';
import DisableVideo from '/assets/disable.webm';

export default {
  name: 'PopupApp',
  components: {
    Logo
  },
  setup() {
    const currentVideo = ref(undefined);
    const isEnabled = ref(true);
    const isLoading = ref(false);

    // Определяем доступный API (Firefox или Chrome)
    const extensionAPI = typeof browser !== 'undefined' ? browser : chrome;

    const showVideo = computed(() => {
      return currentVideo.value !== undefined;
    });

    const isVideoLooped = computed(() => {
      return currentVideo.value === EnabledVideo;
    });

    function onVideoEnded() {
      if (currentVideo.value === EnableVideo) {
        currentVideo.value = EnabledVideo;
        isLoading.value = false;
      }
      else if (currentVideo.value === DisableVideo) {
        currentVideo.value = undefined;
        isLoading.value = false;
      }
    }

    async function loadStoredState() {
      try {
        const result = await extensionAPI.storage.local.get(['streamKeyEnabled']);
        if (result.streamKeyEnabled !== undefined) {
          isEnabled.value = result.streamKeyEnabled;
        }
      } catch (error) {
        console.error('Ошибка загрузки состояния:', error);
      }
    }

    async function saveStoredState() {
      try {
        await extensionAPI.storage.local.set({
          streamKeyEnabled: isEnabled.value
        });
      } catch (error) {
        console.error('Ошибка сохранения состояния:', error);
      }
    }

    async function enableRuleset() {
      try {
        if (extensionAPI.declarativeNetRequest && extensionAPI.declarativeNetRequest.updateEnabledRulesets) {
          await extensionAPI.declarativeNetRequest.updateEnabledRulesets({
            enableRulesetIds: ['ruleset_1'],
            disableRulesetIds: [],
          });
          console.log('Правила перенаправления активированы');
        } else {
          console.warn('declarativeNetRequest не поддерживается в этом браузере');
        }
      } catch (err) {
        console.error('Ошибка активации правил:', err);
      }
    }

    async function disableRuleset() {
      try {
        if (extensionAPI.declarativeNetRequest && extensionAPI.declarativeNetRequest.updateEnabledRulesets) {
          await extensionAPI.declarativeNetRequest.updateEnabledRulesets({
            enableRulesetIds: [],
            disableRulesetIds: ['ruleset_1'],
          });
          console.log('Правила перенаправления деактивированы');
        } else {
          console.warn('declarativeNetRequest не поддерживается в этом браузере');
        }
      } catch (err) {
        console.error('Ошибка деактивации правил:', err);
      }
    }

    async function onLogoClick() {
      if (isLoading.value) {
        return;
      }

      isLoading.value = true;

      try {
        if (!isEnabled.value) {
          // Включаем
          await enableRuleset();
          isEnabled.value = true;
          currentVideo.value = EnableVideo;
        } else {
          // Выключаем
          await disableRuleset();
          isEnabled.value = false;
          currentVideo.value = DisableVideo;
        }
        await saveStoredState();
      } catch (error) {
        console.error('Ошибка при переключении состояния:', error);
        isLoading.value = false;
      }
    }

    function openTelegram() {
      if (typeof browser !== 'undefined') {
        // Firefox
        browser.tabs.create({ url: 'https://t.me/streamkey' });
      } else {
        // Chrome
        window.open('https://t.me/streamkey', '_blank');
      }
    }

    onMounted(async () => {
      console.log('Popup запущен в:', typeof browser !== 'undefined' ? 'Firefox' : 'Chrome');
      
      await loadStoredState();
      if (isEnabled.value) {
        await enableRuleset();
        currentVideo.value = EnabledVideo;
      } else {
        await disableRuleset();
        currentVideo.value = undefined;
      }
    });

    return {
      showVideo,
      currentVideo,
      isEnabled,
      isLoading,
      isVideoLooped,
      onLogoClick,
      openTelegram,
      onVideoEnded
    };
  }
};
</script>

<style>
html,
body {
  margin: 0;
  padding: 0;
  width: 100%;
  height: 100%;
  user-select: none;
}

.popup {
  width: 289px;
  height: 386px;
  padding: 16px 8px;
  background: radial-gradient(ellipse at top, #271f2c 0%, #040715 100%);
  color: #fff;
  font-family: 'Montserrat', sans-serif;
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  box-sizing: border-box;
}

.circle-logo {
  width: 164px;
  height: 164px;
  border-radius: 50%;
  overflow: hidden;
  cursor: pointer;
  transition: width 0.3s ease, height 0.3s ease;
}

.logo-video {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}

@font-face {
  font-family: 'DIN Pro';
  src: url('./fonts/DINPro-BlackItalic.otf') format('opentype');
  font-weight: 900;
  font-style: italic;
  font-display: swap;
}

.title {
  margin-top: 24px;
  margin-bottom: 4px;
  text-align: center;

  font-family: 'DIN Pro', sans-serif;
  font-style: italic;
  font-weight: 900;
  font-size: 26px;
  line-height: 43px;

  color: #ffffff;
  text-transform: uppercase;
}

@font-face {
  font-family: 'Manrope';
  src: url('./fonts/Manrope-Regular.ttf') format('truetype');
  font-weight: 400;
  font-style: normal;
  font-display: swap;
}

@font-face {
  font-family: 'Manrope';
  src: url('./fonts/Manrope-SemiBold.ttf') format('truetype');
  font-weight: 600;
  font-style: normal;
  font-display: swap;
}

.subtitle {
  margin-top: 0;
  text-align: center;

  font-family: 'Manrope', sans-serif;
  font-style: normal;
  font-weight: 600;
  font-size: 14px;
  line-height: 19px;

  color: #9a9a9a;
}

.tg-button {
  margin-top: auto;
  background: none;
  border: none;
  cursor: pointer;
  padding: 12px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.2s;
}

.tg-button:hover {
  background: rgba(255, 255, 255, 0.1);
}

.tg-button svg {
  display: block;
}
</style>

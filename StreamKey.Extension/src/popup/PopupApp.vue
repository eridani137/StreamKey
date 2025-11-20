<template>
  <div class="popup-window">
    <div class="circle-logo" @click="onLogoClick">
      <template v-if="!showVideo">
        <StreamKeyLogo />
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
    <div class="subscribe-status">
      <p>1440p:</p>
    </div>
    <h1 class="stream-key-title">STREAM KEY</h1>
    <p class="stream-key-subtitle">Твой ключ от мира стриминга</p>
    <button class="telegram-button" @click="openTelegram">
      <TelegramCircle />
    </button>
  </div>
</template>

<script>
import { ref, onMounted, computed } from 'vue';
import StreamKeyLogo from './assets/StreamKeyLogo.vue';
import TelegramCircle from './assets/TelegramCircle.vue'
import EnableVideo from '/assets/enable.webm';
import EnabledVideo from '/assets/enabled.webm';
import DisableVideo from '/assets/disable.webm';

export default {
  name: 'PopupApp',
  components: {
    StreamKeyLogo,
    TelegramCircle
  },
  setup() {
    const currentVideo = ref(undefined);
    const isEnabled = ref(true);
    const isLoading = ref(false);

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
      } else if (currentVideo.value === DisableVideo) {
        currentVideo.value = undefined;
        isLoading.value = false;
      }
    }

    async function loadStoredState() {
      try {
        const result = await extensionAPI.storage.local.get([
          'streamKeyEnabled',
        ]);
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
          streamKeyEnabled: isEnabled.value,
        });
      } catch (error) {
        console.error('Ошибка сохранения состояния:', error);
      }
    }

    async function enableRuleset() {
      try {
        if (
          extensionAPI.declarativeNetRequest &&
          extensionAPI.declarativeNetRequest.updateEnabledRulesets
        ) {
          await extensionAPI.declarativeNetRequest.updateEnabledRulesets({
            enableRulesetIds: ['ruleset_1'],
            disableRulesetIds: [],
          });
          console.log('Правила перенаправления активированы');
        } else {
          console.warn(
            'declarativeNetRequest не поддерживается в этом браузере'
          );
        }
      } catch (err) {
        console.error('Ошибка активации правил:', err);
      }
    }

    async function disableRuleset() {
      try {
        if (
          extensionAPI.declarativeNetRequest &&
          extensionAPI.declarativeNetRequest.updateEnabledRulesets
        ) {
          await extensionAPI.declarativeNetRequest.updateEnabledRulesets({
            enableRulesetIds: [],
            disableRulesetIds: ['ruleset_1'],
          });
          console.log('Правила перенаправления деактивированы');
        } else {
          console.warn(
            'declarativeNetRequest не поддерживается в этом браузере'
          );
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
      console.log(
        'Popup запущен в:',
        typeof browser !== 'undefined' ? 'Firefox' : 'Chrome'
      );

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
      onVideoEnded,
    };
  },
};
</script>

<style>
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

.stream-key-title {
  margin-top: 10px;
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

.stream-key-subtitle {
  margin-top: 0;
  text-align: center;

  font-family: 'Manrope', sans-serif;
  font-style: normal;
  font-weight: 600;
  font-size: 14px;
  line-height: 19px;

  color: #9a9a9a;
}

.telegram-button {
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

.telegram-button:hover {
  background: rgba(255, 255, 255, 0.1);
}

.telegram-button svg {
  display: block;
}

.subscribe-status {
  margin-top: 0;
  text-align: center;

  font-family: 'Manrope', sans-serif;
  font-style: normal;
  font-weight: 600;
  font-size: 14px;
  line-height: 19px;

  color: #9a9a9a;
}

</style>

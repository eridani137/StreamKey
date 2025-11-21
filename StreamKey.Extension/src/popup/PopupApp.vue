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
      <p>
        1440p: <span :class="status1440pColorClass">{{ status1440pText }}</span>
      </p>
    </div>
    <div>
      <template v-if="is1440pActive">
        <QAButton @click="openQA" />
      </template>
      <template v-else>
        <ActivateButton @click="openTelegramAuthentication" />
      </template>
    </div>
    <h1 class="stream-key-title">STREAM KEY</h1>
    <p class="stream-key-subtitle">Твой ключ от мира стриминга</p>
    <div class="authentication-block">
      <span>*перейдите в меню выбора качества</span>
      <button class="telegram-button" @click="openTelegram">
        <TelegramCircle />
      </button>
    </div>
  </div>
</template>

<script>
import { ref, onMounted, computed } from 'vue';
import { CONFIG } from '../config';
import StreamKeyLogo from './assets/StreamKeyLogo.vue';
import TelegramCircle from './assets/TelegramCircle.vue';
import QAButton from './assets/QAButton.vue';
import ActivateButton from './assets/ActivateButton.vue';
import EnableVideo from '/assets/enable.webm';
import EnabledVideo from '/assets/enabled.webm';
import DisableVideo from '/assets/disable.webm';

export default {
  name: 'PopupApp',
  components: {
    StreamKeyLogo,
    TelegramCircle,
    QAButton,
    ActivateButton,
  },
  setup() {
    const api = typeof browser !== 'undefined' ? browser : chrome;

    const currentVideo = ref(undefined);
    const isEnabled = ref(true);
    const isLoading = ref(false);
    const is1440pActive = ref(false);

    const extensionAPI = typeof browser !== 'undefined' ? browser : chrome;

    const showVideo = computed(() => {
      return currentVideo.value !== undefined;
    });

    const isVideoLooped = computed(() => {
      return currentVideo.value === EnabledVideo;
    });

    const status1440pText = computed(() => {
      return is1440pActive.value ? 'Активирован' : 'Не активирован*';
    });

    const status1440pColorClass = computed(() => {
      return is1440pActive.value ? 'status-active' : 'status-inactive';
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

    function openTelegramChannel() {
      api.tabs.create({ url: CONFIG.telegramChannelUrl });
    }

    function openTelegramAuthentication() {
      api.tabs.create({ url: CONFIG.extensionAuthorizationUrl });
    }

    function openQA() {
      api.tabs.create({ url: CONFIG.streamKeyQAUrl });
    }

    onMounted(async () => {
      console.log('Popup запущен в:', api);

      await loadStoredState();
      if (isEnabled.value) {
        await enableRuleset();
        currentVideo.value = EnabledVideo;
      } else {
        await disableRuleset();
        currentVideo.value = undefined;
      }

      api.cookies.get({ url: CONFIG.oauthTelegramUrl, name: 'stel_acid' }, (cookie) => {
        if (chrome.runtime?.lastError) {
          is1440pActive.value = false;
          return;
        }
        is1440pActive.value = true;
      });
    });

    return {
      showVideo,
      currentVideo,
      isEnabled,
      isLoading,
      isVideoLooped,
      onLogoClick,
      openTelegram: openTelegramChannel,
      openTelegramAuthentication,
      openQA,
      onVideoEnded,
      is1440pActive,
      status1440pText,
      status1440pColorClass,
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
  margin-top: 16px;
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

.status-active {
  color: rgb(14, 148, 114);
}

.status-inactive {
  color: rgb(219, 75, 70);
}

.telegram-button {
  margin-top: auto;
  background: none;
  border: none;
  cursor: pointer;
  padding: 8px 8px 8px 8px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.2s, transform 0.2s;
}

.telegram-button:hover {
  background: rgba(255, 255, 255, 0.1);
  transform: scale(1.05);
}

.telegram-button:active {
  transform: scale(0.95);
  background: rgba(255, 255, 255, 0.2);
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

.authentication-block {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 34px;
  padding: 8px;
  /* width: 80%; */
  margin-left: 14px;

  font-family: 'Manrope', sans-serif;
  font-style: normal;
  font-size: 10px;
}
</style>

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
        1440p:
        <span :class="telegramStatusColorClass">{{ telegramStatusText }}</span>
      </p>
    </div>
    <div>
      <template v-if="telegramStatus == 0 || telegramStatus == 1">
        <ActivateButton @click="openTelegramAuthentication" />
      </template>
      <template v-else>
        <QAButton @click="openQA" />
      </template>
    </div>
    <h1 class="stream-key-title">STREAM KEY</h1>
    <p class="stream-key-subtitle">Твой ключ от мира стриминга</p>
    <div class="authentication-block">
      <template v-if="!tgUser">
        <span>*перейдите в меню выбора качества</span>
      </template>
      <template v-else>
        <div class="profile-block">
          <div class="avatar">
            <img :src="tgUser.photo_url" alt="avatar" class="avatar-img" />
          </div>
          <div class="info">
            <div class="nickname">{{ tgUser.username }}</div>
            <div class="id">{{ tgUser.id }}</div>
          </div>
        </div>
      </template>
      <button class="telegram-button" @click="openTelegram">
        <TelegramCircle />
      </button>
    </div>
  </div>
</template>

<script>
import { ref, onMounted, computed } from 'vue';
import { CONFIG } from '../config';
import * as utils from '../utils';

import StreamKeyLogo from './assets/StreamKeyLogo.vue';
import TelegramCircle from './assets/TelegramCircle.vue';
import QAButton from './assets/QAButton.vue';
import ActivateButton from './assets/ActivateButton.vue';

import EnableVideo from '/assets/enable.webm';
import EnabledVideo from '/assets/enabled.webm';
import DisableVideo from '/assets/disable.webm';

export default {
  name: 'MainApp',
  components: {
    StreamKeyLogo,
    TelegramCircle,
    QAButton,
    ActivateButton,
  },
  setup() {
    const currentVideo = ref(undefined);
    const isEnabled = ref(true);
    const isLoading = ref(false);
    const telegramStatus = ref(0);

    const tgUser = ref(undefined);

    const showVideo = computed(() => {
      return currentVideo.value !== undefined;
    });

    const isVideoLooped = computed(() => {
      return currentVideo.value === EnabledVideo;
    });

    const telegramStatusText = computed(() => {
      if (telegramStatus.value == 0) {
        return 'Не активирован*';
      } else if (telegramStatus.value == 1) {
        return 'Нужно подписаться на канал';
      } else {
        return 'Активирован';
      }
    });

    const telegramStatusColorClass = computed(() => {
      if (telegramStatus.value == 0 || telegramStatus.value == 1) {
        return 'status-inactive';
      } else {
        return 'status-active';
      }
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

    async function onLogoClick() {
      if (isLoading.value) {
        return;
      }

      isLoading.value = true;

      try {
        if (!isEnabled.value) {
          // Включаем
          await utils.enableRuleset();
          isEnabled.value = true;
          currentVideo.value = EnableVideo;
        } else {
          // Выключаем
          await utils.disableRuleset();
          isEnabled.value = false;
          currentVideo.value = DisableVideo;
        }
        await utils.saveState(CONFIG.extensionStateKeyName, isEnabled.value);
      } catch (error) {
        console.error('Ошибка при переключении состояния:', error);
        isLoading.value = false;
      }
    }

    function openTelegramChannel() {
      utils.api.tabs.create({ url: CONFIG.telegramChannelUrl });
    }

    function openTelegramAuthentication() {
      utils.api.tabs.create({ url: CONFIG.extensionAuthorizationUrl });
    }

    function openQA() {
      utils.api.tabs.create({ url: CONFIG.streamKeyQAUrl });
    }

    onMounted(async () => {
      isEnabled.value = await utils.loadState(CONFIG.extensionStateKeyName);
      if (isEnabled.value) {
        await utils.enableRuleset();
        currentVideo.value = EnabledVideo;
      } else {
        await utils.disableRuleset();
        currentVideo.value = undefined;
      }

      tgUser.value = await utils.api.runtime.sendMessage({ type: "GET_USER_PROFILE" });
      if (tgUser.value) {
        if (tgUser.value.is_chat_member) {
          telegramStatus.value = 2;
        } else {
          telegramStatus.value = 1;
        }
      } else {
        telegramStatus.value = 0;
      }
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
      telegramStatus,
      telegramStatusText,
      telegramStatusColorClass,
      tgUser,
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
  margin-top: 8px;
  padding: 8px;
  /* width: 80%; */
  margin-left: 14px;

  font-family: 'Manrope', sans-serif;
  font-style: normal;
  font-size: 10px;
}

.profile-block {
  display: flex;
  align-items: center;
  background: #181725;
  padding: 0px 16px 0px 0px;
  border-radius: 12px;
  width: fit-content;
}

.avatar {
  width: 48px;
  height: 48px;
  background: #8562b7;
  border-radius: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 12px;
  box-shadow: 0 2px 8px #0002;
}

.avatar-img {
  width: 48px;
  height: 48px;
  border-radius: 16px;
  object-fit: cover;
  background: #8562b7;
}

.info .nickname {
  font-size: 12px;
  color: #fff;
  font-weight: 500;
}

.info .id {
  font-size: 12px;
  color: #888;
  margin-top: 2px;
}
</style>

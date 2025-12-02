<template>
  <div class="popup-window">
    <!-- <div class="status-container">
      <StatusLabel />
    </div> -->
    <div class="circle-logo" @click="handleLogoClick">
      <StreamKeyLogo v-if="!showVideo" />
      <video
        v-else
        ref="videoPlayer"
        class="logo-video"
        :src="currentVideo"
        :loop="isVideoLooped"
        autoplay
        muted
        playsinline
        @ended="handleVideoEnd"
      />
    </div>

    <div class="subscribe-status">
      <p>
        1440p:
        <span :class="statusColorClass">{{ statusText }}</span>
      </p>
    </div>

    <ActivateButton
      v-if="telegramStatus === TelegramStatus.NotAuthorized"
      label="Подключить 1440p"
      @click="openTelegramAuthentication"
    />
    <ActivateButton
      v-else-if="telegramStatus === TelegramStatus.NotMember"
      label="Подписаться на канал"
      @click="openTelegramChannel"
    />
    <QAButton v-else label="Не работает!" @click="openQA" />

    <h1 class="stream-key-title">STREAM KEY</h1>
    <p class="stream-key-subtitle">Твой ключ от мира стриминга</p>

    <div class="authentication-block">
      <span v-if="!telegramUser">*перейдите в меню выбора качества</span>
      <div v-else class="profile-block">
        <div class="avatar">
          <img
            v-if="telegramUser.photo_url"
            :src="telegramUser.photo_url"
            alt="avatar"
            class="avatar-img"
          />
        </div>
        <div class="info">
          <div class="nickname">{{ telegramUser.username }}</div>
          <div class="id">{{ telegramUser.id }}</div>
        </div>
      </div>

      <button
        class="telegram-button"
        @click="openTelegramChannel"
        aria-label="Open Telegram"
      >
        <TelegramCircle />
      </button>
    </div>
  </div>
</template>

<script lang="ts" setup>
import { ref, computed, onMounted } from 'vue';
import Config from '@/config';
import { StatusType, TelegramStatus, TelegramUser } from '@/types';

import StreamKeyLogo from '@/components/StreamKeyLogo.vue';
import TelegramCircle from '@/components/TelegramCircle.vue';
import QAButton from '@/components/QAButton.vue';
import ActivateButton from '@/components/ActivateButton.vue';
import StatusLabel from '@/components/StatusLabel.vue';

import EnableVideo from '~/assets/enable.webm';
import EnabledVideo from '~/assets/enabled.webm';
import DisableVideo from '~/assets/disable.webm';
import { loadTwitchRedirectRules, removeAllDynamicRules } from '@/rules';

const currentVideo = ref<string | undefined>(undefined);
const isEnabled = ref(false);
const isLoading = ref(false);
const telegramStatus = ref<TelegramStatus>(TelegramStatus.NotAuthorized);
const telegramUser = ref<TelegramUser | undefined>(undefined);

const showVideo = computed(() => currentVideo.value !== undefined);
const isVideoLooped = computed(() => currentVideo.value === EnabledVideo);

const STATUS_MAP = {
  [TelegramStatus.NotAuthorized]: {
    text: 'Не активирован*',
    class: 'status-inactive',
  },
  [TelegramStatus.NotMember]: {
    text: 'Нужно подписаться',
    class: 'status-inactive',
  },
  [TelegramStatus.Ok]: { text: 'Активирован', class: 'status-active' },
} as const;

const statusText = computed(() => STATUS_MAP[telegramStatus.value]?.text || '');
const statusColorClass = computed(
  () => STATUS_MAP[telegramStatus.value]?.class || ''
);

function handleVideoEnd() {
  if (currentVideo.value === EnableVideo) {
    currentVideo.value = EnabledVideo;
  } else if (currentVideo.value === DisableVideo) {
    currentVideo.value = undefined;
  }
  isLoading.value = false;
}

async function handleLogoClick() {
  if (isLoading.value) return;

  isLoading.value = true;

  try {
    const newState = !isEnabled.value;

    if (newState) {
      await loadTwitchRedirectRules();
      currentVideo.value = EnableVideo;
    } else {
      await removeAllDynamicRules();
      currentVideo.value = DisableVideo;
    }

    isEnabled.value = newState;
    await storage.setItem(Config.keys.extensionState, newState);
  } catch (error) {
    console.error('Ошибка при переключении состояния:', error);
    isLoading.value = false;
  }
}

function openTelegramChannel() {
  browser.tabs.create({ url: Config.urls.telegramChannelUrl });
}

function openTelegramAuthentication() {
  browser.tabs.create({ url: Config.urls.extensionAuthorizationUrl });
}

function openQA() {
  browser.tabs.create({ url: Config.urls.streamKeyQAUrl });
}

async function initializeExtension() {
  const savedState = await storage.getItem<boolean>(Config.keys.extensionState);
  isEnabled.value = savedState ?? false;

  if (isEnabled.value) {
    await loadTwitchRedirectRules();
    currentVideo.value = EnabledVideo;
  } else {
    await removeAllDynamicRules();
  }
}

async function loadUserProfile() {
  try {
    const userData = await storage.getItem<TelegramUser>(
      Config.keys.userProfile
    );

    console.log('Загрузка профиля', userData);

    if (userData) {
      telegramUser.value = userData;

      if (userData.is_chat_member) {
        telegramStatus.value = TelegramStatus.Ok;
      } else {
        telegramStatus.value = TelegramStatus.NotMember;
      }
    } else {
      console.warn('Профиль не загружен');
      telegramUser.value = undefined;
      telegramStatus.value = TelegramStatus.NotAuthorized;
    }
  } catch (error) {
    console.error('Ошибка при загрузке профиля', error);
    telegramUser.value = undefined;
    telegramStatus.value = TelegramStatus.NotAuthorized;
  }
}

onMounted(async () => {
  await initializeExtension();
  await loadUserProfile();
});
</script>

<style scoped>
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
  margin: 24px 0 4px;
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
  padding: 8px;
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

.subscribe-status {
  margin-top: 0;
  text-align: center;
  font-family: 'Manrope', sans-serif;
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
  margin-left: 14px;
  font-family: 'Manrope', sans-serif;
  font-size: 10px;
}

.profile-block {
  display: flex;
  align-items: center;
  background: #181725;
  padding: 0 16px 0 0;
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
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.13);
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

.status-container {
  display: flex;
  justify-content: flex-start;
  align-items: center;
  padding: 0px 0px 8px 0px;
  margin-left: 8px;
  width: 100%;
}
</style>

import * as utils from '@/utils';
import Config from '@/config';
import extensionClient from '@/BrowserExtensionClient';
import { onMessage } from '@/messaging';
import { HubConnectionState } from '@microsoft/signalr';
import { TelegramUser } from '@/types/messaging';
import { enableDynamicRules } from '@/rules';

export default defineBackground(() => {
  registerMessageHandlers();

  browser.runtime.onInstalled.addListener(async () => {
    await onInstalled();
    await onStartup();
  });

  browser.runtime.onStartup.addListener(async () => {
    await onStartup();
  });

  browser.alarms.onAlarm.addListener(async (alarm) => {
    if (alarm.name === Config.alarms.checkConnectionState) {
      console.log(`[ALARM] ${alarm.name}`);
      if (extensionClient.connectionState === HubConnectionState.Disconnected) {
        console.log(
          `Статус соединения ${extensionClient.connectionState}, пробуем переподключиться`
        );
        const sessionId = await utils.createNewSession();
        await extensionClient.startWithPersistentRetry(sessionId);
      }
    }

    const sessionId = await storage.getItem<string>(Config.keys.sessionId);
    if (sessionId) {
      utils.setSessionIdToCookie(sessionId);
      console.log('Сессия сохранена в куки', sessionId);
    }
  });
});

export async function onInstalled() {
  await storage.setItem(Config.keys.extensionState, true);
}

export async function onStartup() {
  const sessionId = await utils.createNewSession();
  await extensionClient.startWithPersistentRetry(sessionId);
  await utils.initUserProfile();

  await enableDynamicRules()

  browser.alarms.create(Config.alarms.checkConnectionState, {
    delayInMinutes: 0.5,
    periodInMinutes: 0.5,
  });
}

export function registerMessageHandlers() {
  onMessage('updateActivity', async (message) => {
    await extensionClient.updateActivity(message.data);
  });

  onMessage('clickChannel', async (message) => {
    await extensionClient.clickChannel(message.data);
  });

  onMessage('clickButton', async (message) => {
    await extensionClient.clickButton(message.data);
  });

  onMessage('getChannels', async () => {
    return await extensionClient.getChannels();
  });

  onMessage('getButtons', async (message) => {
    return await extensionClient.getButtons(message.data);
  });

  onMessage('getConnectionState', async () => {
    return extensionClient.connectionState;
  });

  onMessage('checkMember', async (message) => {
    await extensionClient.checkMember(message.data);
  });

  onMessage('getProfile', async () => {
    return await utils.getUserProfile();
  });

  onMessage('initProfile', async () => {
    await utils.initUserProfile();
  });

  onMessage('getSessionId', async () => {
    const sessionId = await storage.getItem<string>(Config.keys.sessionId);
    return sessionId;
  });

  onMessage('getProfileFromStorage', async () => {
    return await storage.getItem<TelegramUser>(Config.keys.userProfile);
  });

  onMessage('enableRulesIfEnabled', async () => {
    const state = await storage.getItem<boolean>(Config.keys.extensionState);
    if (state) {
      await enableDynamicRules();
    }
  });
}

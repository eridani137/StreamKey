import * as utils from '@/utils';
import Config from '@/config';
import extensionClient from '@/BrowserExtensionClient';
import { onMessage } from '@/messaging';
import { loadTwitchRedirectRules } from '@/rules';
import { HubConnectionState } from '@microsoft/signalr';
// import client from '@/client';

export default defineBackground(() => {
  registerMessageHandlers();

  browser.runtime.onInstalled.addListener(async () => {
    await onStartup();
    await onInstalled();
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
  await loadTwitchRedirectRules();
}

export async function onStartup() {
  const sessionId = await utils.createNewSession();
  await extensionClient.startWithPersistentRetry(sessionId);
  await utils.initUserProfile();

  const isEnabled = await storage.getItem(Config.keys.extensionState);
  if (isEnabled) await loadTwitchRedirectRules();

  browser.alarms.create(Config.alarms.checkConnectionState, {
    delayInMinutes: 0.5,
    periodInMinutes: 0.5,
  });
}

export function registerMessageHandlers() {
  onMessage('updateActivity', async (message) => {
    await extensionClient.updateActivity(message.data);

    // await client.updateActivity(message.data);
  });

  onMessage('clickChannel', async (message) => {
    await extensionClient.clickChannel(message.data);

    // await client.clickChannel(message.data);
  });

  onMessage('getChannels', async () => {
    return await extensionClient.getChannels();

    // return await client.getChannels();
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
}

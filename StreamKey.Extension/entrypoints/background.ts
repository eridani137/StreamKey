import * as utils from '@/utils';
import Config from '@/config';
import extensionClient from '@/BrowserExtensionClient';
import { onMessage } from '@/messaging';
import { loadTwitchRedirectRules } from '@/rules';
import { HubConnectionState } from '@microsoft/signalr';

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
      if (extensionClient.connectionState === HubConnectionState.Disconnected) {
        const sessionId = await utils.createNewSession();
        await extensionClient.startWithPersistentRetry(sessionId);
      }
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
  });

  onMessage('clickChannel', async (message) => {
    await extensionClient.clickChannel(message.data);
  });

  onMessage('getConnectionState', async () => {
    return extensionClient.connectionState;
  });

  onMessage('getChannels', async () => {
    return await extensionClient.getChannels();
  });

  onMessage('checkMember', async (message) => {
    await extensionClient.checkMember(message.data);
  });
}

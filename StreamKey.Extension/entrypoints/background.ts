import * as utils from '@/utils';
import Config from '@/config';
import extensionClient from '@/BrowserExtensionClient';
import { onMessage } from '@/messaging';
import { loadTwitchRedirectRules } from '@/rules';
import { createPinia, setActivePinia } from 'pinia'

const pinia = createPinia();
setActivePinia(pinia);

export default defineBackground(() => {
  registerMessageHandlers();
  browser.runtime.onInstalled.addListener(async () => {
    await onStartup();
    await onInstalled();
  });

  browser.runtime.onStartup.addListener(async () => {
    await onStartup();
  });
});

export async function onInstalled() {
  await storage.setItem(Config.keys.extensionState, true);
  await loadTwitchRedirectRules();
}

export async function onStartup() {
  const sessionId = await utils.createNewSession();
  await extensionClient.start(sessionId);
  await utils.initUserProfile();
  const isEnabled = await storage.getItem(Config.keys.extensionState);
  if (isEnabled) {
    await loadTwitchRedirectRules();
  }
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

  // onMessage('wakeConnection', async () => {
  //   await extensionClient.start();
  // });
}
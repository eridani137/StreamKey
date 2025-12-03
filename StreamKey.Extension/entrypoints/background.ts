import * as utils from '@/utils';
import Config from '@/config';
import extensionClient from '@/BrowserExtensionClient';
import { onMessage } from '@/messaging';
import { loadTwitchRedirectRules } from '@/rules';

export default defineBackground(() => {
  registerMessageHandlers();
  browser.runtime.onInstalled.addListener(async () => {
    const sessionId = await utils.createNewSession();
    await extensionClient.start(sessionId);
    await storage.setItem(Config.keys.extensionState, true);
    await loadTwitchRedirectRules();
    await utils.initUserProfile();
  });

  browser.runtime.onStartup.addListener(async () => {
    const sessionId = await utils.createNewSession();
    await extensionClient.start(sessionId);
    await utils.initUserProfile();
  });
});

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
    return extensionClient.getChannels();
  });
}
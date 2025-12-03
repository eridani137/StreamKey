import * as utils from '@/utils';
import Config from '@/config';
import extensionClient from '@/BrowserExtensionClient';
import { onMessage, sendMessage } from '@/messaging';
import { loadTwitchRedirectRules } from '@/rules';

export default defineBackground(() => {
  browser.runtime.onInstalled.addListener(async () => {
    const sessionId = await utils.createNewSession();
    await storage.setItem(Config.keys.extensionState, true);
    await loadTwitchRedirectRules();
    await utils.initUserProfile();
    await extensionClient.start(sessionId);
  });

  browser.runtime.onStartup.addListener(async () => {
    const sessionId = await utils.createNewSession();
    await utils.initUserProfile();
    await extensionClient.start(sessionId);
  });

  onMessage('updateActivity', async (message) => {
    await extensionClient.updateActivity(message.data);
  });

  onMessage('clickChannel', async (message) => {
    await extensionClient.clickChannel(message.data);
  });
  
  onMessage('getConnectionState', async () => {
    return extensionClient.connectionState;
  })

  onMessage('getTelegramUser', async (message) => {
    return extensionClient.getTelegramUser(message.data);
  })
});

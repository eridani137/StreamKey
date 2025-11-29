import * as utils from '@/utils';
import { Config } from '@/config';

export default defineBackground(() => {
  browser.runtime.onInstalled.addListener(async () => {
    console.log('Расширение установлено. Устанавливаем состояние по умолчанию');

    utils.createNewSession();

    await storage.setItem(Config.keys.extensionState, true);

    utils.enableRuleset();

    await utils.initUserProfile();
  });

  browser.runtime.onStartup.addListener(async () => {
    utils.createNewSession();

    await utils.initUserProfile();
  });

  browser.runtime.onMessage.addListener(async (message) => {
    if (message.type === 'GET_USER_PROFILE') {
      return await storage.getItem(Config.keys.userProfile);
    }

    return undefined;
  });
});

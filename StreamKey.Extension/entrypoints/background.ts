import * as utils from '@/utils';
import Config from '@/config';
import extensionClient from "@/entrypoints/BrowserExtensionClient";

export default defineBackground(() => {
    browser.runtime.onInstalled.addListener(async () => {
        console.log('Расширение установлено. Устанавливаем состояние по умолчанию');
        const sessionId = await utils.createNewSession();
        await storage.setItem(Config.keys.extensionState, true);
        await utils.enableRuleset();
        await utils.initUserProfile();
        await extensionClient.start(sessionId);
    });

    browser.runtime.onStartup.addListener(async () => {
        const sessionId = await utils.createNewSession();
        await utils.initUserProfile();
        await extensionClient.start(sessionId);
    });

    browser.runtime.onMessage.addListener(async (message) => {
        switch (message.type) {
            case 'getExtensionClient':
                return extensionClient;
            default:
                return undefined;
        }
    });
});

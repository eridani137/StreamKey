import * as utils from '@/utils';
import Config from '@/config';
import extensionClient from "@/entrypoints/BrowserExtensionClient";

export default defineBackground(() => {
    browser.runtime.onInstalled.addListener(async () => {
        console.log('Расширение установлено. Устанавливаем состояние по умолчанию');
        await utils.createNewSession();
        await storage.setItem(Config.keys.extensionState, true);
        await utils.enableRuleset();
        await utils.initUserProfile();
        await extensionClient.start();
    });

    browser.runtime.onStartup.addListener(async () => {
        await utils.createNewSession();
        await utils.initUserProfile();
        await extensionClient.start();
    });

    // browser.runtime.onMessage.addListener(async (message) => {
    //     switch (message.type) {
    //         default:
    //             return undefined;
    //     }
    // });
});

import * as utils from '@/utils';
import Config from '@/config';
import extensionClient from "@/entrypoints/BrowserExtensionClient";

export default defineBackground(() => {
    browser.runtime.onInstalled.addListener(async () => {
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
});

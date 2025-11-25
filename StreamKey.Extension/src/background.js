import { CONFIG } from './config';
import * as utils from './utils';

utils.api.runtime.onInstalled.addListener(async () => {
    console.log('Расширение установлено. Устанавливаем состояние по умолчанию');

    utils.createNewSession();

    utils.saveState(CONFIG.extensionStateKeyName, true);

    utils.enableRuleset();

    utils.getUserProfile();
});

utils.api.runtime.onStartup.addListener(async () => {
    utils.createNewSession();

    utils.getUserProfile();
});

utils.api.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message.type === "GET_COOKIE") {
        utils.api.cookies.getAll({ domain: message.domain, name: message.name }, (cookies) => {
            const cookie = cookies.find(c => c.name === message.name);
            if (cookie) {
                sendResponse(cookie.value);
            } else {
                sendResponse(null);
            }
        });
        return true;
    }
});
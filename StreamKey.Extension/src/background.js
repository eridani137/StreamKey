import { CONFIG } from './config';
import * as utils from './utils';

utils.api.runtime.onInstalled.addListener(() => {
    console.log('Расширение установлено. Устанавливаем состояние по умолчанию');

    utils.createNewSession();

    utils.saveState(CONFIG.extensionStateKeyName, true);

    utils.enableRuleset();
});

utils.api.runtime.onStartup.addListener(() => {
    utils.createNewSession();

    const telegramUserId = utils.getCookieValue(CONFIG.streamKeyUrl, 'tg_user_id');
    if (telegramUserId) {
        console.log("TelegramId получен", telegramUserId);

        
    }
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
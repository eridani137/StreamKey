import { CONFIG } from '../config';
import * as utils from '../utils';

utils.api.runtime.onInstalled.addListener(() => {
    console.log('Расширение установлено. Устанавливаем состояние по умолчанию');

    utils.createNewSession();

    utils.saveState(CONFIG.extensionStateKeyName, true);

    utils.enableRuleset();
});

utils.api.runtime.onStartup.addListener(() => { utils.createNewSession(); });

utils.api.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message.type === "GET_COOKIES") {
        utils.api.cookies.getAll({ domain: message.domain }, (cookies) => {
            const cookie = cookies.find(c => c.name === message.name);
            if (cookie) {
                let valueObj;
                try {
                    valueObj = JSON.parse(decodeURIComponent(cookie.value));
                } catch (e) {
                    valueObj = cookie.value;
                }

                sendResponse(valueObj);
            } else {
                sendResponse(null);
            }
        });
        return true;
    }
});
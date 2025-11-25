import { CONFIG } from './config';
import * as utils from './utils';

async function initializeUserProfile() {
    const userProfile = await utils.getUserProfile();
    console.log('Инициализация профиля', userProfile)
    if (userProfile) {
        await utils.api.storage.local.set({ userProfile: userProfile });
    }
}

utils.api.runtime.onInstalled.addListener(async () => {
    console.log('Расширение установлено. Устанавливаем состояние по умолчанию');

    utils.createNewSession();

    utils.saveState(CONFIG.extensionStateKeyName, true);

    utils.enableRuleset();

    await initializeUserProfile();
});

utils.api.runtime.onStartup.addListener(async () => {
    utils.createNewSession();

    await initializeUserProfile();
});

utils.api.runtime.onMessage.addListener((message, sender, sendResponse) => {
    // console.log('onMessage', message);
    if (message.type === "GET_COOKIE") {
        utils.api.cookies.getAll({ domain: message.domain, name: message.name }, (cookies) => {
            const cookie = cookies.find(c => c.name === message.name);
            sendResponse(cookie.value || null);
        });
    } else if (message.type === "GET_USER_PROFILE") {
        utils.api.storage.local.get('userProfile', (result) => {
            sendResponse(result.userProfile || null);
        });
    }

    return true;
});

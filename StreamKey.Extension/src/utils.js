export const api = typeof browser !== 'undefined' ? browser : chrome;

const sessionIdKeyName = "sessionId";
const oauthTelegramUrl = "https://oauth.telegram.org/";
const streamKeyUrl = "https://streamkey.ru/";

export function generateSessionId() {
    const timestamp = Date.now();
    const tsHex = timestamp.toString(16).padStart(12, '0');
    let rand = '';
    for (let i = 0; i < 19; i++) {
        rand += Math.floor(Math.random() * 16).toString(16);
    }
    return [
        tsHex.substring(0, 8),                // 8 символов времени
        tsHex.substring(8, 12),               // 4 символа времени
        '7' + rand.substring(0, 3),           // версия 7 + часть рандома
        (parseInt(rand.substring(3, 4), 16) & 0x3 | 0x8).toString(16) + rand.substring(4, 7), // вариант 10xx + 3 символа
        rand.substring(7, 19)                 // оставшиеся 12 символов
    ].join('-');
}

export function createNewSession() {
    const sessionId = generateSessionId();

    saveState(sessionIdKeyName, sessionId);
    console.log('Сгенерирован ID сессии:', sessionId);

    return sessionId;
}

export async function loadState(keyName) {
    try {
        const result = await api.storage.local.get([keyName]);
        return result[keyName];
    } catch (error) {
        console.error(`Ошибка загрузки значения для ключа ${keyName}:`, error);
        return undefined;
    }
}

export async function saveState(keyName, value) {
    try {
        await api.storage.local.set({
            [keyName]: value,
        });
    } catch (error) {
        console.error(`Ошибка сохранения значения для ключа ${keyName}:`, error);
    }
}

export async function enableRuleset() {
    try {
        if (
            api.declarativeNetRequest &&
            api.declarativeNetRequest.updateEnabledRulesets
        ) {
            await api.declarativeNetRequest.updateEnabledRulesets({
                enableRulesetIds: ['ruleset_1'],
                disableRulesetIds: [],
            });
            console.log('Правила перенаправления активированы');
        } else {
            console.warn(
                'declarativeNetRequest не поддерживается в этом браузере'
            );
        }
    } catch (err) {
        console.error('Ошибка активации правил:', err);
    }
}

export async function disableRuleset() {
    try {
        if (
            api.declarativeNetRequest &&
            api.declarativeNetRequest.updateEnabledRulesets
        ) {
            await api.declarativeNetRequest.updateEnabledRulesets({
                enableRulesetIds: [],
                disableRulesetIds: ['ruleset_1'],
            });
            console.log('Правила перенаправления деактивированы');
        } else {
            console.warn(
                'declarativeNetRequest не поддерживается в этом браузере'
            );
        }
    } catch (err) {
        console.error('Ошибка деактивации правил:', err);
    }
}

export async function hasStelAcidCookie() {
    return new Promise((resolve) => {
        api.cookies.get({ url: oauthTelegramUrl, name: 'stel_acid' }, (cookie) => {
            if (chrome.runtime?.lastError) {
                console.error("hasAcidCookie", chrome.runtime.lastError);
                resolve(false);
                return;
            }

            resolve(!!cookie);
        });
    });
}

export async function getTgUser() {
    return new Promise((resolve) => {
        api.cookies.get({ url: streamKeyUrl, name: 'tg_user' }, (cookie) => {
            if (chrome.runtime?.lastError) {
                console.error("getTgUser", chrome.runtime.lastError);
                resolve(undefined);
                return;
            }

            let valueObj;
            try {
                valueObj = JSON.parse(decodeURIComponent(cookie.value));
            } catch (e) {
                valueObj = undefined;
            }

            resolve(valueObj);
        });
    });
}

export function checkTgUser(user) {
    const currentTime = Math.floor(Date.now() / 1000);
    const seconds = 604800;
    if (currentTime - user.authDate > seconds) {
        return false;
    }

    return true;
}
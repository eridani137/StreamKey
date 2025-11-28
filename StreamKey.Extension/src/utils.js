import { CONFIG } from './config';

export const api = typeof browser !== 'undefined' ? browser : chrome;

const sessionIdKeyName = "sessionId";

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

export async function getCookieValue(url, name) {
    return new Promise((resolve) => {
        api.cookies.get({ url, name }, (cookie) => {
            if (chrome.runtime?.lastError) {
                console.error("getCookieValue:", chrome.runtime.lastError);
                resolve(null);
                return;
            }
            if (cookie) {
                resolve(cookie.value);
            } else {
                resolve(null);
            }
        });
    });
}

export async function getUserProfile() {
    const telegramUserId = await getCookieValue(CONFIG.streamKeyUrl, 'tg_user_id');
    const telegramUserHash = await getCookieValue(CONFIG.streamKeyUrl, 'tg_user_hash');

    if (telegramUserId && telegramUserHash) {
        try {
            const response = await fetch(`${CONFIG.apiUrl}/telegram/user/${telegramUserId}/${telegramUserHash}`, {
                method: 'GET',
                headers: { 'Content-Type': 'application/json' }
            });

            if (!response.ok) {
                console.log('Сервер вернул ошибку: ' + response.status);
                return;
            }

            const text = await response.text();
            const obj = text ? JSON.parse(text) : null;

            console.log(obj);

            return obj;
        } catch (err) {
            console.error(err);
            return null;
        }
    } else {
        await api.storage.local.remove("userProfile");
    }
    return null;
}
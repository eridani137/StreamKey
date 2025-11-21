import { CONFIG } from './config';

export const api = typeof browser !== 'undefined' ? browser : chrome;

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

    api.storage.local.set({sessionId: sessionId}, () => {
        console.log('Сгенерирован ID сессии:', sessionId);
    });

    return sessionId;
}

export async function loadExtensionState() {
    try {
        const result = await api.storage.local.get([
            CONFIG.extensionStateKeyName
        ]);
        return !!result[CONFIG.extensionStateKeyName];
    } catch (error) {
        console.error('Ошибка загрузки состояния:', error);
        return false;
    }
}

export async function saveExtensionState(value) {
    try {
        await api.storage.local.set({
            [CONFIG.extensionStateKeyName]: value,
        });
    } catch (error) {
        console.error('Ошибка сохранения состояния:', error);
    }
}

export async function loadExtensionMode() {
    try {
        const result = await api.storage.local.get([
            CONFIG.enhancedQualityKeyName
        ]);
        return !!result[CONFIG.enhancedQualityKeyName];
    } catch (error) {
        console.error('Ошибка загрузки режима работы:', error);
        return false;
    }
}

export async function saveExtensionMode(value) {
    try {
        await api.storage.local.set({
            [CONFIG.enhancedQualityKeyName]: value,
        });
    } catch (error) {
        console.error('Ошибка сохранения режима работы:', error);
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

export async function hasAcidCookie() {
    return new Promise((resolve) => {
        api.cookies.get({ url: CONFIG.oauthTelegramUrl, name: 'stel_acid' }, (cookie) => {
            if (chrome.runtime?.lastError) {
                console.error("hasAcidCookie", chrome.runtime.lastError);
                resolve(false);
                return;
            }

            resolve(!!cookie);
        });
    });
}
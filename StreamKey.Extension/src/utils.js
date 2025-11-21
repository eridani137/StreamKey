import { CONFIG } from './config';

export const api = typeof browser !== 'undefined' ? browser : chrome;

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

            if (cookie) {
                console.log("hasAcidCookie: Cookie есть:", cookie);
            } else {
                console.log("hasAcidCookie: Cookie нету");
            }
            resolve(!!cookie);
        });
    });
}
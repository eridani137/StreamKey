import Config from './config';
import {TelegramUser} from './types';

export function generateSessionId(): string {
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

export async function createNewSession(): Promise<string> {
    const sessionId = generateSessionId();

    await storage.setItem(Config.keys.sessionId, sessionId);
    console.log('Сгенерирован ID сессии:', sessionId);

    return sessionId;
}

export async function enableRuleset(): Promise<void> {
    try {
        if (
            browser.declarativeNetRequest &&
            browser.declarativeNetRequest.updateEnabledRulesets
        ) {
            await browser.declarativeNetRequest.updateEnabledRulesets({
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

export async function disableRuleset(): Promise<void> {
    try {
        if (
            browser.declarativeNetRequest &&
            browser.declarativeNetRequest.updateEnabledRulesets
        ) {
            await browser.declarativeNetRequest.updateEnabledRulesets({
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

export async function getCookieValue(url: string, name: string): Promise<string | null> {
    return new Promise((resolve) => {
        browser.cookies.get({url, name}, (cookie) => {
            if (browser.runtime?.lastError) {
                console.error("getCookieValue:", browser.runtime.lastError);
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

export async function getUserProfile(): Promise<TelegramUser | null> {
    const telegramUserId = await getCookieValue(Config.urls.streamKeyUrl, 'tg_user_id');
    const telegramUserHash = await getCookieValue(Config.urls.streamKeyUrl, 'tg_user_hash');

    console.log('tgUserId:', telegramUserId);
    console.log('tgUserHash:', telegramUserHash);

    if (telegramUserId && telegramUserHash) {
        try {
            const response = await fetch(`${Config.urls.apiUrl}/telegram/user/${telegramUserId}/${telegramUserHash}`, {
                method: 'GET',
                headers: {'Content-Type': 'application/json'}
            });

            if (!response.ok) {
                console.log('Сервер вернул ошибку: ' + response.status);
                return null;
            }

            const text = await response.text();

            const data = text ? JSON.parse(text) : null;

            console.log('received user data', data);

            return data;
        } catch (err) {
            console.error(err);
            return null;
        }
    } else {
        await storage.removeItem(Config.keys.userProfile);
    }
    return null;
}

export async function initUserProfile(): Promise<void> {
    const userProfile = await getUserProfile();
    if (userProfile) {
        await storage.setItem(Config.keys.userProfile, userProfile);
    }
}

export const sleep = (ms: number): Promise<void> => new Promise(resolve => setTimeout(resolve, ms));

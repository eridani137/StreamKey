import Config from './config';
import {DeviceInfo, TelegramUser} from './types';

export function getDeviceInfo() : DeviceInfo {
    return {
        userAgent: navigator.userAgent,
        language: navigator.language,
        platform: navigator.platform || null,
        product: navigator.product || null,
        appName: navigator.appName || null,
        appCodeName: navigator.appCodeName || null,
        hardwareConcurrency: navigator.hardwareConcurrency || null,
        timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
        deviceMemory: (navigator as any).deviceMemory || null,
    };
}

export function getDeviceHash(): string {
    const deviceInfo = getDeviceInfo();
    const deviceString = Object.values(deviceInfo).join('|');

    let hash = 2166136261n; // FNV offset basis (32-bit)

    for (let i = 0; i < deviceString.length; i++) {
        hash ^= BigInt(deviceString.charCodeAt(i));
        hash *= 16777619n; // FNV prime
        hash &= 0xffffffffn; // Keep 32-bit
    }

    let result = hash.toString(16).padStart(8, '0');

    for (let round = 0; round < 7; round++) {
        hash = 2166136261n;
        const input = result + deviceString + round;
        for (let i = 0; i < input.length; i++) {
            hash ^= BigInt(input.charCodeAt(i));
            hash *= 16777619n;
            hash &= 0xffffffffn;
        }
        result += hash.toString(16).padStart(8, '0');
    }

    return result;
}

export function generateDeviceUUID(): string {
    const timestamp = Date.now();
    const tsHex = timestamp.toString(16).padStart(12, '0');

    const deviceHash = getDeviceHash();

    const devicePart = deviceHash.substring(0, 19);

    return [
        tsHex.substring(0, 8),
        tsHex.substring(8, 12),
        '7' + devicePart.substring(0, 3),
        (parseInt(devicePart.substring(3, 4), 16) & 0x3 | 0x8).toString(16) + devicePart.substring(4, 7),
        devicePart.substring(7, 19)
    ].join('-');
}

export async function setSessionId(sessionId: string): Promise<void> {
    await storage.setItem(Config.keys.sessionId, sessionId);

    browser.cookies.set({
        url: Config.urls.streamKeyUrl,
        name: 'sessionId',
        value: sessionId,
        path: '/'
    });

    console.log('Сгенерирован ID сессии:', sessionId);
}

export async function createNewSession(): Promise<string> {
    const sessionId = generateDeviceUUID();

    await setSessionId(sessionId);

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

    console.log('Обновление профиля');
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

            console.log(data);

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

export async function initUserProfile(telegramUser: TelegramUser | null = null): Promise<void> {
    const userData = telegramUser ?? await getUserProfile();

    if (!userData) {
        await storage.removeItem(Config.keys.userProfile);
        return;
    }

    await storage.setItem(Config.keys.userProfile, userData);
}

export const sleep = (ms: number): Promise<void> => new Promise(resolve => setTimeout(resolve, ms));

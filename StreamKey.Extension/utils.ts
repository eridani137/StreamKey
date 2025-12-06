import { HubConnectionState } from '@microsoft/signalr';
import extensionClient from './BrowserExtensionClient';
import Config from './config';
import { DeviceInfo, StatusType, TelegramUser, TelegramUserResponse } from './types';

export function getDeviceInfo(): DeviceInfo {
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
    ((parseInt(devicePart.substring(3, 4), 16) & 0x3) | 0x8).toString(16) +
      devicePart.substring(4, 7),
    devicePart.substring(7, 19),
  ].join('-');
}

export async function setSessionId(sessionId: string): Promise<void> {
  await storage.setItem(Config.keys.sessionId, sessionId);

  browser.cookies.set({
    url: Config.urls.streamKeyUrl,
    name: 'sessionId',
    value: sessionId,
    path: '/',
  });

  console.log('Сгенерирован ID сессии:', sessionId);
}

export async function createNewSession(): Promise<string> {
  const sessionId = generateDeviceUUID();

  await setSessionId(sessionId);

  return sessionId;
}

export async function getUserProfile(): Promise<TelegramUser | null> {
  const telegramUserId = (await browser.cookies.get({
    url: Config.urls.streamKeyUrl,
    name: 'tg_user_id'
  }))?.value;
  const telegramUserHash = (await browser.cookies.get({
    url: Config.urls.streamKeyUrl,
    name: 'tg_user_hash'
  }))?.value;

  console.log('Обновление профиля');

  console.log('tgUserId:', telegramUserId);
  console.log('tgUserHash:', telegramUserHash);

  if (telegramUserId && telegramUserHash) {
    try {
      const responseData : TelegramUserResponse = {
        UserId: Number(telegramUserId),
        UserHash: telegramUserHash
      }

      const response = await extensionClient.getTelegramUser(responseData);
      if (!response) {
        console.log("Сервер не вернул пользователя");
        return null;
      }

      console.log('Сервер вернул пользователя', response);

      return response;
    } catch (err) {
      console.error(err);
      return null;
    }
  } else {
    await storage.removeItem(Config.keys.userProfile);
  }
  return null;
}

export async function initUserProfile(
  telegramUser: TelegramUser | null = null
): Promise<void> {
  const userData = telegramUser ?? (await getUserProfile());

  if (!userData) {
    await storage.removeItem(Config.keys.userProfile);
    return;
  }

  await storage.setItem(Config.keys.userProfile, userData);
}

export const sleep = (ms: number): Promise<void> =>
  new Promise((resolve) => setTimeout(resolve, ms));

export function getTwitchUserId(): string | null {
  const userIdRaw = localStorage.getItem(Config.keys.twId);
  return userIdRaw ? userIdRaw.replace(/^"|"$/g, '') : null;
}

export async function waitForElement(xpath: string, timeout = 20000) {
  const startTime = Date.now();

  return new Promise((resolve) => {
    const element = getElementByXPath(xpath);
    if (element) {
      resolve(element);
      return;
    }

    const observer = new MutationObserver(() => {
      const element = getElementByXPath(xpath);
      if (element) {
        observer.disconnect();
        resolve(element);
      } else if (Date.now() - startTime > timeout) {
        observer.disconnect();
        resolve(null);
      }
    });

    observer.observe(document.body, {
      childList: true,
      subtree: true,
    });
  });
}

export function getElementByXPath(xpath: string) {
  const result = document.evaluate(
    xpath,
    document,
    null,
    XPathResult.FIRST_ORDERED_NODE_TYPE,
    null
  );
  return result.singleNodeValue as HTMLElement | null;
}

export function getStateClass(state: HubConnectionState) {
  return state === HubConnectionState.Connected
    ? StatusType.WORKING
    : StatusType.MAINTENANCE;
}
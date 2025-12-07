import { HTMLDivElement } from 'linkedom';

export interface AppUrls {
  streamKeyUrl: string;
  streamKeyQAUrl: string;
  apiUrl: string;
  telegramChannelUrl: string;
  extensionAuthorizationUrl: string;
  extensionStateKeyName: string;
  extensionHub: string;
  twitchUrl: string;
}

export interface StorageKeys {
  sessionId: StorageItemKey;
  userProfile: StorageItemKey;
  extensionState: StorageItemKey;
}

export interface QualityMenu {
  qualityMenuSelectors: {
    menuContainer: string;
    radioItems: string;
    radioLabel: string;
  };
  badgeText: string;
  minResolution: number;
  instructionUrl: string;
}

export interface AlarmKeys {
  checkConnectionState: string;
}

export interface AppConfig {
  urls: AppUrls;
  keys: StorageKeys;
  qualityMenu: QualityMenu;
  alarms: AlarmKeys;
}

export interface TelegramUser {
  photo_url: string;
  username: string;
  id: number | string;
  is_chat_member?: boolean;
}

export enum TelegramStatus {
  NotAuthorized,
  NotMember,
  Ok,
}

export type Nullable<T> = T | null;

export interface StreamkeyBlockedElement extends HTMLElement {
  _streamkeyBlockers?: {
    blockAllEvents: (e: Event) => void;
    overlay: HTMLDivElement;
  };
}

export interface ChannelInfo {
  thumb: string;
  title: string;
  viewers: number;
  category: string;
  description?: string;
}

export interface ChannelData {
  channelName: string;
  position: number;
  info: ChannelInfo;
}

export interface WithSessionId {
  SessionId: string;
}

export interface WithUserId {
  UserId: string;
}

export interface WithNumberUserId {
  UserId: number;
}

export interface WithUserHash {
  UserHash: string;
}

export interface WithChannelName {
  ChannelName: string;
}

export interface ActivityRequest extends WithUserId, WithSessionId {}

export interface TelegramUserResponse extends WithNumberUserId, WithUserHash {}

export interface ClickChannel extends WithUserId, WithChannelName {}

export interface CheckMemberResponse extends WithNumberUserId {}

export interface DeviceInfo {
  userAgent: string;
  language: string;
  platform: string | null;
  product: string | null;
  appName: string | null;
  appCodeName: string | null;
  hardwareConcurrency: number | null;
  timezone: string;
  deviceMemory: number | null;
}

export type Rule = Awaited<
  ReturnType<typeof browser.declarativeNetRequest.getDynamicRules>
>[number];

export enum StatusType {
  WORKING = 'working',
  MAINTENANCE = 'maintenance'
}
import {HTMLDivElement} from "linkedom";

export interface AppUrls {
  streamKeyUrl: string;
  streamKeyQAUrl: string;
  apiUrl: string;
  telegramChannelUrl: string;
  extensionAuthorizationUrl: string;
  extensionStateKeyName: string;
  extensionHub: string;
}

export interface StorageKeys {
  sessionId: string;
  userProfile: StorageItemKey;
  extensionState: StorageItemKey;
  twId: string;
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

export interface AppConfig {
  urls: AppUrls;
  keys: StorageKeys;
  qualityMenu: QualityMenu;
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
    Ok
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

export interface UserData {
    SessionId: string;
}

export interface UserActivity extends UserData {
    UserId: string;
}

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
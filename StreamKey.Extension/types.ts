export interface AppUrls {
  streamKeyUrl: string;
  streamKeyQAUrl: string;
  apiUrl: string;
  telegramChannelUrl: string;
  extensionAuthorizationUrl: string;
  extensionStateKeyName: string;
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
    },
    badgeText: string;
    minResolution: number,
    instructionUrl: string;
}

export interface MessagingKeys {

}

export interface AppConfig {
  urls: AppUrls;
  keys: StorageKeys;
  qualityMenu: QualityMenu;
  messaging: MessagingKeys;
}

export interface TgUser {
  photo_url: string;
  username: string;
  id: number | string;
  is_chat_member?: boolean;
}

export type Nullable<T> = T | null;

export interface StreamkeyBlockedElement extends HTMLElement {
  _streamkeyBlockers?: {
    blockClick: (e: Event) => void;
  };
}

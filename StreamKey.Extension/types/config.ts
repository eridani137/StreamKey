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

export interface AlarmKeys {
  checkConnectionState: string;
}

export interface StreamBottomButtonsMenu {
  spacingSelector: string;
  buttonsContainerName: string;
  uniqueButtonClassMask: string;
}

export interface Intervals {
  updateChannels: number;
  updateActivity: number;
  updateStreamBottomButtons: number;
}

export interface AppConfig {
  urls: AppUrls;
  keys: StorageKeys;
  qualityMenu: QualityMenu;
  alarms: AlarmKeys;
  streamBottomButtonsMenu: StreamBottomButtonsMenu;
  intervals: Intervals;
}
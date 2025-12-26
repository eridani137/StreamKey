import { AppConfig } from './types/config';

const Config: AppConfig = {
  urls: {
    streamKeyUrl: 'https://streamkey.ru',
    streamKeyQAUrl: 'https://t.me/streamkeys',
    apiUrl: 'https://service.streamkey.ru',
    telegramChannelUrl: 'https://t.me/streamkey',
    extensionAuthorizationUrl: 'https://streamkey.ru/extension-authorization/',
    extensionStateKeyName: 'streamKeyEnabled',
    extensionHub: 'http://5.129.226.150:7777/hubs/extension',
    twitchUrl: 'https://www.twitch.tv/'
  },
  keys: {
    sessionId: 'session:sessionId',
    userProfile: 'session:userProfile',
    extensionState: 'local:streamKeyEnabled',
    twId: 'local_copy_unique_id',
  },
  qualityMenu: {
    qualityMenuSelectors: {
      menuContainer: "div[data-a-target='player-settings-menu']",
      radioItems: "div[role='menuitemradio']",
      radioLabel: 'label.ScRadioLabel-sc-1pxozg3-0',
    },
    badgeText: 'Stream Key',
    minResolution: 1080,
    instructionUrl: 'https://service.streamkey.ru/files/menu-instruction.jpg',
  },
  alarms: {
    checkConnectionState: 'CHECK_CONNECTION_STATE'
  },
  buttonsMenu: {
    spacingSelector: '.metadata-layout__secondary-button-spacing',
    buttonsContainerName: 'streamkey-buttons-container',
    uniqueButtonClassMask: 'streamkey-livechannel-button-'
  }
};

export default Config;

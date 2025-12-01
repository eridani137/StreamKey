import {AppConfig} from './types';

const Config: AppConfig = {
    urls: {
        streamKeyUrl: 'https://streamkey.ru',
        streamKeyQAUrl: 'https://t.me/streamkeys',
        apiUrl: 'https://service.streamkey.ru',
        telegramChannelUrl: 'https://t.me/streamkey',
        extensionAuthorizationUrl: 'https://streamkey.ru/extension-authorization/',
        extensionStateKeyName: 'streamKeyEnabled',
        extensionHub: 'https://service.streamkey.ru/hubs/extension'
    },
    keys: {
        sessionId: 'local:sessionId',
        userProfile: 'local:userProfile',
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
};

export default Config

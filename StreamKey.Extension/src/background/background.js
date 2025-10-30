const api = typeof browser !== 'undefined' ? browser : chrome;

function generateSessionId() {
    const timestamp = Date.now();
    const timestampHex = timestamp.toString(16).padStart(12, '0');
    const version = '7';
    const randA = Math.floor(Math.random() * 0x1000).toString(16).padStart(3, '0');
    const randB1 = (Math.floor(Math.random() * 0x4000) | 0x8000).toString(16);
    const randB2 = Math.floor(Math.random() * 0x1000000000000).toString(16).padStart(12, '0');
    return [
        timestampHex.substring(0, 8),
        timestampHex.substring(8, 12) + version + randA.substring(0, 3),
        randB1 + randA.substring(3, 3) + randB2.substring(0, 3),
        randB2.substring(3, 7) + randB2.substring(7, 11),
        randB2.substring(11, 12) + Math.floor(Math.random() * 0x100000000000).toString(16).padStart(11, '0')
    ].join('-');
}

function createNewSession() {
    const sessionId = generateSessionId();

    api.storage.local.set({
        streamKeyEnabled: true,
        sessionId: sessionId
    }, () => {
        console.log('Сгенерирован ID сессии:', sessionId);
    });

    return sessionId;
}

api.runtime.onInstalled.addListener(() => {
    console.log('Расширение установлено. Устанавливаем состояние по умолчанию');

    createNewSession();

    api.storage.local.set({
        streamKeyEnabled: true
    }, () => {
        console.log('Состояние streamKeyEnabled по умолчанию: true');
    });

    if (api.declarativeNetRequest && api.declarativeNetRequest.updateEnabledRulesets) {
        api.declarativeNetRequest.updateEnabledRulesets({
            enableRulesetIds: ['ruleset_1'],
            disableRulesetIds: []
        }).then(() => {
            console.log('Правила declarativeNetRequest активированы');
        }).catch((error) => {
            console.error('Ошибка активации правил:', error);
        });
    } else {
        console.warn('declarativeNetRequest не поддерживается в этом браузере');
    }
});

api.runtime.onStartup.addListener(() => {
    createNewSession();
});
const api = typeof browser !== 'undefined' ? browser : chrome;

function generateSessionId() {
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
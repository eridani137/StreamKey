const api = typeof browser !== 'undefined' ? browser : chrome;

api.runtime.onInstalled.addListener(() => {
  console.log('Расширение установлено. Устанавливаем состояние по умолчанию');

  api.storage.local.set({ streamKeyEnabled: true }, () => {
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
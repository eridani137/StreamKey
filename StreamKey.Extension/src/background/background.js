import { CONFIG } from '../config';
import * as utils from '../utils';

utils.api.runtime.onInstalled.addListener(() => {
    console.log('Расширение установлено. Устанавливаем состояние по умолчанию');

    utils.createNewSession();

    utils.saveState(CONFIG.extensionStateKeyName, true);

    utils.enableRuleset();
});

utils.api.runtime.onStartup.addListener(() => { utils.createNewSession(); });
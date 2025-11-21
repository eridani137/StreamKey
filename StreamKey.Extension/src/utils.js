import { CONFIG } from './config';

export const api = typeof browser !== 'undefined' ? browser : chrome;

export async function loadExtensionState() {
    try {
        const result = await api.storage.local.get([
            CONFIG.extensionStateKeyName
        ]);
        return !!result[CONFIG.extensionStateKeyName];
    } catch (error) {
        console.error('Ошибка загрузки состояния:', error);
        return false;
    }
}

export async function saveExtensionState(value) {
    try {
        await api.storage.local.set({
            [CONFIG.extensionStateKeyName]: value,
        });
    } catch (error) {
        console.error('Ошибка сохранения состояния:', error);
    }
}
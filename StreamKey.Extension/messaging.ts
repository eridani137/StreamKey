import { defineExtensionMessaging } from '@webext-core/messaging';

interface ProtocolMap {
    updateActivity(userId: string) : Promise<void>;
}

export const { sendMessage, onMessage } = defineExtensionMessaging<ProtocolMap>();
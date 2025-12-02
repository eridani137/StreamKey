import { defineExtensionMessaging } from '@webext-core/messaging';
import { ClickChannel, WithUserId } from '@/types';

interface ProtocolMap {
  updateActivity(payload: WithUserId): Promise<void>;
  clickChannel(payload: ClickChannel): Promise<void>;
}

export const { sendMessage, onMessage } =
  defineExtensionMessaging<ProtocolMap>();

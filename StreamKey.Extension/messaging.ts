import { defineExtensionMessaging } from '@webext-core/messaging';
import { ClickChannel, WithUserId } from '@/types';
import { HubConnectionState } from '@microsoft/signalr/dist/esm/HubConnection';

interface ProtocolMap {
  updateActivity(payload: WithUserId): Promise<void>;
  clickChannel(payload: ClickChannel): Promise<void>;
  getConnectionState(): Promise<HubConnectionState>;
  setConnectionState(payload: HubConnectionState): Promise<void>;
}

export const { sendMessage, onMessage } = defineExtensionMessaging<ProtocolMap>();

import { defineExtensionMessaging } from '@webext-core/messaging';
import { ChannelData, ClickChannel, WithUserId } from '@/types';
import { HubConnectionState } from '@microsoft/signalr/dist/esm/HubConnection';

interface ProtocolMap {
  updateActivity(payload: WithUserId): Promise<void>;
  clickChannel(payload: ClickChannel): Promise<void>;
  getConnectionState(): Promise<HubConnectionState>;
  setConnectionState(payload: HubConnectionState): Promise<void>;
  getChannels(): Promise<ChannelData[]>;
}

export const { sendMessage, onMessage } =
  defineExtensionMessaging<ProtocolMap>();

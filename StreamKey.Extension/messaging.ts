import { defineExtensionMessaging } from '@webext-core/messaging';
import { ChannelData, CheckMemberResponse, ClickChannel, WithUserId } from '@/types';
import { HubConnectionState } from '@microsoft/signalr/dist/esm/HubConnection';

interface ProtocolMap {
  updateActivity(payload: WithUserId): Promise<void>;
  clickChannel(payload: ClickChannel): Promise<void>;
  getConnectionState(): Promise<HubConnectionState>;
  setConnectionState(payload: HubConnectionState): Promise<void>;
  getChannels(): Promise<ChannelData[]>;
  checkMember(payload: CheckMemberResponse): Promise<void>;
  wakeConnection(): Promise<void>;
}

export const { sendMessage, onMessage } =
  defineExtensionMessaging<ProtocolMap>();

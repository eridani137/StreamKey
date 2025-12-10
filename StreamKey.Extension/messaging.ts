import { defineExtensionMessaging } from '@webext-core/messaging';
import { ActivityRequest, ChannelData, CheckMemberResponse, ClickChannel, TelegramUser } from '@/types';
import { HubConnectionState } from '@microsoft/signalr';

interface ProtocolMap {
  updateActivity(payload: ActivityRequest): Promise<void>;
  clickChannel(payload: ClickChannel): Promise<void>;
  getChannels(): Promise<ChannelData[] | null>;
  checkMember(payload: CheckMemberResponse): Promise<void>;
  
  getConnectionState(): Promise<HubConnectionState>;
  setConnectionState(payload: HubConnectionState): Promise<void>;
  
  getProfile(): Promise<TelegramUser | null>;
}

export const { sendMessage, onMessage } =
  defineExtensionMessaging<ProtocolMap>();

import { defineExtensionMessaging } from '@webext-core/messaging';
import { ClickChannel, TelegramUser, TelegramUserResponse, WithUserId } from '@/types';
import { HubConnectionState } from '@microsoft/signalr/dist/esm/HubConnection';

interface ProtocolMap {
  updateActivity(payload: WithUserId): Promise<void>;
  clickChannel(payload: ClickChannel): Promise<void>;
  getConnectionState(): Promise<HubConnectionState>;
  setConnectionState(payload: HubConnectionState): Promise<void>;
  getTelegramUser(payload: TelegramUserResponse): Promise<TelegramUser>;
}

export const { sendMessage, onMessage } =
  defineExtensionMessaging<ProtocolMap>();

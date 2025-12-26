import { defineExtensionMessaging } from '@webext-core/messaging';
import { HubConnectionState } from '@microsoft/signalr';
import { ActivityRequest, Button, ChannelData, CheckMemberResponse, ClickButton, ClickChannel, TelegramUser } from './types/messaging';

interface ProtocolMap {
  updateActivity(payload: ActivityRequest): Promise<void>;
  clickChannel(payload: ClickChannel): Promise<void>;
  clickButton(payload: ClickButton): Promise<void>;
  getChannels(): Promise<ChannelData[] | null>;
  checkMember(payload: CheckMemberResponse): Promise<void>;
  getButtons(): Promise<Button[] | null>;
  
  getConnectionState(): Promise<HubConnectionState>;
  setConnectionState(payload: HubConnectionState): Promise<void>;
  
  getProfile(): Promise<TelegramUser | null>;
  initProfile(): Promise<void>;

  getSessionId(): Promise<string | null>;
  getProfileFromStorage(): Promise<TelegramUser | null>;
  enableRulesIfEnabled(): Promise<void>;
}

export const { sendMessage, onMessage } =
  defineExtensionMessaging<ProtocolMap>();

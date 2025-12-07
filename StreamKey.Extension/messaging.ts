import { defineExtensionMessaging } from '@webext-core/messaging';
import { ActivityRequest, ChannelData, ClickChannel } from '@/types';

interface ProtocolMap {
  updateActivity(payload: ActivityRequest): Promise<void>;
  clickChannel(payload: ClickChannel): Promise<void>;
  getChannels(): Promise<ChannelData[] | null>;
  
  // getConnectionState(): Promise<HubConnectionState>;
  // setConnectionState(payload: HubConnectionState): Promise<void>;
  // checkMember(payload: CheckMemberResponse): Promise<void>;
}

export const { sendMessage, onMessage } =
  defineExtensionMessaging<ProtocolMap>();

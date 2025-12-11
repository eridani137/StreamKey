export interface TelegramUser {
  photo_url: string;
  username: string;
  id: number | string;
  is_chat_member?: boolean;
}

export interface ChannelData {
  channelName: string;
  position: number;
  info: ChannelInfo;
}

export interface ChannelInfo {
  thumb: string;
  title: string;
  viewers: number;
  category: string;
  description?: string;
}

export interface WithSessionId {
  sessionId: string;
}

export interface WithUserId {
  userId: string;
}

export interface WithNumberUserId {
  userId: number;
}

export interface WithUserHash {
  userHash: string;
}

export interface WithChannelName {
  channelName: string;
}

export interface ActivityRequest extends WithUserId, WithSessionId {}

export interface TelegramUserResponse extends WithNumberUserId, WithUserHash {}

export interface ClickChannel extends WithUserId, WithChannelName {}

export interface CheckMemberResponse extends WithNumberUserId {}

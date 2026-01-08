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

export interface ActivityRequest extends WithUserId {}

export interface TelegramUserResponse extends WithNumberUserId {}

export interface ClickChannel extends WithUserId, WithChannelName {}

export interface ClickButton extends WithUserId {
  link: string;
  position: ButtonPosition;
}

export interface CheckMemberResponse extends WithNumberUserId {}

export interface Button {
  id: string;
  html: string;
  style: string;
  hoverStyle?: string;
  activeStyle?: string;
  link: string;
  position: ButtonPosition;
}

export enum ButtonPosition {
  StreamBottom = 0,
  LeftTopMenu = 1,
  TopChat = 2
}
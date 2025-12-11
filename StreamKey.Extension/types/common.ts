export type Nullable<T> = T | null;

export interface StreamkeyBlockedElement extends HTMLElement {
  _streamkeyBlockers?: {
    blockAllEvents: (e: Event) => void;
    overlay: HTMLDivElement;
  };
}

export enum TelegramStatus {
  None,
  NotAuthorized,
  NotMember,
  Ok,
}

export interface DeviceInfo {
  userAgent: string;
  language: string;
  platform: string | null;
  product: string | null;
  appName: string | null;
  appCodeName: string | null;
  hardwareConcurrency: number | null;
  timezone: string;
  deviceMemory: number | null;
}

export enum StatusType {
  WORKING = 'working',
  MAINTENANCE = 'maintenance',
}

export type Rule = Awaited<
  ReturnType<typeof browser.declarativeNetRequest.getDynamicRules>
>[number];

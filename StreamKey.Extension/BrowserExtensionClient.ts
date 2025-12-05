import {
  HttpTransportType,
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { MessagePackHubProtocol } from '@microsoft/signalr-protocol-msgpack';
import {
  ActivityRequest,
  ChannelData,
  CheckMemberResponse,
  ClickChannel,
  TelegramUser,
  TelegramUserResponse,
  WithSessionId,
  WithUserId,
} from '@/types';
import Config from '@/config';
import * as utils from '@/utils';
import { sendMessage } from './messaging';
import { loadTwitchRedirectRules, removeAllDynamicRules } from './rules';

class BrowserExtensionClient {
  private connection: HubConnection;
  private sessionId: string;

  // private inactivityTimer: NodeJS.Timeout | null = null;
  // private readonly INACTIVITY_LIMIT_MS = 15 * 60 * 1000;

  constructor() {
    this.sessionId = '';
    this.connection = new HubConnectionBuilder()
      .withUrl(Config.urls.extensionHub, {
        skipNegotiation: true,
        transport: HttpTransportType.WebSockets,
      })
      .withHubProtocol(new MessagePackHubProtocol())
      .configureLogging(LogLevel.Error) // TODO
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds(retryContext) {
          const defaultDelays = [
            0, 2000, 2000, 2000, 2000, 2000
          ];
          if (retryContext.previousRetryCount < defaultDelays.length) {
            return defaultDelays[retryContext.previousRetryCount];
          } else {
            return 5000;
          }
        },
      })
      .build();

    this.connection.on('RequestUserData', async (): Promise<void> => {
      this.resetInactivityTimer();

      await this.connection.invoke('EntranceUserData', {
        SessionId: this.sessionId,
      } as WithSessionId);
    });

    this.connection.on(
      'ReloadUserData',
      async (user: TelegramUser): Promise<void> => {
        this.resetInactivityTimer();

        await utils.initUserProfile(user);
      }
    );

    this.setupConnectionHandlers();
  }

  private setupConnectionHandlers(): void {
    this.connection.onreconnecting(async (error) => {
      console.log('Переподключение...', error);

      try {
        await sendMessage('setConnectionState', this.connectionState);
      } catch {}

      await removeAllDynamicRules();
    });

    this.connection.onreconnected(async () => {
      console.log('Переподключено');

      try {
        await sendMessage('setConnectionState', this.connectionState);
      } catch {}

      const isEnabled = await storage.getItem(Config.keys.extensionState);
      if (isEnabled) await loadTwitchRedirectRules();
    });

    this.connection.onclose(async (error) => {
      console.warn('Соединение закрыто:', error);

      try {
        await sendMessage('setConnectionState', this.connectionState);
      } catch {}

      await removeAllDynamicRules();

      this.clearInactivityTimer();
    });
  }

  private resetInactivityTimer() {
    this.clearInactivityTimer();
    // this.inactivityTimer = setTimeout(async () => {
    //   console.warn('Нет активности — останавливаю соединение');
    //   await this.stop();
    // }, this.INACTIVITY_LIMIT_MS);
  }

  private clearInactivityTimer() {
    // if (this.inactivityTimer) {
    //   clearTimeout(this.inactivityTimer);
    //   this.inactivityTimer = null;
    // }
  }

  public get connectionState(): HubConnectionState {
    return this.connection.state;
  }

  async start(sessionId: string | null = null): Promise<void> {
    if (sessionId) {
      this.sessionId = sessionId;
    }

    if (!this.sessionId) {
      console.error('Нет назначена сессия');
      return;
    }
    if (this.connection.state === HubConnectionState.Connected) return;
    if (this.connection.state !== HubConnectionState.Disconnected) {
      this.waitForState(HubConnectionState.Disconnected);
    }

    await this.connection.start();
    this.resetInactivityTimer();
  }

  private async invokeWithActivity(method: string, ...args: any[]) {
    const state = this.connection.state;

    if (state === HubConnectionState.Disconnected) {
      console.log('Автоподключение после неактивности...');
      await this.start(this.sessionId);
    }

    if (
      state === HubConnectionState.Connecting ||
      state === HubConnectionState.Reconnecting
    ) {
      console.log('Ожидаю завершения соединения...');
      await this.waitForState(HubConnectionState.Connected);
    }

    this.resetInactivityTimer();
    return await this.connection.invoke(method, ...args);
  }

  public waitForState(state: HubConnectionState): Promise<void> {
    return new Promise((resolve) => {
      const check = () => {
        if (this.connection.state === state) {
          resolve();
        } else {
          setTimeout(check, 100);
        }
      };
      check();
    });
  }

  // ================================
  //      PUBLIC API METHODS
  // ================================

  async updateActivity(payload: WithUserId): Promise<void> {
    return await this.invokeWithActivity('UpdateActivity', {
      SessionId: this.sessionId,
      UserId: payload.UserId,
    } as ActivityRequest);
  }

  async getTelegramUser(
    payload: TelegramUserResponse
  ): Promise<TelegramUser | null> {
    return await this.invokeWithActivity('GetTelegramUser', payload);
  }

  async getChannels(): Promise<ChannelData[]> {
    return await this.invokeWithActivity('GetChannels');
  }

  async clickChannel(payload: ClickChannel): Promise<void> {
    await this.invokeWithActivity('ClickChannel', payload);
  }

  async checkMember(payload: CheckMemberResponse): Promise<void> {
    await this.invokeWithActivity('CheckMember', payload);
  }

  async stop() {
    this.clearInactivityTimer();
    await this.connection.stop();
  }
}

const extensionClient = new BrowserExtensionClient();

export default extensionClient;

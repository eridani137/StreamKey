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
          const defaultDelays = [0, 2000, 5000, 10000, 15000, 20000];
          if (retryContext.previousRetryCount < defaultDelays.length) {
            return defaultDelays[retryContext.previousRetryCount];
          } else {
            return 60000;
          }
        },
      })
      .build();

    this.connection.on('RequestUserData', async (): Promise<void> => {
      const requestUserData: WithSessionId = {
        SessionId: this.sessionId,
      };

      console.log('EntranceUserData', requestUserData);
      await this.connection.invoke('EntranceUserData', requestUserData);
    });

    this.connection.on(
      'ReloadUserData',
      async (user: TelegramUser): Promise<void> => {
        console.log('ReloadUserData', user);
        await utils.initUserProfile(user);
      }
    );

    this.setupConnectionHandlers();
  }

  private setupConnectionHandlers(): void {
    this.connection.onreconnecting(async (error) => {
      console.log('Переподключение...', error);
      await sendMessage('setConnectionState', this.connectionState);
      await removeAllDynamicRules();
    });

    this.connection.onreconnected(async () => {
      console.log('Переподключено');
      await sendMessage('setConnectionState', this.connectionState);
      const isEnabled = await storage.getItem(Config.keys.extensionState);
      if (isEnabled) {
        await loadTwitchRedirectRules();
      }
    });

    this.connection.onclose(async (error) => {
      console.warn('Соединение закрыто:', error);
      await sendMessage('setConnectionState', this.connectionState);
      await removeAllDynamicRules();
    });
  }

  public get connectionState(): HubConnectionState {
    return this.connection.state;
  }

  async start(sessionId: string): Promise<void> {
    this.sessionId = sessionId;

    if (this.connection.state === HubConnectionState.Disconnected) {
      try {
        await this.connection.start();
      } catch (error) {
        console.error('Ошибка соединения:', error);
        setTimeout(() => this.start(sessionId), 5000);
      }
    }
  }

  async updateActivity(payload: WithUserId): Promise<void> {
    const userActivity: ActivityRequest = {
      SessionId: this.sessionId,
      UserId: payload.UserId,
    };

    await this.connection.invoke('UpdateActivity', userActivity);
  }

  async getTelegramUser(
    payload: TelegramUserResponse
  ): Promise<TelegramUser | null> {
    return await this.connection.invoke('GetTelegramUser', payload);
  }

  async getChannels(): Promise<ChannelData[]> {
    return await this.connection.invoke('GetChannels');
  }

  async clickChannel(payload: ClickChannel): Promise<void> {
    await this.connection.invoke('ClickChannel', payload);
  }

  async checkMember(payload: CheckMemberResponse): Promise<void> {
    await this.connection.invoke('CheckMember', payload);
  }

  async stop() {
    await this.connection.stop();
  }
}

const extensionClient = new BrowserExtensionClient();

export default extensionClient;

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
          const defaultDelays = [
            0, 2000, 5000, 10000, 15000, 20000, 30000, 40000, 50000, 60000,
          ];
          if (retryContext.previousRetryCount < defaultDelays.length) {
            return defaultDelays[retryContext.previousRetryCount];
          } else {
            return null;
          }
        },
      })
      .build();

    this.connection.on('RequestUserData', async (): Promise<void> => {
      await this.connection.invoke('EntranceUserData', {
        SessionId: this.sessionId,
      } as WithSessionId);
    });

    this.connection.on(
      'ReloadUserData',
      async (user: TelegramUser): Promise<void> => {
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
      if (isEnabled) await loadTwitchRedirectRules();
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
    await this.connection.start();
  }

  async updateActivity(payload: WithUserId): Promise<void> {
    await this.connection.invoke('UpdateActivity', {
      SessionId: this.sessionId,
      UserId: payload.UserId
    } as ActivityRequest);
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

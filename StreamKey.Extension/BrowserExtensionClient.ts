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
          const defaultDelays = [0, 1000, 1000, 1000];
          if (retryContext.previousRetryCount < defaultDelays.length) {
            return defaultDelays[retryContext.previousRetryCount];
          } else {
            return 2000;
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
  }

  public get connectionState(): HubConnectionState {
    return this.connection.state;
  }

  async startWithPersistentRetry(sessionId: string) {
    if (sessionId) this.sessionId = sessionId;
  
    const connect = async () => {
      while (true) {
        try {
          console.log('Попытка подключения...');
          await this.connection.start();
          console.log('SignalR соединение установлено');
          break;
        } catch (err) {
          console.warn('Ошибка подключения. Повтор через 2 секунды...', err);
          await new Promise((resolve) => setTimeout(resolve, 2000));
        }
      }
    };
  
    await connect();
  
    this.connection.onclose(async () => {
      console.warn('Соединение закрыто. Переподключение...');
      await removeAllDynamicRules();
    
      this.sessionId = await utils.createNewSession();
      
      await connect();
    });
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
    await this.connection.invoke('UpdateActivity', {
      SessionId: this.sessionId,
      UserId: payload.UserId,
    } as ActivityRequest);
  }

  async getTelegramUser(
    payload: TelegramUserResponse
  ): Promise<TelegramUser | null> {
    return (await this.connection.invoke('GetTelegramUser', payload)) || null;
  }

  async getChannels(): Promise<ChannelData[] | null> {
    return (await this.connection.invoke('GetChannels')) || null;
  }

  async clickChannel(payload: ClickChannel): Promise<void> {
    await this.connection.invoke('ClickChannel', payload);
  }

  async checkMember(payload: CheckMemberResponse): Promise<void> {
    await this.connection.invoke('CheckMember', payload);
  }
}

const extensionClient = new BrowserExtensionClient();

export default extensionClient;

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
  private shouldReconnect: boolean = true;

  constructor() {
    this.sessionId = '';
    this.connection = this.createConnection();
  }

  private createConnection(): HubConnection {
    const connection = new HubConnectionBuilder()
      .withUrl(Config.urls.extensionHub, {
        skipNegotiation: true,
        transport: HttpTransportType.WebSockets,
      })
      .withHubProtocol(new MessagePackHubProtocol())
      .configureLogging(LogLevel.Error)
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

    connection.on('RequestUserData', async (): Promise<void> => {
      await connection.invoke('EntranceUserData', {
        SessionId: this.sessionId,
      } as WithSessionId);
    });

    connection.on(
      'ReloadUserData',
      async (user: TelegramUser): Promise<void> => {
        await utils.initUserProfile(user);
      }
    );

    this.setupConnectionHandlers(connection);

    return connection;
  }

  private setupConnectionHandlers(connection: HubConnection): void {
    connection.onreconnecting(async (error) => {
      console.log('Переподключение...', error);

      try {
        await sendMessage('setConnectionState', this.connectionState);
      } catch (e) {
        // Ignore messaging errors
      }

      await removeAllDynamicRules();
    });

    connection.onreconnected(async () => {
      console.log('Переподключено');

      try {
        await sendMessage('setConnectionState', this.connectionState);
      } catch (e) {
        // Ignore messaging errors
      }

      const isEnabled = await storage.getItem(Config.keys.extensionState);
      if (isEnabled) await loadTwitchRedirectRules();
    });

    connection.onclose(async (error) => {
      console.warn('Соединение закрыто', error);
      
      await removeAllDynamicRules();

      try {
        await sendMessage('setConnectionState', this.connectionState);
      } catch (e) {
        // Ignore messaging errors
      }

      if (this.shouldReconnect) {
        console.log('Запуск переподключения после закрытия соединения...');
        await this.reconnectAfterClose();
      }
    });
  }

  private async reconnectAfterClose(): Promise<void> {
    let retryCount = 0;
    const initialDelay = 45000;
    const subsequentDelay = 3000;
    
    while (this.shouldReconnect) {
      try {
        const delay = retryCount === 0 ? initialDelay : subsequentDelay;
        console.log(`Повтор подключения через ${delay}ms (попытка ${retryCount + 1})...`);
        
        await new Promise((resolve) => setTimeout(resolve, delay));

        if (!this.sessionId) {
          this.sessionId = await utils.createNewSession();
        }

        console.log('Попытка подключения...');
        await this.connection.start();

        try {
          await sendMessage('setConnectionState', this.connectionState);
        } catch (e) {
          // Ignore messaging errors
        }

        console.log('SignalR соединение восстановлено');
        
        const isEnabled = await storage.getItem(Config.keys.extensionState);
        if (isEnabled) await loadTwitchRedirectRules();
        
        await utils.initUserProfile();
        
        break;
      } catch (err) {
        console.warn('Ошибка переподключения:', err);
        retryCount++;
      }
    }
  }

  public get connectionState(): HubConnectionState {
    return this.connection.state;
  }

  async startWithPersistentRetry(sessionId: string): Promise<void> {
    this.sessionId = sessionId;
    this.shouldReconnect = true;
    
    while (this.shouldReconnect) {
      try {
        console.log('Попытка подключения...');
        await this.connection.start();

        try {
          await sendMessage('setConnectionState', this.connectionState);
        } catch (e) {
          // Ignore messaging errors
        }

        console.log('SignalR соединение установлено');
        break;
      } catch (err) {
        console.warn('Ошибка подключения. Повтор через 2 секунды...', err);
        await new Promise((resolve) => setTimeout(resolve, 2000));
      }
    }
  }

  public async stop(): Promise<void> {
    this.shouldReconnect = false;
    await this.connection.stop();
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
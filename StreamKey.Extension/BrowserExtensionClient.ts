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
  ClickChannel,
  TelegramUser,
  WithSessionId,
  WithUserId,
} from '@/types';
import Config from '@/config';
import * as utils from '@/utils';

class BrowserExtensionClient {
  private connection: HubConnection;
  private sessionId: string;
  private stateCheckInterval: NodeJS.Timeout;

  constructor() {
    this.sessionId = '';
    this.connection = new HubConnectionBuilder()
      .withUrl(Config.urls.extensionHub, {
        transport: HttpTransportType.WebSockets,
      })
      .withHubProtocol(new MessagePackHubProtocol())
      .configureLogging(LogLevel.Warning) // TODO
      .withAutomaticReconnect()
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

    this.stateCheckInterval = setInterval(async () => {
      if (this.connection.state === HubConnectionState.Disconnected && this.sessionId) {
        console.log('Disconnected, auto-restarting...');
        await this.start(this.sessionId);
      }
    }, 10000);
  }

  private setupConnectionHandlers(): void {
    this.connection.onreconnecting((error) => {
      console.log('Переподключение...', error);
    });

    this.connection.onreconnected((connectionId) => {
      console.log('Переподключено, ID:', connectionId);
      if (this.sessionId) {
        this.connection.invoke('EntranceUserData', { SessionId: this.sessionId });
      }
    });

    this.connection.onclose((error) => {
      console.warn('Соединение закрыто:', error);
    });
  }

  public get connectionState(): HubConnectionState {
    return this.connection.state;
  }

  async ping(): Promise<boolean> {
    try {
      await this.connection.invoke('Ping');
      return true;
    } catch (error) {
      console.warn('Ping failed:', error);
      return false;
    }
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

  async clickChannel(payload: ClickChannel): Promise<void> {
    await this.connection.invoke('ClickChannel', payload);
  }

  async stop() {
    await this.connection.stop();
  }
}

const extensionClient = new BrowserExtensionClient();

export default extensionClient;

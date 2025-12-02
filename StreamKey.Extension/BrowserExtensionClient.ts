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
  private reconnectAttempts: number = 0;
  private maxReconnectAttempts: number = 10;
  private isIntentionallyStopped: boolean = false;
  private heartbeatInterval: NodeJS.Timeout | null = null;
  private reconnectTimeout: NodeJS.Timeout | null = null;

  constructor() {
    this.sessionId = '';
    this.connection = this.createConnection();
    this.setupConnectionHandlers();
  }

  private createConnection(): HubConnection {
    return new HubConnectionBuilder()
      .withUrl(Config.urls.extensionHub, {
        withCredentials: true,
        skipNegotiation: true,
        transport: HttpTransportType.WebSockets,
      })
      .withHubProtocol(new MessagePackHubProtocol())
      .configureLogging(LogLevel.Warning)
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Экспоненциальная задержка: 0, 2, 10, 30 секунд, затем каждую минуту
          if (retryContext.previousRetryCount === 0) return 0;
          if (retryContext.previousRetryCount === 1) return 2000;
          if (retryContext.previousRetryCount === 2) return 10000;
          if (retryContext.previousRetryCount === 3) return 30000;
          return 60000;
        },
      })
      .build();
  }

  private setupConnectionHandlers(): void {
    this.connection.on('RequestUserData', async (): Promise<void> => {
      const requestUserData: WithSessionId = {
        SessionId: this.sessionId,
      };
      console.log('EntranceUserData', requestUserData);
      try {
        await this.connection.invoke('EntranceUserData', requestUserData);
      } catch (error) {
        console.error('Error invoking EntranceUserData:', error);
      }
    });

    this.connection.on(
      'ReloadUserData',
      async (user: TelegramUser): Promise<void> => {
        console.log('ReloadUserData', user);
        try {
          await utils.initUserProfile(user);
        } catch (error) {
          console.error('Error reloading user data:', error);
        }
      }
    );

    this.connection.onreconnecting((error) => {
      console.warn('Connection lost. Reconnecting...', error);
      this.stopHeartbeat();
    });

    this.connection.onreconnected((connectionId) => {
      console.log('Connection reestablished. ConnectionId:', connectionId);
      this.reconnectAttempts = 0;
      this.startHeartbeat();
      // Повторная отправка данных пользователя после переподключения
      this.sendUserData();
    });

    this.connection.onclose(async (error) => {
      console.warn('Connection closed', error);
      this.stopHeartbeat();

      // Если остановлено намеренно, не пытаемся переподключиться
      if (this.isIntentionallyStopped) {
        console.log('Connection intentionally stopped');
        return;
      }

      // Попытка ручного переподключения, если автоматический не сработал
      if (this.reconnectAttempts < this.maxReconnectAttempts) {
        this.reconnectAttempts++;
        const delay = Math.min(
          1000 * Math.pow(2, this.reconnectAttempts),
          30000
        );
        console.log(
          `Attempting manual reconnect in ${delay}ms (attempt ${this.reconnectAttempts})`
        );

        this.reconnectTimeout = setTimeout(async () => {
          try {
            await this.start(this.sessionId);
          } catch (err) {
            console.error('Manual reconnect failed:', err);
          }
        }, delay);
      } else {
        console.error('Max reconnect attempts reached');
      }
    });
  }

  private async sendUserData(): Promise<void> {
    if (
      this.connection.state === HubConnectionState.Connected &&
      this.sessionId
    ) {
      try {
        const requestUserData: WithSessionId = {
          SessionId: this.sessionId,
        };
        await this.connection.invoke('EntranceUserData', requestUserData);
      } catch (error) {
        console.error('Error sending user data after reconnect:', error);
      }
    }
  }

  private startHeartbeat(): void {
    // Пинг каждые 30 секунд для поддержания активного соединения
    this.heartbeatInterval = setInterval(async () => {
      if (this.connection.state === HubConnectionState.Connected) {
        try {
          await this.connection.invoke('Ping');
        } catch (error) {
          console.error('Heartbeat ping failed:', error);
        }
      }
    }, 30000);
  }

  private stopHeartbeat(): void {
    if (this.heartbeatInterval) {
      clearInterval(this.heartbeatInterval);
      this.heartbeatInterval = null;
    }
  }

  async start(sessionId: string): Promise<void> {
    if (this.connection.state === HubConnectionState.Connected) {
      console.log('Already connected');
      return;
    }

    this.sessionId = sessionId;
    this.isIntentionallyStopped = false;

    try {
      await this.connection.start();
      console.log('SignalR connected successfully');
      this.reconnectAttempts = 0;
      this.startHeartbeat();
      await this.sendUserData();
    } catch (error) {
      console.error('Error starting connection:', error);
      throw error;
    }
  }

  async updateActivity(payload: WithUserId): Promise<void> {
    if (this.connection.state !== HubConnectionState.Connected) {
      console.warn('Cannot update activity: not connected');
      return;
    }

    const userActivity: ActivityRequest = {
      SessionId: this.sessionId,
      UserId: payload.UserId,
    };

    try {
      await this.connection.invoke('UpdateActivity', userActivity);
    } catch (error) {
      console.error('Error updating activity:', error);
      throw error;
    }
  }

  async clickChannel(payload: ClickChannel): Promise<void> {
    if (this.connection.state !== HubConnectionState.Connected) {
      console.warn('Cannot click channel: not connected');
      return;
    }

    try {
      await this.connection.invoke('ClickChannel', payload);
    } catch (error) {
      console.error('Error clicking channel:', error);
      throw error;
    }
  }

  async stop(): Promise<void> {
    this.isIntentionallyStopped = true;
    this.stopHeartbeat();

    if (this.reconnectTimeout) {
      clearTimeout(this.reconnectTimeout);
      this.reconnectTimeout = null;
    }

    if (this.connection.state === HubConnectionState.Connected) {
      await this.connection.stop();
      console.log('Connection stopped');
    }
  }

  getConnectionState(): HubConnectionState {
    return this.connection.state;
  }

  isConnected(): boolean {
    return this.connection.state === HubConnectionState.Connected;
  }
}

const extensionClient = new BrowserExtensionClient();
export default extensionClient;

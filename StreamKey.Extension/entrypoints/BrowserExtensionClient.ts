import {HttpTransportType, HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel} from "@microsoft/signalr";
import {MessagePackHubProtocol} from '@microsoft/signalr-protocol-msgpack';
import {ActivityRequest, ClickChannel, TelegramUser, WithSessionId, WithUserId} from "@/types";
import Config from "@/config";
import * as utils from "@/utils";

class BrowserExtensionClient {
    private connection: HubConnection;
    private sessionId: string;

    constructor() {
        this.sessionId = "";
        this.connection = new HubConnectionBuilder()
            .withUrl(Config.urls.extensionHub, {
                withCredentials: true,
                skipNegotiation: true,
                transport: HttpTransportType.WebSockets,
            })
            .withHubProtocol(new MessagePackHubProtocol())
            .configureLogging(LogLevel.Debug) // TODO
            .withAutomaticReconnect()
            .build();

        this.connection.on('RequestUserData', async () : Promise<void> => {
            const requestUserData: WithSessionId = {
                SessionId: this.sessionId,
            };

            console.log('EntranceUserData', requestUserData);
            await this.connection.invoke('EntranceUserData', requestUserData);
        });

        this.connection.on('ReloadUserData', async (user: TelegramUser) : Promise<void> => {
            console.log('ReloadUserData', user);
            await utils.initUserProfile(user);
        });

        this.connection.onclose(() => {
            console.warn('Connection closed');
        });
    }

    async start(sessionId: string): Promise<void> {
        if (this.connection.state === HubConnectionState.Connected) return;

        this.sessionId = sessionId;

        await this.connection.start();
    }

    async updateActivity(payload: WithUserId): Promise<void> {
        const userActivity: ActivityRequest = {
            SessionId: this.sessionId,
            UserId: payload.UserId,
        };

        await this.connection.invoke('UpdateActivity', userActivity)
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
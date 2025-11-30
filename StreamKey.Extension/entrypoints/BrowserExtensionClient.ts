import {HttpTransportType, HubConnection, HubConnectionBuilder, LogLevel} from "@microsoft/signalr";
import {MessagePackHubProtocol} from '@microsoft/signalr-protocol-msgpack';
import {UserData} from "@/types";
import Config from "@/config";

export class BrowserExtensionClient {
    private connection: HubConnection;
    private registrationTimeoutMs = 7000;
    private registrationTimeoutHandle: any;

    constructor() {
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

        this.connection.on('RequestUserData', this.handleRequestUserData.bind(this));

        this.connection.onclose(() => {
            console.warn('Connection closed');
            clearTimeout(this.registrationTimeoutHandle);
        });
    }

    async start() {
        await this.connection.start();

        this.registrationTimeoutHandle = setTimeout(() => {
            console.warn('Пользователь не предоставил данные в течение 7 секунд, отключаем.');
            this.connection.stop();
        }, this.registrationTimeoutMs);
    }

    async handleRequestUserData() {
        const userData: UserData | null = await this.getUserData();

        if (userData === null) {
            console.warn('Получены невалидные данные пользователя');
            await this.connection.stop();
            return;
        }

        clearTimeout(this.registrationTimeoutHandle);

        await this.connection.invoke('EntranceUserData', userData);
    }

    async getUserData(): Promise<UserData | null> {
        const sessionId = await storage.getItem<string>(Config.keys.sessionId);

        if (!sessionId) {
            return null;
        }

        return {
            sessionId,
        };
    }

    async stop() {
        await this.connection.stop();
    }
}

const extensionClient = new BrowserExtensionClient();

export default extensionClient;
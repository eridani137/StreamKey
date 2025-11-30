import {HttpTransportType, HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel} from "@microsoft/signalr";
import {MessagePackHubProtocol} from '@microsoft/signalr-protocol-msgpack';
import {UserData} from "@/types";
import Config from "@/config";

class BrowserExtensionClient {
    private connection: HubConnection;
    private registrationTimeoutMs = 7000;
    private registrationTimeoutHandle: any;
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

        this.connection.on('RequestUserData', this.handleRequestUserData.bind(this));

        this.connection.onclose(() => {
            console.warn('Connection closed');
            clearTimeout(this.registrationTimeoutHandle);
        });
    }

    async start(sessionId: string) : Promise<void> {
        if (this.connection.state === HubConnectionState.Connected) return;

        this.sessionId = sessionId;

        await this.connection.start();

        this.registrationTimeoutHandle = setTimeout(() => {
            console.warn('Пользователь не предоставил данные в течение 7 секунд, отключаем.');
            this.connection.stop();
        }, this.registrationTimeoutMs);
    }

    async handleRequestUserData() {
        const userData: UserData = {
            SessionId: this.sessionId,
        };

        clearTimeout(this.registrationTimeoutHandle);

        await this.connection.invoke('EntranceUserData', userData);
    }

    async stop() {
        await this.connection.stop();
    }
}

export default BrowserExtensionClient
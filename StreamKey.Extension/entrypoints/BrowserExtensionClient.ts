import {HttpTransportType, HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel} from "@microsoft/signalr";
import {MessagePackHubProtocol} from '@microsoft/signalr-protocol-msgpack';
import {TelegramUser, UserActivity, UserData} from "@/types";
import Config from "@/config";
import * as utils from "@/utils";

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

        this.connection.on('RequestUserData', async () : Promise<void> => {
            const userData: UserData = {
                SessionId: this.sessionId,
            };

            clearTimeout(this.registrationTimeoutHandle);

            console.log('EntranceUserData', userData);
            await this.connection.invoke('EntranceUserData', userData);
        });
        group.MapPost<TelegramUserDto>("/user/set-data",
            async (TelegramUserDto dto, Guid sessionId, Hub<IBrowserExtensionHub> extensionHub, ILogger<Telegram> logger) =>
        {
            var client = BrowserExtensionHub.Users.FirstOrDefault(kvp => kvp.Value.SessionId == sessionId);
            if (client.Key is null)
            {
                return Results.NotFound();
            }

            logger.LogInformation("Found client: {@Client}", client);

            await extensionHub.Clients.Client(client.Key).ReloadUserData(dto);

            return Results.Ok();
        });
        this.connection.on('ReloadUserData', async (user: TelegramUser) : Promise<void> => {
            console.log('ReloadUserData', user);
            await utils.initUserProfile(user);
        });

        this.connection.onclose(() => {
            console.warn('Connection closed');
            clearTimeout(this.registrationTimeoutHandle);
        });
    }

    async start(sessionId: string): Promise<void> {
        if (this.connection.state === HubConnectionState.Connected) return;

        this.sessionId = sessionId;

        await this.connection.start();

        this.registrationTimeoutHandle = setTimeout(() => {
            console.warn('Пользователь не предоставил данные в течение 7 секунд, отключаем.');
            this.connection.stop();
        }, this.registrationTimeoutMs);
    }

    async updateActivity(userId: string): Promise<void> {
        const userActivity: UserActivity = {
            SessionId: this.sessionId,
            UserId: userId,
        };

        await this.connection.invoke('UpdateActivity', userActivity)
    }

    async stop() {
        await this.connection.stop();
    }
}

const extensionClient = new BrowserExtensionClient();

export default extensionClient;
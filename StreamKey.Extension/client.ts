import Config from './config';
import { ActivityRequest, ClickChannel, ChannelData, TelegramUserResponse, TelegramUser } from './types/messaging';

class HttpClient {
  async updateActivity(payload: ActivityRequest) {
    const response = await fetch(`${Config.urls.apiUrl}/activity/update`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        sessionId: payload.sessionId,
        userId: payload.userId,
      }),
    });

    if (!response.ok) {
      console.error('[updateActivity] Сервер вернул ошибку:', response.status);
    }

    console.log('Активность обновлена', payload.userId);
  }

  async clickChannel(payload: ClickChannel) {
    const response = await fetch(`${Config.urls.apiUrl}/activity/click`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        channelName: payload.channelName,
        userId: payload.userId,
      }),
    });

    if (!response.ok) {
      console.warn('[clickChannel] Сервер вернул ошибку: ' + response.status);
    }

    console.log('Клик на канал', payload.channelName);
  }

  async getChannels(): Promise<ChannelData[] | null> {
    const response = await fetch(`${Config.urls.apiUrl}/channels`, {
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      console.warn('[getChannels] Сервер вернул ошибку: ' + response.status);
      return null;
    }

    const channels = (await response.json()) as ChannelData[];

    return channels;
  }

  async getTelegramUser(
    requestData: TelegramUserResponse
  ): Promise<TelegramUser | null> {
    const response = await fetch(
      `${Config.urls.apiUrl}/telegram/user/${requestData.userId}`,
      {
        method: 'GET',
        headers: { 'Content-Type': 'application/json' },
      }
    );

    if (!response.ok) {
      console.log('Сервер вернул ошибку: ' + response.status);
      return null;
    }

    const text = await response.text();

    const data = text ? (JSON.parse(text) as TelegramUser) : null;

    return data;
  }
}

const client = new HttpClient();

export default client;

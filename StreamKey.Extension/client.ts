import Config from './config';
import { ActivityRequest, ChannelData, ClickChannel } from './types';

class HttpClient {
  async updateActivity(payload: ActivityRequest) {
    const response = await fetch(`${Config.urls.apiUrl}/activity/update`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        sessionId: payload.SessionId,
        userId: payload.UserId,
      }),
    });

    if (!response.ok) {
      console.error('[updateActivity] Сервер вернул ошибку:', response.status);
    }

    console.log('Активность обновлена', payload.UserId);
  }

  async clickChannel(payload: ClickChannel) {
    const response = await fetch(`${Config.urls.apiUrl}/activity/click`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        channelName: payload.ChannelName,
        userId: payload.UserId,
      }),
    });

    if (!response.ok) {
      console.warn('[clickChannel] Сервер вернул ошибку: ' + response.status);
    }

    console.log('Клик на канал', payload.ChannelName);
  }

  async getChannels(): Promise<ChannelData[] | null> {
    const response = await fetch(`${Config.urls.apiUrl}/channels`, {
      headers: {
        'Content-Type': 'application/json',
      }
    });

    if (!response.ok) {
      console.warn('[getChannels] Сервер вернул ошибку: ' + response.status);
      return null;
    }

    const channels = (await response.json()) as ChannelData[];

    return channels;
  }
}

const client = new HttpClient();

export default client;

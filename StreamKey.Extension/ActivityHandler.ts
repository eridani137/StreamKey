import Config from '@/config';
import { sendMessage } from '@/messaging';
import { getTwitchUserId } from '@/utils';
import { WithUserId } from '@/types';

export class ActivityHandler {
  private ctx: any = null;

  async init(ctx: any) {
    this.ctx = ctx;

    this.ctx.setInterval(async () => {
      await this.updateActivity();
    }, 180000);
  }

  async updateActivity() {
    const sessionId = await storage.getItem(Config.keys.sessionId);
    const userId = getTwitchUserId();

    console.log('Обновление активности');
    console.log('sessionId', sessionId);
    console.log('userId', userId);

    if (sessionId && userId) {
      // await sendMessage('updateActivity', { UserId: userId } as WithUserId);

      const request = await fetch(`${Config.urls.apiUrl}/activity/update`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ sessionId: sessionId, userId: userId }),
      });

      if (!request.ok) {
        console.error('Сервер вернул ошибку:', request.status);
        return undefined;
      }

      console.log('Активность обновлена', userId);
    }
  }
}

const activityHandler = new ActivityHandler();
export default activityHandler;

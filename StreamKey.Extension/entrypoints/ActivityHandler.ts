import { Config } from '@/config';

export class ActivityHandler {
  private ctx: any = null;

  init(ctx: any) {
    this.ctx = ctx;

    this.ctx.setTimeout(async () => {
      await this.updateActivity();
    }, 5000);

    this.ctx.setInterval(async () => {
      await this.updateActivity();
    }, 180000);
  }

  async updateActivity() {
    const sessionId = await storage.getItem(Config.keys.sessionId);
    const userId = localStorage.getItem('local_copy_unique_id');

    if (sessionId && userId) {
      const updateActivity = await fetch(
        `${Config.urls.apiUrl}/activity/update`,
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ sessionId: sessionId, userId: userId }),
        }
      );

      if (!updateActivity.ok) {
        console.error('Сервер вернул ошибку:', updateActivity.status);
        return undefined;
      }

      const text = await updateActivity.text();
      const data = text ? JSON.parse(text) : undefined;
      console.log('Обновление активности:', data);
    }
  }
}

const activityHandler = new ActivityHandler();

export default activityHandler;

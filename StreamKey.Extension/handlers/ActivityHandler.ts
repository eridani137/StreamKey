import { sendMessage } from '@/messaging';
import { ActivityRequest } from '@/types/messaging';
import { getTwitchUserId } from '@/utils';

export class ActivityHandler {
  private ctx: any = null;

  async init(ctx: any) {
    this.ctx = ctx;

    await this.updateActivity();
    this.ctx.setInterval(async () => {
      await this.updateActivity();
    }, 60000);
  }

  async updateActivity() {
    const sessionId = await sendMessage('getSessionId');
    const userId = getTwitchUserId();

    console.log('Обновление активности');

    console.log('sessionId', sessionId);
    console.log('userId', userId);

    if (sessionId && userId) {
      await sendMessage('updateActivity', {
        userId: userId
      } as ActivityRequest);
    }
  }
}

const activityHandler = new ActivityHandler();
export default activityHandler;

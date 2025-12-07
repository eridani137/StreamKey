import Config from '@/config';
import { sendMessage } from '@/messaging';
import { ActivityRequest, WithUserId } from '@/types';

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
    const userId = (await browser.cookies.get({ url: Config.urls.twitchUrl, name: 'unique_id' }))?.value;

    console.log('Обновление активности');
    console.log('sessionId', sessionId);
    console.log('userId', userId);

    if (sessionId && userId) {
      await sendMessage('updateActivity', { SessionId: sessionId, UserId: userId } as ActivityRequest);
    }
  }
}

const activityHandler = new ActivityHandler();
export default activityHandler;

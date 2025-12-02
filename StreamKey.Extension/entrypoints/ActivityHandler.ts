import Config from '@/config';
import {sendMessage} from "@/messaging";
import {getTwitchUserId} from "@/utils";

export class ActivityHandler {
    private ctx: any = null;

    async init(ctx: any) {
        this.ctx = ctx;

        this.ctx.setInterval(async () => {
            await this.updateActivity();
        }, 60000);
    }

    async updateActivity() {
        const sessionId = await storage.getItem(Config.keys.sessionId);
        const userId = getTwitchUserId();

        console.log('Обновление активности');
        console.log('sessionId', sessionId);
        console.log('userId', userId);

        if (sessionId && userId) {
            await sendMessage('updateActivity', userId);

            console.log('Активность обновлена');
        }
    }
}

const activityHandler = new ActivityHandler();
export default activityHandler;

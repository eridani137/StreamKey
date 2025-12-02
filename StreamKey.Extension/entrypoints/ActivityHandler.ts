import Config from '@/config';
import {sendMessage} from "@/messaging";

export class ActivityHandler {
    private ctx: any = null;

    async init(ctx: any) {
        this.ctx = ctx;

        await this.updateActivity();

        this.ctx.setInterval(async () => {
            await this.updateActivity();
        }, 180000);
    }

    async updateActivity() {
        const sessionId = await storage.getItem(Config.keys.sessionId);
        const userId = localStorage.getItem(Config.keys.twId);

        console.log('Обновление активности');
        console.log('sessionId', sessionId);
        console.log('userId', userId);

        if (sessionId && userId) {
            // const updateActivity = await fetch(
            //     `${Config.urls.apiUrl}/activity/update`,
            //     {
            //         method: 'POST',
            //         headers: {'Content-Type': 'application/json'},
            //         body: JSON.stringify({sessionId: sessionId, userId: userId}),
            //     }
            // );
            //
            // if (!updateActivity.ok) {
            //     console.error('Сервер вернул ошибку:', updateActivity.status);
            //     return undefined;
            // }

            await sendMessage('updateActivity', userId);

            console.log('Активность обновлена');
        }
    }
}

const activityHandler = new ActivityHandler();
export default activityHandler;

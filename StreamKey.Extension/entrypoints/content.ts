import './style.css';
import qualityMenu from './QualityMenu';
import activeChannels from './ActiveChannels';
import activityHandler from './ActivityHandler';
import {waitForElement} from "@/utils";

export default defineContentScript({
    matches: ['https://*.twitch.tv/*'],
    runAt: 'document_idle',
    async main(ctx) {
        await runScripts(ctx);
    },
});

async function runScripts(ctx: any) {
    await activeChannels.init(ctx);
    console.log('Поиск плеера');
    const videoPlayer = await waitForElement("//div[@data-a-target='video-player']");
    if (videoPlayer) {
        await qualityMenu.init(ctx);
        await activityHandler.init(ctx);
    } else {
        console.log('Плеер не найден');
    }
}
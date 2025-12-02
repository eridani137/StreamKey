import './style.css';
import qualityMenu from './QualityMenu';
import activeChannels from './ActiveChannels';
import activityHandler from './ActivityHandler';

export default defineContentScript({
    matches: ['https://*.twitch.tv/*'],
    async main(ctx) {
        await runScripts(ctx);
    },
});

async function runScripts(ctx: any) {
    await qualityMenu.init(ctx);
    await activeChannels.init(ctx);
    await activityHandler.init(ctx);
}
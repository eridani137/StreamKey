import './style.css';
import qualityMenu from '@/QualityMenu';
import activeChannels from '@/ActiveChannels';
import activityHandler from '@/ActivityHandler';

export default defineContentScript({
  matches: ['https://*.twitch.tv/*'],
  runAt: 'document_idle',
  async main(ctx) {
    await runScripts(ctx);
  },
});

async function runScripts(ctx: any) {
  await activeChannels.init(ctx);
  await qualityMenu.init(ctx);
  await activityHandler.init(ctx);
}

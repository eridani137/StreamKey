import activeChannels from '@/handlers/ActiveChannels';
import activityHandler from '@/handlers/ActivityHandler';
import qualityMenu from '@/handlers/QualityMenu';
import buttons from '@/handlers/Buttons';
import './style.css';

export default defineContentScript({
  matches: ['https://*.twitch.tv/*'],
  runAt: 'document_idle',
  async main(ctx) {
    await runScripts(ctx);
  },
});

async function runScripts(ctx: any) {
  activeChannels.init(ctx);
  await qualityMenu.init(ctx);
  await activityHandler.init(ctx);
  await buttons.init(ctx);
}

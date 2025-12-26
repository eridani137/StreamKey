import activeChannels from '@/handlers/ActiveChannels';
import activityHandler from '@/handlers/ActivityHandler';
import qualityMenu from '@/handlers/QualityMenu';
import buttonsMenu from '@/handlers/ButtonsMenu';
import './style.css';
import { sendMessage } from '@/messaging';

export default defineContentScript({
  matches: ['https://*.twitch.tv/*'],
  runAt: 'document_idle',
  async main(ctx) {
    await runScripts(ctx);
  },
});

async function runScripts(ctx: any) {
  await sendMessage('enableRulesIfEnabled');
  activeChannels.init(ctx);
  await qualityMenu.init(ctx);
  await activityHandler.init(ctx);
  await buttonsMenu.init(ctx);
}

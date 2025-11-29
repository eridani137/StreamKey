import './style.css';
import qualityMenu from './QualityMenu';
import activeChannels from './ActiveChannels';
import activityHandler from './ActivityHandler';

export default defineContentScript({
  matches: ['https://*.twitch.tv/*'],
  main(ctx) {
    qualityMenu.init(ctx);
    activeChannels.init(ctx);
    activityHandler.init(ctx);
  },
});

import './style.css';
import qualityMenu from './QualityMenu';

export default defineContentScript({
  matches: ['https://*.twitch.tv/*'],
  main(ctx) {
    console.log('Hello content.');

    qualityMenu.init(ctx);
  },
});

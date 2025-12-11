import Config from '@/config';
import { Nullable } from './types/common';
import { TelegramUser } from './types/messaging';
import { sendMessage } from './messaging';

export class QualityMenu {
  private ctx: any = null;
  private observer: Nullable<MutationObserver> = null;
  private styleElement: Nullable<HTMLStyleElement> = null;
  private isProcessing = false;

  async init(ctx: any): Promise<void> {
    this.ctx = ctx;
    this.startObserver();
    await this.setDefault();
  }

  getResolutionElements(): HTMLElement[] {
    const items = document.querySelectorAll(
      `${Config.qualityMenu.qualityMenuSelectors.menuContainer} ${Config.qualityMenu.qualityMenuSelectors.radioItems}`
    );

    return Array.from(items)
      .map((radio) =>
        radio.querySelector<HTMLElement>(
          Config.qualityMenu.qualityMenuSelectors.radioLabel
        )
      )
      .filter((label): label is HTMLElement => {
        if (!label) return false;
        const text = label.textContent ?? '';
        const match = text.match(/(\d{3,4})p/i);
        return (
          match !== null &&
          parseInt(match[1], 10) >= Config.qualityMenu.minResolution
        );
      });
  }

  addInstruction(): void {
    const targetMenu = document.querySelector<HTMLElement>(
      Config.qualityMenu.qualityMenuSelectors.menuContainer
    );
    if (!targetMenu) return;

    if (targetMenu.querySelector('.tw-in-feature-notification')) return;

    const html = `
    <div class="Layout-sc-1xcs6mc-0 goosYB">
        <div class="ScInFeatureNotification-sc-a4oqgt-1 MSbwY tw-in-feature-notification" role="alert">
            <div class="Layout-sc-1xcs6mc-0 kGRpNK">
                <div class="Layout-sc-1xcs6mc-0 fHdBNk">
                <div class="Layout-sc-1xcs6mc-0 fIxYas">
                    <div class="Layout-sc-1xcs6mc-0 evOsLv" style="width: 100%;">
                    <div class="Layout-sc-1xcs6mc-0 dZHLjx">
                        <div class="Layout-sc-1xcs6mc-0 bwtGga">
                        <div class="Layout-sc-1xcs6mc-0 efdWMj notification-image-container">
                            <img src="${Config.qualityMenu.instructionUrl}" alt="instruction" class="notification-image">
                        </div>
                        </div>
                    </div>
                    </div>
                </div>
                </div>
            </div>
        </div>
    </div>
    `;

    targetMenu.insertAdjacentHTML('afterbegin', html);

    const img = targetMenu.querySelector<HTMLImageElement>(
      '.notification-image'
    );
    if (img) {
      img.style.cursor = 'pointer';
      img.addEventListener('click', (e) => {
        e.stopPropagation();
        window.open(Config.urls.telegramChannelUrl, '_blank');
      });
    }
  }

  async block2KResolutionElement(): Promise<void> {
    const tgUser = await sendMessage('getProfileFromStorage');
    if (tgUser?.is_chat_member) return;

    this.addInstruction();

    const elements = this.getResolutionElements();

    const element1440 = elements.find((label) => {
      const text = label.textContent ?? '';
      return text.includes('1440');
    });
    if (!element1440) return;

    const radioItem = element1440.closest<HTMLElement>(
      Config.qualityMenu.qualityMenuSelectors.radioItems
    );
    if (!radioItem) return;

    const flexContainer = radioItem.parentElement;
    if (!flexContainer || flexContainer.style.display !== 'flex') {
      return;
    }

    if (flexContainer.getAttribute('data-streamkey-blocked') === 'true') {
      return;
    }

    const input = radioItem.querySelector<HTMLInputElement>(
      "input[type='radio']"
    );

    if (!input) return;

    if (input.checked) {
      await this.autoSwitch();
    }

    flexContainer.setAttribute('data-streamkey-blocked', 'true');
    flexContainer.style.opacity = '0.5';
    flexContainer.style.cursor = 'not-allowed';
    flexContainer.style.position = 'relative';

    input.disabled = true;

    const labelElement = radioItem.querySelector<HTMLLabelElement>('label');
    if (labelElement) {
      labelElement.removeAttribute('for');
    }

    const blockAllEvents = (e: Event): void => {
      e.preventDefault();
      e.stopPropagation();
      e.stopImmediatePropagation();
    };

    const events = [
      'click',
      'mousedown',
      'mouseup',
      'mousemove',
      'pointerdown',
      'pointerup',
      'pointermove',
      'touchstart',
      'touchend',
      'touchmove',
      'change',
      'input',
    ];

    events.forEach((eventType) => {
      flexContainer.addEventListener(eventType, blockAllEvents, true);
    });

    const overlay = document.createElement('div');
    overlay.style.cssText = `
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        cursor: not-allowed;
        z-index: 10;
        background: transparent;
    `;

    flexContainer.insertBefore(overlay, flexContainer.firstChild);

    events.forEach((eventType) => {
      overlay.addEventListener(eventType, blockAllEvents, true);
    });

    if (input.checked) {
      input.checked = false;
    }

    (flexContainer as any)._streamkeyBlockers = { blockAllEvents, overlay };
  }

  async autoSwitch(): Promise<void> {
    const tgUser = await sendMessage('getProfileFromStorage');
    if (tgUser?.is_chat_member) return;

    await this.setDefault();

    const elements = this.getResolutionElements();

    const currentSelected = elements.find((label) => {
      const radioItem = label.closest<HTMLElement>(
        Config.qualityMenu.qualityMenuSelectors.radioItems
      );
      if (!radioItem) return false;

      const input = radioItem.querySelector<HTMLInputElement>(
        "input[type='radio']"
      );
      return input?.checked === true;
    });

    if (!currentSelected) return;

    const currentText = currentSelected.textContent ?? '';
    const currentMatch = currentText.match(/(\d{3,4})p/i);

    if (!currentMatch || parseInt(currentMatch[1], 10) !== 1440) return;

    const lowerResolutions = elements
      .map((label) => {
        const text = label.textContent ?? '';
        const match = text.match(/(\d{3,4})p/i);
        if (!match) return null;

        const resolution = parseInt(match[1], 10);
        const radioItem = label.closest<HTMLElement>(
          Config.qualityMenu.qualityMenuSelectors.radioItems
        );

        return {
          resolution,
          label,
          radioItem,
        };
      })
      .filter(
        (item): item is NonNullable<typeof item> =>
          item !== null &&
          item.resolution < 1440 &&
          item.radioItem?.getAttribute('data-streamkey-blocked') !== 'true'
      )
      .sort((a, b) => b.resolution - a.resolution);

    if (lowerResolutions.length === 0) return;

    const targetResolution = lowerResolutions[0];
    const input = targetResolution.radioItem?.querySelector<HTMLInputElement>(
      "input[type='radio']"
    );

    if (input && !input.disabled) {
      input.click();
      console.log(
        `Auto-switched from 1440p to ${targetResolution.resolution}p`
      );
    }
  }

  async setDefault(): Promise<void> {
    const tgUser = await sendMessage('getProfileFromStorage');
    if (tgUser?.is_chat_member) return;

    localStorage.setItem('s-qs-ts', String(Math.floor(Date.now())));
    localStorage.setItem('video-quality', '{"default":"1080p60"}');
  }

  createBadge(): HTMLElement {
    const container = document.createElement('span');
    container.classList.add('custom-radio-badge');

    const prefix = document.createElement('span');
    prefix.textContent = 'с помощью ';

    const clickable = document.createElement('span');
    clickable.textContent = Config.qualityMenu.badgeText;
    clickable.classList.add('custom-radio-badge-clickable');
    clickable.addEventListener('click', (e) => {
      e.stopPropagation();
      window.open(Config.urls.telegramChannelUrl, '_blank');
    });

    container.append(prefix, clickable);
    return container;
  }

  handleLabelClick(label: HTMLElement): void {
    document.querySelectorAll<HTMLElement>('.custom-radio').forEach((el) => {
      el.classList.remove('selected');
    });
    label.classList.add('selected');
  }

  enhanceLabel(label: HTMLElement): void {
    if (label.dataset.listenerAttached === 'true') return;

    label.classList.add('custom-radio');

    const text = label.textContent ?? '';
    if (text.includes('1080') || text.includes('1440')) {
      const next = label.nextElementSibling as HTMLElement | null;
      if (!(next && next.classList.contains('custom-radio-badge'))) {
        const badge = this.createBadge();
        label.parentNode?.insertBefore(badge, label.nextSibling);
      }
    }

    label.addEventListener('click', () => this.handleLabelClick(label));
    label.dataset.listenerAttached = 'true';
  }

  applyEnhancements(): boolean {
    const labels = this.getResolutionElements();
    labels.forEach((label) => this.enhanceLabel(label));
    return labels.length > 0;
  }

  startObserver(): void {
    if (this.observer) return;

    this.observer = new MutationObserver(() => {
      if (this.isProcessing) return;

      this.isProcessing = true;

      this.ctx.setTimeout(async () => {
        this.observer!.disconnect();

        try {
          this.applyEnhancements();
          await this.block2KResolutionElement();
        } finally {
          this.observer!.observe(document.body, {
            childList: true,
            subtree: true,
          });
          this.isProcessing = false;
        }
      }, 300);
    });

    this.observer.observe(document.body, {
      childList: true,
      subtree: true,
    });
  }

  destroy(): void {
    this.observer?.disconnect();
    this.observer = null;

    this.styleElement?.remove();
    this.styleElement = null;

    document.querySelectorAll<HTMLElement>('.custom-radio').forEach((el) => {
      el.classList.remove('custom-radio', 'selected');
      delete el.dataset.listenerAttached;
    });

    document.querySelectorAll('.custom-radio-badge').forEach((b) => b.remove());

    document.querySelectorAll('[data-streamkey-blocked]').forEach((el) => {
      el.removeAttribute('data-streamkey-blocked');
    });

    document.querySelectorAll('.tw-in-feature-notification').forEach((el) => {
      el.closest('.goosYB')?.remove();
    });
  }
}

const qualityMenu = new QualityMenu();

export default qualityMenu;

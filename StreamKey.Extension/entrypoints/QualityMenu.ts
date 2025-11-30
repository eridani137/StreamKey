import { Nullable, StreamkeyBlockedElement } from '@/types';
import Config from '@/config';

export class QualityMenu {
  private ctx: any = null;
  private observer: Nullable<MutationObserver> = null;
  private styleElement: Nullable<HTMLStyleElement> = null;
  private isProcessing = false;

  init(ctx: any): void {
    this.ctx = ctx;
    this.startObserver();
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
    const tgUser = await browser.runtime.sendMessage({
      type: Config.messaging.getUserProfile,
    });
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

    if (radioItem.getAttribute('data-streamkey-blocked') === 'true') {
      return;
    }

    const input = radioItem.querySelector<HTMLInputElement>(
      "input[type='radio']"
    );
    const labelElement = radioItem.querySelector<HTMLLabelElement>('label');

    if (!input) return;

    radioItem.setAttribute('data-streamkey-blocked', 'true');
    input.disabled = true;
    input.readOnly = true;

    if (labelElement) {
      labelElement.removeAttribute('for');
      labelElement.style.pointerEvents = 'none';
    }

    const clone = radioItem.cloneNode(true) as StreamkeyBlockedElement;
    clone.setAttribute('data-streamkey-blocked', 'true');
    (clone.style as any).opacity = '0.5';
    clone.style.cursor = 'not-allowed';

    radioItem.parentNode?.replaceChild(clone, radioItem);

    const blockClick = (e: Event): false | void => {
      if (clone.contains(e.target as Node)) {
        e.preventDefault();
        e.stopPropagation();
        e.stopImmediatePropagation();
        return false;
      }
    };

    document.addEventListener('click', blockClick, true);
    document.addEventListener('mousedown', blockClick, true);
    document.addEventListener('mouseup', blockClick, true);
    document.addEventListener('pointerdown', blockClick, true);
    document.addEventListener('pointerup', blockClick, true);

    clone._streamkeyBlockers = { blockClick };
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

      this.ctx.setTimeout(() => {
        this.observer!.disconnect();

        try {
          this.applyEnhancements();
          this.block2KResolutionElement();
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
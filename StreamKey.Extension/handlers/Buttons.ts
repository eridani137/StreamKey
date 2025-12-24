import { sendMessage } from '@/messaging';
import { Nullable } from '@/types/common';
import { Button, ClickButton } from '@/types/messaging';
import { getTwitchUserId, handleClickAndNavigate } from '@/utils';

export class Buttons {
  private ctx: any = null;
  private observer: Nullable<MutationObserver> = null;
  private isProcessing = false;
  private buttonCounter = 0;
  private buttons: Button[] = [];

  async init(ctx: any): Promise<void> {
    this.ctx = ctx;
    await this.fetchButtons();
    ctx.setInterval(async () => this.fetchButtons(), 180000);
    this.startObserver();
  }

  private async fetchButtons() {
    this.buttons = (await sendMessage('getButtons')) || [];
    console.log('[Buttons] Fetch', this.buttons.length, 'items');
  }

  async addButtons() {
    if (this.buttons.length == 0) return;

    this.buttons.forEach((b) => this.addButton(b));
  }

  addButton(data: Button) {
    const spacer = document.querySelector(
      '.metadata-layout__secondary-button-spacing'
    );
    if (!spacer) return;

    const existingButton = spacer.parentNode?.querySelector(
      `button[link="${data.link}"]`
    );
    if (existingButton) return;

    this.buttonCounter++;
    const uniqueClass = `streamkey-livechannel-button-${this.buttonCounter}`;

    const styleEl = document.createElement('style');
    styleEl.id = `style-${uniqueClass}`;
    styleEl.textContent = `
      .${uniqueClass} {
        ${data.style}
      }
      .${uniqueClass}:hover {
        ${data.hoverStyle || ''}
      }
      .${uniqueClass}:active {
        ${data.activeStyle || ''}
      }
    `;
    document.head.appendChild(styleEl);

    const button = document.createElement('button');
    button.className = uniqueClass;
    button.innerHTML = data.html;
    button.setAttribute('link', data.link);

    button.addEventListener('click', (event: MouseEvent) => {
      handleClickAndNavigate(
        event,
        data.link,
        (url) => window.open(url, '_blank'),
        async (userId) => {
          await sendMessage('clickButton', {
            link: data.link,
            userId
          } as ClickButton);
        }
      );
    });

    spacer.parentNode?.insertBefore(button, spacer.nextSibling);
  }

  startObserver(): void {
    if (this.observer) return;

    this.observer = new MutationObserver(() => {
      if (this.isProcessing) return;

      this.isProcessing = true;

      this.ctx.setTimeout(async () => {
        this.observer!.disconnect();

        try {
          await this.addButtons();
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
}

const buttons = new Buttons();

export default buttons;

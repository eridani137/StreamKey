import Config from '@/config';
import { sendMessage } from '@/messaging';
import { Nullable } from '@/types/common';
import { Button, ButtonPosition, ClickButton } from '@/types/messaging';
import { handleClickAndNavigate } from '@/utils';

export class LeftTopMenuButtons {
  private ctx: any = null;
  private observer: Nullable<MutationObserver> = null;
  private isProcessing = false;
  private buttonCounter = 0;
  private buttons: Button[] = [];

  async init(ctx: any): Promise<void> {
    this.ctx = ctx;
    await this.fetchButtons();
    ctx.setInterval(async () => this.fetchButtons(), Config.intervals.updateStreamBottomButtons);
    this.startObserver();
  }

  private async fetchButtons() {
    this.buttons =
      (await sendMessage('getButtons', ButtonPosition.LeftTopMenu)) || [];
    console.log('[LeftTopMenu] Fetch', this.buttons.length, 'items');
  }

  addButtons() {
    if (this.buttons.length === 0) return;

    const node = document.querySelector<HTMLDivElement>(
      Config.leftTopMenuButtons.selector
    );
    if (!node) return;

    this.buttons.forEach((b) => {
      const button = this.createDivElement(b);
      node.insertBefore(button, null);
    });
  }
  
  createDivElement(data: Button): HTMLDivElement {
    const existingButton = document.querySelector(`div[id="${data.id}"]`);
    if (existingButton) return existingButton as HTMLDivElement;

    const div = document.createElement('div');
    div.innerHTML = data.html;
    div.setAttribute('id', data.id);

    div.addEventListener('click', (event: MouseEvent) => {
      handleClickAndNavigate(
        event,
        data.link,
        (url) => window.open(url, '_blank'),
        async (userId) => {
          await sendMessage('clickButton', {
            link: data.link,
            userId,
          } as ClickButton);
        }
      );
    });

    return div;
  }

  startObserver(): void {
    if (this.observer) return;

    this.observer = new MutationObserver(() => {
      if (this.isProcessing) return;

      this.isProcessing = true;

      this.ctx.setTimeout(() => {
        this.observer!.disconnect();

        try {
          this.addButtons();
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

const leftTopMenuButtons = new LeftTopMenuButtons();

export default leftTopMenuButtons;
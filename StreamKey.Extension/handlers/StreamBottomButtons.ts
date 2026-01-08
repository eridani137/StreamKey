import Config from '@/config';
import { sendMessage } from '@/messaging';
import { Nullable } from '@/types/common';
import { Button, ButtonPosition, ClickButton } from '@/types/messaging';
import { createButtonElement, handleClickAndNavigate } from '@/utils';

export class StreamBottomButtons {
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
    this.buttons = (await sendMessage('getButtons', ButtonPosition.StreamBottom)) || [];
    console.log('[StreamBottom] Fetch', this.buttons.length, 'items');
  }

  addButtons() {
    if (this.buttons.length === 0) return;

    const node = document.querySelector<HTMLDivElement>(
      Config.streamBottomButtonsMenu.spacingSelector
    );
    if (!node) return;

    const parent = node.parentElement;
    if (!parent) return;

    this.buttons.forEach((b) => {
      const button = createButtonElement(b);
      parent.insertBefore(button, node);
    });
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

const streamBottomButtons = new StreamBottomButtons();

export default streamBottomButtons;

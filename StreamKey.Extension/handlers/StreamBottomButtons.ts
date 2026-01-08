import Config from '@/config';
import { sendMessage } from '@/messaging';
import { Nullable } from '@/types/common';
import { Button, ButtonPosition, ClickButton } from '@/types/messaging';
import { handleClickAndNavigate } from '@/utils';

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

    const spacer = document.querySelector<HTMLDivElement>(
      Config.streamBottomButtonsMenu.spacingSelector
    );
    if (!spacer) return;

    const parent = spacer.parentElement;
    if (!parent) return;

    this.buttons.forEach((b) => {
      const button = this.createButtonElement(b);
      parent.insertBefore(button, spacer);
    });
  }

  createButtonElement(data: Button): HTMLButtonElement {
    const existingButton = document.querySelector(`button[id="${data.id}"]`);
    if (existingButton) return existingButton as HTMLButtonElement;

    this.buttonCounter++;
    const uniqueClass = `${Config.streamBottomButtonsMenu.uniqueButtonClassMask}${this.buttonCounter}`;

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
    button.setAttribute('id', data.id);

    button.addEventListener('click', (event: MouseEvent) => {
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

    return button;
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

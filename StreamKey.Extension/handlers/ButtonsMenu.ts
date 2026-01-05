import Config from '@/config';
import { sendMessage } from '@/messaging';
import { Nullable } from '@/types/common';
import { Button, ClickButton } from '@/types/messaging';
import { handleClickAndNavigate } from '@/utils';

export class ButtonsMenu {
  private ctx: any = null;
  private observer: Nullable<MutationObserver> = null;
  private isProcessing = false;
  private buttonCounter = 0;
  private buttons: Button[] = [];

  async init(ctx: any): Promise<void> {
    this.ctx = ctx;
    await this.fetchButtons();
    ctx.setInterval(async () => this.fetchButtons(), 180000);
    this.ctx.setInterval(() => {
      this.startObserver();
    }, 2000);
  }

  private async fetchButtons() {
    this.buttons = (await sendMessage('getButtons')) || [];
    console.log('[Buttons] Fetch', this.buttons.length, 'items');
  }

  addButtons() {
    if (this.buttons.length === 0) return;

    const spacer = document.querySelector<HTMLDivElement>(
      Config.buttonsMenu.spacingSelector
    );
    if (!spacer) return;

    const parent = spacer.parentElement;
    if (!parent) return;

    this.buttons.forEach((b) => {
      const button = this.createButtonElement(b);
      parent.insertBefore(button, spacer);
    });

    // let customContainer = parent.querySelector<HTMLDivElement>(`.${Config.buttonsMenu.buttonsContainerName}`);
    // if (!customContainer) {
    //   customContainer = document.createElement('div');
    //   customContainer.className = Config.buttonsMenu.buttonsContainerName;
    //   parent.insertBefore(customContainer, spacer);
    // }

    // this.buttons.forEach((b) => this.addButtonToContainer(b, customContainer));
  }

  // addButtonToContainer(data: Button, container: HTMLElement) {
  //   const existingButton = container.querySelector(`button[id="${data.id}"]`);
  //   if (existingButton) return;

  //   this.buttonCounter++;
  //   const uniqueClass = `${Config.buttonsMenu.uniqueButtonClassMask}${this.buttonCounter}`;

  //   const styleEl = document.createElement('style');
  //   styleEl.id = `style-${uniqueClass}`;
  //   styleEl.textContent = `
  //     .${uniqueClass} {
  //       ${data.style}
  //     }
  //     .${uniqueClass}:hover {
  //       ${data.hoverStyle || ''}
  //     }
  //     .${uniqueClass}:active {
  //       ${data.activeStyle || ''}
  //     }
  //   `;
  //   document.head.appendChild(styleEl);

  //   const button = document.createElement('button');
  //   button.className = uniqueClass;
  //   button.innerHTML = data.html;
  //   button.setAttribute('id', data.id);

  //   button.addEventListener('click', (event: MouseEvent) => {
  //     handleClickAndNavigate(
  //       event,
  //       data.link,
  //       (url) => window.open(url, '_blank'),
  //       async (userId) => {
  //         await sendMessage('clickButton', {
  //           link: data.link,
  //           userId,
  //         } as ClickButton);
  //       }
  //     );
  //   });

  //   container.appendChild(button);
  // }

  createButtonElement(data: Button): HTMLButtonElement {
    const existingButton = document.querySelector(`button[id="${data.id}"]`);
    if (existingButton) return existingButton as HTMLButtonElement;

    this.buttonCounter++;
    const uniqueClass = `${Config.buttonsMenu.uniqueButtonClassMask}${this.buttonCounter}`;

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

const buttonsMenu = new ButtonsMenu();

export default buttonsMenu;

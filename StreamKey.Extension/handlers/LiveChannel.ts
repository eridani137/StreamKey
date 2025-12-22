import { Nullable } from '@/types/common';

export class LiveChannel {
  private ctx: any = null;
  private observer: Nullable<MutationObserver> = null;
  private isProcessing = false;

  init(ctx: any): void {
    this.ctx = ctx;
    this.startObserver();
  }

  addButton() {
    const spacer = document.querySelector(
      '.metadata-layout__secondary-button-spacing'
    );

    if (
      !spacer ||
      spacer.nextElementSibling?.classList.contains('streamkey-channel-info-button')
    ) {
      return;
    }

    const button = document.createElement('button');
    button.className = 'streamkey-channel-info-button';
    button.addEventListener('click', () => {
        alert('Кнопка нажата!');
    });
    button.innerHTML = `
    <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor">
      <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"/>
    </svg>
    StreamKey
  `;

    spacer.parentNode?.insertBefore(button, spacer.nextSibling);
  }

  startObserver(): void {
    if (this.observer) return;

    this.observer = new MutationObserver(() => {
      if (this.isProcessing) return;

      this.isProcessing = true;

      this.ctx.setTimeout(() => {
        this.observer!.disconnect();

        try {
          this.addButton();
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

const liveChannel = new LiveChannel();

export default liveChannel;

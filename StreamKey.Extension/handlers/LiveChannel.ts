import { LiveChannelButton, Nullable } from '@/types/common';

export class LiveChannel {
  private ctx: any = null;
  private observer: Nullable<MutationObserver> = null;
  private isProcessing = false;
  private buttonCounter = 0;

  init(ctx: any): void {
    this.ctx = ctx;
    this.startObserver();
  }

  addButtons() {
    const buttons: LiveChannelButton[] = [
      {
        html: `
          <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor">
            <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"/>
          </svg>
          StreamKey
        `,
        style: `
          margin-left: 8px;
          padding: 6px 12px;
          border: none;
          border-radius: 16px;
          background: #9146ff;
          color: white;
          font-size: 14px;
          cursor: pointer;
          display: flex;
          align-items: center;
          gap: 6px;
          transition: background 0.2s;
        `,
        hoverStyle: `
          background: #7a3ed0;
        `,
        activeStyle: `
          background: #6a34b8;
        `,
        link: 'https://google.com',
      },
    ];
    buttons.forEach((b) => this.addButton(b));
  }

  addButton(data: LiveChannelButton) {
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
    button.addEventListener('click', () => {
      window.open(data.link, '_blank');
    });

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

const liveChannel = new LiveChannel();

export default liveChannel;

import { getTwitchUserId, handleClickAndNavigate, sleep } from '@/utils';
import { sendMessage } from '@/messaging';
import { ChannelData, ClickChannel } from '@/types/messaging';

export class ActiveChannels {
  private ctx: any = null;
  private channelData: ChannelData[] = [];
  private isDataReady: boolean = false;
  private tooltipObserver: MutationObserver | null = null;

  public init(ctx: any): void {
    this.ctx = ctx;
    this.setupTooltipHandler();
    this.fetchAndUpdateChannels();
    ctx.setInterval(() => this.fetchAndUpdateChannels(), 180000);
  }

  private setupTooltipHandler(): void {
    this.tooltipObserver = new MutationObserver(
      (mutations: MutationRecord[]) => {
        mutations.forEach((mutation: MutationRecord) => {
          mutation.addedNodes.forEach((node: Node) => {
            if (node.nodeType === 1) {
              const element = node as Element;
              if (
                element.matches('.tw-dialog-layer, .ReactModal__Overlay') ||
                element.querySelector('.online-side-nav-channel-tooltip__body')
              ) {
                this.updateTooltipContent(element);
              }
            }
          });
        });
      }
    );

    this.tooltipObserver.observe(document.body, {
      childList: true,
      subtree: true,
    });
  }

  private updateTooltipContent(tooltipNode: Element): void {
    const tooltipBody = tooltipNode.querySelector(
      '.online-side-nav-channel-tooltip__body p'
    );
    if (!tooltipBody || !this.channelData.length) return;

    const hoveredChannel = this.findHoveredChannel();
    if (hoveredChannel && hoveredChannel.info.description) {
      tooltipBody.textContent = hoveredChannel.info.description;
    }
  }

  private findHoveredChannel(): ChannelData | undefined {
    const channelLinks = document.querySelectorAll(
      '[data-test-selector="recommended-channel"]'
    );
    for (let i = 0; i < channelLinks.length; i++) {
      const link = channelLinks[i] as HTMLElement;
      if (link.matches(':hover')) {
        return this.channelData.find((channel) => channel.position === i);
      }
    }
    return undefined;
  }

  private async fetchAndUpdateChannels(): Promise<void> {
    const channels = await sendMessage('getChannels');

    if (channels) {
      console.log(`[Channels] Fetch OK — received ${channels.length} items`);

      this.channelData = channels;
      this.isDataReady = true;

      await this.waitForChannelsAndReplace();
    } else {
      console.log(`[Channels] Fetch failed`);
    }
  }

  private async waitForChannelsAndReplace(): Promise<void> {
    if (!this.isDataReady) return;

    const activeChannelsSection = await this.waitForElement(
      'div[aria-label="Активные каналы"]'
    );
    if (!activeChannelsSection) {
      console.log('Active channels section not found after waiting');
      return;
    }

    const channelCards = (await this.waitForElement(
      '[data-test-selector="recommended-channel"]',
      5000,
      true
    )) as NodeListOf<Element> | null;
    if (!channelCards || channelCards.length === 0) {
      console.log('Channel cards not found after waiting');
      return;
    }

    console.log(`Found ${channelCards.length} channel cards, checking...`);
    await this.updateChannels();
  }

  private waitForElement(
    selector: string,
    timeout?: number,
    multiple?: false
  ): Promise<Element | null>;
  private waitForElement(
    selector: string,
    timeout: number,
    multiple: true
  ): Promise<NodeListOf<Element> | null>;
  private waitForElement(
    selector: string,
    timeout: number = 10000,
    multiple: boolean = false
  ): Promise<Element | NodeListOf<Element> | null> {
    return new Promise((resolve) => {
      const startTime = Date.now();

      const checkElement = (): void => {
        const elements = multiple
          ? document.querySelectorAll(selector)
          : document.querySelector(selector);

        if (
          multiple ? (elements as NodeListOf<Element>).length > 0 : elements
        ) {
          resolve(elements);
        } else if (Date.now() - startTime < timeout) {
          this.ctx.setTimeout(checkElement, 300);
        } else {
          console.log(`Timeout waiting for element: ${selector}`);
          resolve(null);
        }
      };

      checkElement();
    });
  }

  private createChannelItem(
    item: ChannelData,
    cl: string,
    style: string
  ): HTMLDivElement {
    const nickname = item.channelName;
    const avatar = item.info.thumb;
    const usersCount = item.info.viewers;
    const category = item.info.category;

    const div = document.createElement('div');
    div.className = `${cl} streamkey-channel-item`;
    div.ariaLabel = 'false';
    div.style.cssText = style;

    div.addEventListener('click', (event) =>
      handleClickAndNavigate(
        event,
        `/${nickname}`,
        (url) => (window.location.href = url),
        async (userId) =>
          sendMessage('clickChannel', {
            channelName: item.channelName,
            userId,
          } as ClickChannel)
      )
    );

    div.innerHTML = `
        <div><div class="Layout-sc-1xcs6mc-0 AoXTY side-nav-card"><a data-a-id="recommended-channel-0" data-test-selector="recommended-channel" aria-haspopup="dialog" class="ScCoreLink-sc-16kq0mq-0 fytYW InjectLayout-sc-1i43xsx-0 cnzybN side-nav-card__link tw-link" href="/${nickname}"><div class="Layout-sc-1xcs6mc-0 kErOMx side-nav-card__avatar"><div class="ScAvatar-sc-144b42z-0 dLsNfm tw-avatar"><img class="InjectLayout-sc-1i43xsx-0 fAYJcN tw-image tw-image-avatar" alt="" src="${avatar}" style="object-fit: cover;"></div></div><div class="Layout-sc-1xcs6mc-0 bLlihH"><div class="Layout-sc-1xcs6mc-0 dJfBsr"><div data-a-target="side-nav-card-metadata" class="Layout-sc-1xcs6mc-0 ffUuNa"><div class="Layout-sc-1xcs6mc-0 kvrzxX side-nav-card__title"><p title="${nickname}" data-a-target="side-nav-title" class="CoreText-sc-1txzju1-0 dTdgXA InjectLayout-sc-1i43xsx-0 hnBAak">${nickname}</p></div><div class="Layout-sc-1xcs6mc-0 dWQoKW side-nav-card__metadata" data-a-target="side-nav-game-title"><p dir="auto" title="${category}" class="CoreText-sc-1txzju1-0 iMyVXK">${category}</p></div></div><div class="Layout-sc-1xcs6mc-0 cXMAQb side-nav-card__live-status" data-a-target="side-nav-live-status"><div class="Layout-sc-1xcs6mc-0 kvrzxX"><div class="ScChannelStatusIndicator-sc-bjn067-0 fJwlvq tw-channel-status-indicator"></div><p class="CoreText-sc-1txzju1-0 cWFBTs InjectLayout-sc-1i43xsx-0 cdydzE">В эфире</p><div class="Layout-sc-1xcs6mc-0 dqfEBK"><span aria-hidden="true" class="CoreText-sc-1txzju1-0 fYAAA-D">${usersCount}</span><p class="CoreText-sc-1txzju1-0 cWFBTs InjectLayout-sc-1i43xsx-0 cdydzE">${usersCount} зрителей</p></div></div></div></div></div><div class="Layout-sc-1xcs6mc-0 dJfBsr"><div class="Layout-sc-1xcs6mc-0 side-nav-card__link__tooltip-arrow"><div class="ScSvgWrapper-sc-wkgzod-0 dKXial tw-svg"><svg width="20" height="20" viewBox="0 0 20 20"><path d="M7.5 7.5 10 10l-2.5 2.5L9 14l4-4-4-4-1.5 1.5z"></path></svg></div><p class="CoreText-sc-1txzju1-0 cWFBTs InjectLayout-sc-1i43xsx-0 cdydzE">Используйте клавишу «Стрелка вправо», чтобы отобразить дополнительную информацию.</p></div></div></a></div></div>
        `;

    return div;
  }

  private async updateChannels(): Promise<void> {
    if (!this.channelData || this.channelData.length === 0) {
      return;
    }

    const activeChannelsSection = document.querySelector(
      'div[aria-label="Активные каналы"]'
    );
    if (!activeChannelsSection) {
      return;
    }

    const get_itemsPane = (): Element | null =>
      document.querySelector(
        'div[aria-label="Активные каналы"] div:nth-child(2)'
      );

    let itemsPane = get_itemsPane();
    while (!itemsPane) {
      await sleep(300);
      itemsPane = get_itemsPane();
    }

    document
      .querySelectorAll('.streamkey-channel-item')
      .forEach((item) => item.remove());

    let updated = false;

    const items = this.channelData
      .sort((a, b) => a.position - b.position)
      .reverse();

    for (const it of items) {
      const firstItem = itemsPane.firstChild as HTMLElement;
      const div = this.createChannelItem(
        it,
        firstItem.className,
        firstItem.style.cssText
      );

      firstItem.parentNode!.insertBefore(div, firstItem);

      updated = true;
    }

    if (updated) {
      console.log('Channels successfully updated');
    }
  }
}

const activeChannels = new ActiveChannels();

export default activeChannels;

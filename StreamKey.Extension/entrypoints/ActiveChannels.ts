import Config from '@/config';
import {ChannelData} from '@/types';
import {sleep} from '@/utils';

export class ActiveChannels {
    private ctx: any = null;
    private updateInterval: NodeJS.Timeout | null = null;
    private channelData: ChannelData[] = [];
    private isDataReady: boolean = false;
    private tooltipObserver: MutationObserver | null = null;
    private lastUpdateTime: number = 0;
    private readonly minUpdateInterval: number = 60000;
    private pendingUpdate: NodeJS.Timeout | null = null;

    public async init(ctx: any): Promise<void> {
        this.ctx = ctx;
        this.setupTooltipHandler();
        await this.fetchAndUpdateChannels();
        this.updateInterval = ctx.setInterval(() => this.fetchAndUpdateChannels(), 60000);
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
        const now = Date.now();
        const timeSinceLastFetch = now - this.lastUpdateTime;

        if (timeSinceLastFetch < 60000) {
            console.log(
                `[Channels] Fetch skipped (${timeSinceLastFetch}ms < 60000ms)`
            );
            return;
        }

        console.log(
            `[Channels] Performing fetch... (last was ${timeSinceLastFetch}ms ago)`
        );

        this.lastUpdateTime = now;

        const getChannels = await fetch(Config.urls.apiUrl + '/channels').catch(
            (err) => {
                console.error('[Channels] Fetch error:', err);
                return null;
            }
        );

        if (!getChannels || !getChannels.ok) {
            console.error(
                `[Channels] API request failed with status ${getChannels?.status}`
            );
            return;
        }

        let error: Error | null = null;
        const data = (await getChannels.json().catch((err: Error) => {
            error = err;
            return null;
        })) as ChannelData[] | null;

        if (error || !data) {
            return console.error('[Channels] Failed to parse JSON:', error);
        }

        console.log(`[Channels] Fetch OK — received ${data.length} items`);

        this.channelData = data;
        this.isDataReady = true;

        await this.waitForChannelsAndReplace();
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
        await this.scheduleUpdate();
    }

    private async scheduleUpdate(): Promise<void> {
        const now = Date.now();
        const timeSinceLastUpdate = now - this.lastUpdateTime;

        if (timeSinceLastUpdate < this.minUpdateInterval) {
            if (this.pendingUpdate) {
                clearTimeout(this.pendingUpdate);
            }

            const remainingTime = this.minUpdateInterval - timeSinceLastUpdate;
            this.pendingUpdate = this.ctx.setTimeout(async () => {
                await this.updateChannels();
                this.pendingUpdate = null;
            }, remainingTime);

            console.log(`Update scheduled in ${remainingTime}ms`);
            return;
        }

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

        div.addEventListener(
            'click',
            async function (event: MouseEvent): Promise<void> {
                if (event && typeof event.preventDefault === 'function') {
                    event.preventDefault();
                }
                event.stopPropagation && event.stopPropagation();

                try {
                    const sessionId = await browser.cookies.get({
                        url: Config.urls.streamKeyUrl,
                        name: Config.keys.sessionId
                    });
                    const userId = localStorage.getItem(Config.keys.twId);
                    if (sessionId && userId) {
                        const response = await fetch(
                            `${Config.urls.apiUrl}/channels/click`,
                            {
                                method: 'POST',
                                headers: {
                                    'Content-Type': 'application/json',
                                },
                                body: JSON.stringify({
                                    channelName: item.channelName,
                                    userId: userId,
                                }),
                            }
                        );

                        if (!response.ok) {
                            console.warn('Сервер вернул ошибку: ' + response.status);
                        }
                    }
                } finally {
                    window.location.href = `/${nickname}`;
                }
            }
        );

        div.innerHTML = `
        <div><div class="Layout-sc-1xcs6mc-0 AoXTY side-nav-card"><a data-a-id="recommended-channel-0" data-test-selector="recommended-channel" aria-haspopup="dialog" class="ScCoreLink-sc-16kq0mq-0 fytYW InjectLayout-sc-1i43xsx-0 cnzybN side-nav-card__link tw-link" href="/${nickname}"><div class="Layout-sc-1xcs6mc-0 kErOMx side-nav-card__avatar"><div class="ScAvatar-sc-144b42z-0 dLsNfm tw-avatar"><img class="InjectLayout-sc-1i43xsx-0 fAYJcN tw-image tw-image-avatar" alt="" src="${avatar}" style="object-fit: cover;"></div></div><div class="Layout-sc-1xcs6mc-0 bLlihH"><div class="Layout-sc-1xcs6mc-0 dJfBsr"><div data-a-target="side-nav-card-metadata" class="Layout-sc-1xcs6mc-0 ffUuNa"><div class="Layout-sc-1xcs6mc-0 kvrzxX side-nav-card__title"><p title="${nickname}" data-a-target="side-nav-title" class="CoreText-sc-1txzju1-0 dTdgXA InjectLayout-sc-1i43xsx-0 hnBAak">${nickname}</p></div><div class="Layout-sc-1xcs6mc-0 dWQoKW side-nav-card__metadata" data-a-target="side-nav-game-title"><p dir="auto" title="${category}" class="CoreText-sc-1txzju1-0 iMyVXK">${category}</p></div></div><div class="Layout-sc-1xcs6mc-0 cXMAQb side-nav-card__live-status" data-a-target="side-nav-live-status"><div class="Layout-sc-1xcs6mc-0 kvrzxX"><div class="ScChannelStatusIndicator-sc-bjn067-0 fJwlvq tw-channel-status-indicator"></div><p class="CoreText-sc-1txzju1-0 cWFBTs InjectLayout-sc-1i43xsx-0 cdydzE">В эфире</p><div class="Layout-sc-1xcs6mc-0 dqfEBK"><span aria-hidden="true" class="CoreText-sc-1txzju1-0 fYAAA-D">${usersCount}</span><p class="CoreText-sc-1txzju1-0 cWFBTs InjectLayout-sc-1i43xsx-0 cdydzE">${usersCount} зрителей</p></div></div></div></div></div><div class="Layout-sc-1xcs6mc-0 dJfBsr"><div class="Layout-sc-1xcs6mc-0 side-nav-card__link__tooltip-arrow"><div class="ScSvgWrapper-sc-wkgzod-0 dKXial tw-svg"><svg width="20" height="20" viewBox="0 0 20 20"><path d="M7.5 7.5 10 10l-2.5 2.5L9 14l4-4-4-4-1.5 1.5z"></path></svg></div><p class="CoreText-sc-1txzju1-0 cWFBTs InjectLayout-sc-1i43xsx-0 cdydzE">Используйте клавишу «Стрелка вправо», чтобы отобразить дополнительную информацию.</p></div></div></a></div></div>
        `;

        return div;
    }

    private async updateChannels(): Promise<void> {
        this.lastUpdateTime = Date.now();

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
            console.log('Channels updated successfully from API.');
        }
    }

    public destroy(): void {
        if (this.updateInterval) {
            this.ctx.clearInterval(this.updateInterval);
            this.updateInterval = null;
        }

        if (this.pendingUpdate) {
            this.ctx.clearTimeout(this.pendingUpdate);
            this.pendingUpdate = null;
        }

        if (this.tooltipObserver) {
            this.tooltipObserver.disconnect();
            this.tooltipObserver = null;
        }
    }
}

const activeChannels = new ActiveChannels();

export default activeChannels;

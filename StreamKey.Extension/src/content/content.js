const CONFIG = {
    styleName: "custom-radio",
    badgeName: "custom-radio-badge",
    quality_menu_selectors: {
        menuContainer: "div[data-a-target='player-settings-menu']",
        radioItems: "div[role='menuitemradio']",
        radioLabel: "label.ScRadioLabel-sc-1pxozg3-0"
    },
    badge: {
        text: "Stream Key",
        url: "https://t.me/streamkey"
    },
    minResolution: 1080,
    apiUrl: "https://service.streamkey.ru"
};

const api = typeof browser !== 'undefined' ? browser : chrome;

const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

const QualityMenuEnhancer = {
    observer: null,
    styleElement: null,

    init() {
        this.injectStyles();
        this.applyEnhancements();
        this.startObserver();
    },

    injectStyles() {
        if (this.styleElement) return;

        this.styleElement = document.createElement("style");
        this.styleElement.textContent = `
            .tw-radio {
                display: flex !important;
                flex-direction: column !important;
                align-items: flex-start !important;
                gap: 2px;
            }

            .${CONFIG.badgeName} {
                padding-left: 24px !important;
                color: white;
                font-weight: normal;
                pointer-events: none;
            }

            .${CONFIG.badgeName}-clickable {
                color: #ff3194;
                font-weight: 900;
                text-transform: uppercase;
                font-style: oblique;
                cursor: pointer;
                pointer-events: auto;
            }
        `;
        document.head.appendChild(this.styleElement);
    },

    getResolutionElements() {
        const radioItems = document.querySelectorAll(
            `${CONFIG.quality_menu_selectors.menuContainer} ${CONFIG.quality_menu_selectors.radioItems}`
        );

        return Array.from(radioItems)
            .map(radio => radio.querySelector(CONFIG.quality_menu_selectors.radioLabel))
            .filter(label => {
                if (!label) return false;
                const text = label.textContent || "";
                const match = text.match(/(\d{3,4})p/i);
                return match && parseInt(match[1], 10) >= CONFIG.minResolution;
            });
    },

    createBadge() {
        const badgeContainer = document.createElement("span");
        badgeContainer.classList.add(CONFIG.badgeName);

        const prefixText = document.createElement("span");
        prefixText.textContent = "с помощью ";

        const clickableText = document.createElement("span");
        clickableText.classList.add(`${CONFIG.badgeName}-clickable`);
        clickableText.textContent = CONFIG.badge.text;

        clickableText.addEventListener("click", (e) => {
            e.stopPropagation();
            window.open(CONFIG.badge.url, '_blank');
        });

        badgeContainer.appendChild(prefixText);
        badgeContainer.appendChild(clickableText);

        return badgeContainer;
    },

    handleLabelClick(selectedLabel) {
        document.querySelectorAll(`.${CONFIG.styleName}`).forEach(el => {
            el.classList.remove("selected");
        });
        selectedLabel.classList.add("selected");
    },

    enhanceLabel(label) {
        if (!label.classList.contains(CONFIG.styleName)) {
            label.classList.add(CONFIG.styleName);
        }

        const text = label.textContent || "";
        if (text.includes("1080") || text.includes("1440")) {
            const next = label.nextElementSibling;
            if (!(next && next.classList.contains(CONFIG.badgeName))) {
                const badge = this.createBadge();
                label.parentNode.insertBefore(badge, label.nextSibling);
            }
        }

        if (!label.dataset.listenerAttached) {
            label.addEventListener("click", () => {
                this.handleLabelClick(label);
            });
            label.dataset.listenerAttached = "true";
        }
    },

    applyEnhancements() {
        const labels = this.getResolutionElements();
        labels.forEach(label => this.enhanceLabel(label));
        return labels.length > 0;
    },

    startObserver() {
        if (this.observer) return;

        this.observer = new MutationObserver(() => this.applyEnhancements());

        const options = {
            childList: true,
            subtree: true
        }

        this.observer.observe(document.body, options);
    },

    destroy() {
        if (this.observer) {
            this.observer.disconnect();
            this.observer = null;
        }

        if (this.styleElement) {
            this.styleElement.remove();
            this.styleElement = null;
        }

        document.querySelectorAll(`.${CONFIG.styleName}`).forEach(el => {
            el.classList.remove(CONFIG.styleName, "selected");
            delete el.dataset.listenerAttached;
        });

        document.querySelectorAll(`.${CONFIG.badgeName}`).forEach(badge => {
            badge.remove();
        });
    }
};

const ActiveChannelsEnhancer = {
    updateInterval: null,
    channelData: [],
    isDataReady: false,
    tooltipObserver: null,
    lastUpdateTime: 0,
    minUpdateInterval: 5000,
    pendingUpdate: null,

    init() {
        this.setupTooltipHandler();
        this.fetchAndUpdateChannels();
        this.updateInterval = setInterval(() => this.fetchAndUpdateChannels(), 60000);
    },

    setupTooltipHandler() {
        this.tooltipObserver = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                mutation.addedNodes.forEach((node) => {
                    if (node.nodeType === 1 &&
                        (node.matches('.tw-dialog-layer, .ReactModal__Overlay') ||
                            node.querySelector('.online-side-nav-channel-tooltip__body'))) {
                        this.updateTooltipContent(node);
                    }
                });
            });
        });

        this.tooltipObserver.observe(document.body, {
            childList: true,
            subtree: true
        });
    },

    updateTooltipContent(tooltipNode) {
        const tooltipBody = tooltipNode.querySelector('.online-side-nav-channel-tooltip__body p');
        if (!tooltipBody || !this.channelData.length) return;

        const hoveredChannel = this.findHoveredChannel();
        if (hoveredChannel && hoveredChannel.info.description) {
            tooltipBody.textContent = hoveredChannel.info.description;
        }
    },

    findHoveredChannel() {
        const channelLinks = document.querySelectorAll('[data-test-selector="recommended-channel"]');
        for (let i = 0; i < channelLinks.length; i++) {
            const link = channelLinks[i];
            if (link.matches(':hover')) {
                return this.channelData.find(channel => channel.position === i);
            }
        }
        return null;
    },

    async fetchAndUpdateChannels() {
        const r = await fetch(CONFIG.apiUrl + "/channels")
            .catch(err => {
            });
        if (!r || !r.ok) {
            console.error(`API request failed with status ${r?.status}`);
            return;
        }

        let error = null;
        const data = await r.json()
            .catch(err => error = err);

        if (error) {
            return console.error("Failed to fetch or update channels:", error);
        }

        this.channelData = data;
        this.isDataReady = true;

        await this.waitForChannelsAndReplace();
    },

    async waitForChannelsAndReplace() {
        if (!this.isDataReady) return;

        const activeChannelsSection = await this.waitForElement('div[aria-label="Активные каналы"]');
        if (!activeChannelsSection) {
            console.log('Active channels section not found after waiting');
            return;
        }

        const channelCards = await this.waitForElement('[data-test-selector="recommended-channel"]', 5000, true);
        if (!channelCards || channelCards.length === 0) {
            console.log('Channel cards not found after waiting');
            return;
        }

        console.log(`Found ${channelCards.length} channel cards, checking...`);
        this.scheduleUpdate();
    },

    scheduleUpdate() {
        const now = Date.now();
        const timeSinceLastUpdate = now - this.lastUpdateTime;

        if (timeSinceLastUpdate < this.minUpdateInterval) {
            if (this.pendingUpdate) {
                clearTimeout(this.pendingUpdate);
            }

            const remainingTime = this.minUpdateInterval - timeSinceLastUpdate;
            this.pendingUpdate = setTimeout(() => {
                this.updateChannels();
                this.pendingUpdate = null;
            }, remainingTime);

            console.log(`Update scheduled in ${remainingTime}ms`);
            return;
        }

        this.updateChannels();
    },

    waitForElement(selector, timeout = 10000, multiple = false) {
        return new Promise((resolve) => {
            const startTime = Date.now();

            const checkElement = () => {
                const elements = multiple ?
                    document.querySelectorAll(selector) :
                    document.querySelector(selector);

                if (multiple ? elements.length > 0 : elements) {
                    resolve(elements);
                } else if (Date.now() - startTime < timeout) {
                    setTimeout(checkElement, 100);
                } else {
                    console.log(`Timeout waiting for element: ${selector}`);
                    resolve(null);
                }
            };

            checkElement();
        });
    },

    createChannelItem(item, cl, style) {
        const nickname = item.channelName;
        const avatar = item.info.thumb;
        const title = item.info.title;
        const usersCount = item.info.viewers;
        const category = item.info.category;

        const div = document.createElement('div');
        div.className = `${cl} streamkey-channel-item`;
        div.ariaLabel = 'false';
        div.style.cssText = style;

        div.addEventListener('click', function (event) {
            if (event && typeof event.preventDefault === 'function') {
                event.preventDefault();
            }
            event.stopPropagation && event.stopPropagation();

            api.storage.local.get(['sessionId'], (result) => {
                const userId = localStorage.getItem('local_copy_unique_id');
                if (result.sessionId && userId) {
                    fetch(`${CONFIG.apiUrl}/channels/click`, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                        },
                        body: JSON.stringify({
                            channelName: item.channelName,
                            userId: userId
                        })
                    })
                        .then(res => {
                            if (!res.ok) throw new Error('Сервер вернул ошибку: ' + res.status);
                            return res.text().then(text => text ? JSON.parse(text) : {});
                        })
                        .then(data => console.log(data))
                        .catch(err => console.error(err));
                }
            });
        });

        div.innerHTML = `
        <div><div class="Layout-sc-1xcs6mc-0 AoXTY side-nav-card"><a data-a-id="recommended-channel-0" data-test-selector="recommended-channel" aria-haspopup="dialog" class="ScCoreLink-sc-16kq0mq-0 fytYW InjectLayout-sc-1i43xsx-0 cnzybN side-nav-card__link tw-link" href="/${nickname}"><div class="Layout-sc-1xcs6mc-0 kErOMx side-nav-card__avatar"><div class="ScAvatar-sc-144b42z-0 dLsNfm tw-avatar"><img class="InjectLayout-sc-1i43xsx-0 fAYJcN tw-image tw-image-avatar" alt="" src="${avatar}" style="object-fit: cover;"></div></div><div class="Layout-sc-1xcs6mc-0 bLlihH"><div class="Layout-sc-1xcs6mc-0 dJfBsr"><div data-a-target="side-nav-card-metadata" class="Layout-sc-1xcs6mc-0 ffUuNa"><div class="Layout-sc-1xcs6mc-0 kvrzxX side-nav-card__title"><p title="${nickname}" data-a-target="side-nav-title" class="CoreText-sc-1txzju1-0 dTdgXA InjectLayout-sc-1i43xsx-0 hnBAak">${nickname}</p></div><div class="Layout-sc-1xcs6mc-0 dWQoKW side-nav-card__metadata" data-a-target="side-nav-game-title"><p dir="auto" title="${category}" class="CoreText-sc-1txzju1-0 iMyVXK">${category}</p></div></div><div class="Layout-sc-1xcs6mc-0 cXMAQb side-nav-card__live-status" data-a-target="side-nav-live-status"><div class="Layout-sc-1xcs6mc-0 kvrzxX"><div class="ScChannelStatusIndicator-sc-bjn067-0 fJwlvq tw-channel-status-indicator"></div><p class="CoreText-sc-1txzju1-0 cWFBTs InjectLayout-sc-1i43xsx-0 cdydzE">В эфире</p><div class="Layout-sc-1xcs6mc-0 dqfEBK"><span aria-hidden="true" class="CoreText-sc-1txzju1-0 fYAAA-D">${usersCount}</span><p class="CoreText-sc-1txzju1-0 cWFBTs InjectLayout-sc-1i43xsx-0 cdydzE">${usersCount} зрителей</p></div></div></div></div></div><div class="Layout-sc-1xcs6mc-0 dJfBsr"><div class="Layout-sc-1xcs6mc-0 side-nav-card__link__tooltip-arrow"><div class="ScSvgWrapper-sc-wkgzod-0 dKXial tw-svg"><svg width="20" height="20" viewBox="0 0 20 20"><path d="M7.5 7.5 10 10l-2.5 2.5L9 14l4-4-4-4-1.5 1.5z"></path></svg></div><p class="CoreText-sc-1txzju1-0 cWFBTs InjectLayout-sc-1i43xsx-0 cdydzE">Используйте клавишу «Стрелка вправо», чтобы отобразить дополнительную информацию.</p></div></div></a></div></div>
        `;

        return div;
    },

    async updateChannels() {
        this.lastUpdateTime = Date.now();

        if (!this.channelData || this.channelData.length === 0) {
            return;
        }

        const activeChannelsSection = document.querySelector('div[aria-label="Активные каналы"]');
        if (!activeChannelsSection) {
            return;
        }

        const get_itemsPane = () => document.querySelector('div[aria-label="Активные каналы"] div:nth-child(2)');

        let itemsPane = get_itemsPane();
        while (!itemsPane) {
            await sleep(500);
            itemsPane = get_itemsPane();
        }

        document.querySelectorAll('.streamkey-channel-item').forEach(item => item.remove());

        let updated = false;

        const items = this.channelData
            .sort((a, b) => a.position - b.position)
            .reverse();

        for (const it of items) {
            const firstItem = itemsPane.firstChild;
            const div = this.createChannelItem(it, firstItem.className, firstItem.style.cssText);

            firstItem.parentNode.insertBefore(div, firstItem);

            updated = true;
        }

        if (updated) {
            console.log("Channels updated successfully from API.");
        }
    },

    destroy() {
        if (this.updateInterval) {
            clearInterval(this.updateInterval);
            this.updateInterval = null;
        }

        if (this.pendingUpdate) {
            clearTimeout(this.pendingUpdate);
            this.pendingUpdate = null;
        }

        if (this.tooltipObserver) {
            this.tooltipObserver.disconnect();
            this.tooltipObserver = null;
        }
    }
};

updateActivity();
setInterval(() => {
    updateActivity();
}, 45000);

function updateActivity() {
    api.storage.local.get(['sessionId'], (result) => {
        const userId = localStorage.getItem('local_copy_unique_id');
        if (result.sessionId && userId) {
            fetch(`${CONFIG.apiUrl}/activity/update`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    sessionId: result.sessionId,
                    userId: userId
                })
            })
                .then(res => {
                    if (!res.ok) throw new Error('Сервер вернул ошибку: ' + res.status);
                    return res.text().then(text => text ? JSON.parse(text) : {});
                })
                .then(data => console.log(data))
                .catch(err => console.error(err));
        }
    });
}

QualityMenuEnhancer.init();
ActiveChannelsEnhancer.init();
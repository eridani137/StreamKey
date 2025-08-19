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
        if (text.includes("1080")) {
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

        this.observer = new MutationObserver(() => {
            this.applyEnhancements();
        });

        this.observer.observe(document.body, {
            childList: true,
            subtree: true
        });
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
    observer: null,
    updateInterval: null,
    channelData: [],

    init() {
        this.fetchAndUpdateChannels();

        const twoMinutes = 2 * 60 * 1000;
        this.updateInterval = setInterval(() => this.fetchAndUpdateChannels(), twoMinutes);

        this.startObserver();
    },

    async fetchAndUpdateChannels() {
        try {
            const response = await fetch(CONFIG.apiUrl);
            if (!response.ok) {
                throw new Error(`API request failed with status ${response.status}`);
            }
            this.channelData = await response.json();
            this.replaceChannels();
            console.log("Channels updated successfully from API.");
        } catch (error) {
            console.error("Failed to fetch or update channels:", error);
        }
    },

    createChannelElement(channel) {
        const card = document.createElement('div');
        card.className = 'Layout-sc-1xcs6mc-0 AoXTY side-nav-card';
        card.innerHTML = `
            <a data-a-id="recommended-channel-${channel.position}" data-test-selector="recommended-channel" class="ScCoreLink-sc-16kq0mq-0 fytYW InjectLayout-sc-1i43xsx-0 cnzybN side-nav-card__link tw-link" href="/${channel.channelName}">
                <div class="Layout-sc-1xcs6mc-0 kErOMx side-nav-card__avatar">
                    <div class="ScAvatar-sc-144b42z-0 dLsNfm tw-avatar">
                        <img class="InjectLayout-sc-1i43xsx-0 fAYJcN tw-image tw-image-avatar" alt="${channel.info.title}" src="${channel.info.thumb}">
                    </div>
                </div>
                <div class="Layout-sc-1xcs6mc-0 bLlihH">
                    <div class="Layout-sc-1xcs6mc-0 dJfBsr">
                        <div data-a-target="side-nav-card-metadata" class="Layout-sc-1xcs6mc-0 ffUuNa">
                            <div class="Layout-sc-1xcs6mc-0 kvrzxX side-nav-card__title">
                                <p title="${channel.info.title}" data-a-target="side-nav-title" class="CoreText-sc-1txzju1-0 dTdgXA InjectLayout-sc-1i43xsx-0 hnBAak">${channel.info.title}</p>
                            </div>
                            <div class="Layout-sc-1xcs6mc-0 dWQoKW side-nav-card__metadata" data-a-target="side-nav-game-title">
                                <p dir="auto" title="${channel.info.description || ''}" class="CoreText-sc-1txzju1-0 iMyVXK">${channel.info.description || ''}</p>
                            </div>
                        </div>
                        <div class="Layout-sc-1xcs6mc-0 cXMAQb side-nav-card__live-status" data-a-target="side-nav-live-status">
                            <div class="Layout-sc-1xcs6mc-0 kvrzxX">
                                <div class="ScChannelStatusIndicator-sc-bjn067-0 fJwlvq tw-channel-status-indicator"></div>
                                <div class="Layout-sc-1xcs6mc-0 dqfEBK">
                                    <span aria-hidden="true" class="CoreText-sc-1txzju1-0 fYAAA-D">${channel.info.viewers}</span>
                                    <p class="CoreText-sc-1txzju1-0 cWFBTs InjectLayout-sc-1i43xsx-0 cdydzE">${channel.info.viewers} зрителя</p>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </a>
        `;
        return card;
    },

    replaceChannels() {
        if (!this.channelData || this.channelData.length === 0) {
            return;
        }

        const activeChannelsSection = document.querySelector('div[aria-label="Активные каналы"]');
        if (!activeChannelsSection) {
            return;
        }

        const existingChannelElements = activeChannelsSection.querySelectorAll('.side-nav-card');

        this.channelData.forEach(channel => {
            if (existingChannelElements[channel.position]) {
                const newElement = this.createChannelElement(channel);
                existingChannelElements[channel.position].replaceWith(newElement);
            }
        });
    },

    startObserver() {
        if (this.observer) return;

        this.observer = new MutationObserver(() => {
            this.replaceChannels();
        })

        this.observer.observe(document.body, {
            childList: true,
            subtree: true
        });
    },

    destroy() {
        if (this.observer) {
            this.observer.disconnect();
            this.observer = null;
        }
        if (this.updateInterval) {
            clearInterval(this.updateInterval);
            this.updateInterval = null;
        }
    }
};

QualityMenuEnhancer.init();
ActiveChannelsEnhancer.init();
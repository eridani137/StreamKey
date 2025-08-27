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
    updateInterval: null,
    channelData: [],
    isDataReady: false,
    tooltipObserver: null,

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

    closeAllTooltips() {
        const modalOverlays = document.querySelectorAll('.ReactModal__Overlay, .tw-dialog-layer');
        modalOverlays.forEach(overlay => {
            overlay.style.display = 'none';
            overlay.remove();
        });

        document.querySelectorAll('.side-nav-card__link').forEach(link => {
            link.dispatchEvent(new Event('mouseleave'));
        });
    },

    async fetchAndUpdateChannels() {
        try {
            const response = await fetch(CONFIG.apiUrl + "/channels");
            if (!response.ok) {
                throw new Error(`API request failed with status ${response.status}`);
            }
            this.channelData = await response.json();
            this.isDataReady = true;
            await this.waitForChannelsAndReplace();
            console.log("Channels updated successfully from API.");
        } catch (error) {
            console.error("Failed to fetch or update channels:", error);
        }
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

        console.log(`Found ${channelCards.length} channel cards, replacing...`);
        this.replaceChannels();
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

    replaceChannels() {
        if (!this.channelData || this.channelData.length === 0) {
            return;
        }

        this.closeAllTooltips();

        const activeChannelsSection = document.querySelector('div[aria-label="Активные каналы"]');
        if (!activeChannelsSection) {
            return;
        }

        const channelCards = document.querySelectorAll('[data-test-selector="recommended-channel"]');

        this.channelData.forEach(channel => {
            const source = channelCards[channel.position];
            if (source) {
                const titleElement = source.querySelector('[data-a-target="side-nav-title"]');
                if (titleElement && channel.info.title) {
                    if (titleElement.textContent !== channel.info.title) {
                        titleElement.textContent = channel.info.title;
                        titleElement.setAttribute('title', channel.info.title);
                    }
                }

                const avatarElement = source.querySelector('.tw-image-avatar');
                if (avatarElement && channel.info.thumb) {
                    avatarElement.src = channel.info.thumb;
                }

                const viewerCountElement = source.querySelector('[data-a-target="side-nav-live-status"] span[aria-hidden="true"]');
                if (viewerCountElement && channel.info.viewers) {
                    viewerCountElement.textContent = channel.info.viewers;

                    const parentDiv = viewerCountElement.parentElement;
                    if (parentDiv) {
                        const viewerTextElement = parentDiv.querySelector('p');
                        if (viewerTextElement) {
                            viewerTextElement.textContent = `${channel.info.viewers} зрителей`;
                        }
                    }
                }

                const categoryElement = source.querySelector('[data-a-target="side-nav-game-title"] p');
                if (categoryElement && channel.info.category) {
                    categoryElement.textContent = channel.info.category;
                    categoryElement.setAttribute('title', channel.info.category);
                }

                if (channel.channelName) {
                    const linkElement = source.querySelector('.side-nav-card__link') || source;
                    linkElement.setAttribute('href', `/${channel.channelName}`);
                    linkElement.href = `/${channel.channelName}`;

                    linkElement.setAttribute('data-a-id', `recommended-channel-${channel.position}`);

                    this.forceUpdateReactComponent(linkElement);

                    this.reassignClickHandlers(linkElement);
                }
            }
        });
    },

    reassignClickHandlers(linkElement) {
        const newElement = linkElement.cloneNode(true);
        linkElement.parentNode.replaceChild(newElement, linkElement);

        newElement.addEventListener('click', function(e) {
            if (e.target.closest('.online-side-nav-channel-tooltip__body')) {
                return;
            }

            const currentHref = this.getAttribute('href') || this.href;
            if (currentHref && currentHref !== 'javascript:void(0)') {
                console.log('Transitioning to:', currentHref);
                return true;
            }

            e.preventDefault();
        }, true);
    },

    forceUpdateReactComponent(element) {
        try {
            const reactKey = Object.keys(element).find(key =>
                key.startsWith('__reactInternalInstance') ||
                key.startsWith('__reactFiber')
            );

            if (reactKey) {
                const fiberNode = element[reactKey];
                if (fiberNode && fiberNode.stateNode && fiberNode.stateNode.forceUpdate) {
                    fiberNode.stateNode.forceUpdate();
                }
            }
        } catch (error) {
            console.log('Could not force update React component:', error);
        }
    },

    destroy() {
        if (this.updateInterval) {
            clearInterval(this.updateInterval);
            this.updateInterval = null;
        }
    }
};

QualityMenuEnhancer.init();
ActiveChannelsEnhancer.init();
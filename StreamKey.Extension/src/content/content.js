const CONFIG = {
    styleName: "custom-radio",
    badgeName: "custom-radio-badge",
    selectors: {
        menuContainer: "div[data-a-target='player-settings-menu']",
        radioItems: "div[role='menuitemradio']",
        radioLabel: "label.ScRadioLabel-sc-1pxozg3-0"
    },
    badge: {
        text: "Stream Key",
        url: "https://t.me/streamkey"
    },
    minResolution: 1080
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
                color: #ff3194;
                font-weight: 900;
                text-transform: uppercase;
                font-style: oblique;
                cursor: pointer;
            }

            .${CONFIG.badgeName}::before {
                content: 'с помощью ';
                color: white;
                font-weight: normal;
                text-transform: none;
            }
        `;
        document.head.appendChild(this.styleElement);
    },

    getResolutionElements() {
        const radioItems = document.querySelectorAll(
            `${CONFIG.selectors.menuContainer} ${CONFIG.selectors.radioItems}`
        );

        return Array.from(radioItems)
            .map(radio => radio.querySelector(CONFIG.selectors.radioLabel))
            .filter(label => {
                if (!label) return false;
                const text = label.textContent || "";
                const match = text.match(/(\d{3,4})p/i);
                return match && parseInt(match[1], 10) >= CONFIG.minResolution;
            });
    },

    createBadge() {
        const badge = document.createElement("span");
        badge.classList.add(CONFIG.badgeName);
        badge.textContent = CONFIG.badge.text;

        badge.addEventListener("click", () => {
            window.open(CONFIG.badge.url, '_blank');
        });

        return badge;
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

QualityMenuEnhancer.init();
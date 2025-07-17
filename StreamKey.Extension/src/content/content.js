const styleName = "custom-radio";
const badgeName = `${styleName}-badge`;

const style = document.createElement("style");
style.textContent = `
  .tw-radio {
    display: flex !important;
    flex-direction: column !important;
    align-items: flex-start !important;
    gap: 2px;
  }

  .${badgeName} {
    padding-left: 24px !important;
    color: #ff3194;
    font-weight: 900;
    text-transform: uppercase;
    font-style: oblique;
    cursor: pointer;
  }

  .${badgeName}::before {
    content: 'с помощью ';
    color: white;
    font-weight: normal;
    text-transform: none;
  }
`;
document.head.appendChild(style);

function getResolutionElements() {
    return Array.from(
        document.querySelectorAll(
            "div[data-a-target='player-settings-menu'] div[role='menuitemradio']"
        )
    ).map(radio => radio.querySelector("label.ScRadioLabel-sc-1pxozg3-0"))
        .filter(label => {
            if (!label) return false;
            const txt = label.textContent || "";
            const match = txt.match(/(\d{3,4})p/i);
            return match && parseInt(match[1], 10) >= 1080;
        });
}

function applyStyle() {
    const labels = getResolutionElements();

    labels.forEach(label => {
        if (!label.classList.contains(styleName)) {
            label.classList.add(styleName);
        }

        const next = label.nextElementSibling;
        if (label.textContent.includes("1080") && !(next && next.classList.contains(badgeName))) {
            const badge = document.createElement("span");
            badge.classList.add(badgeName);
            badge.textContent = "Stream Key";
            badge.addEventListener("click", function () {
                const urlToOpen = "https://t.me/streamkey";
                window.open(urlToOpen, '_blank');
            });
            label.parentNode.insertBefore(badge, label.nextSibling);
        }

        if (!label.dataset.listenerAttached) {
            label.addEventListener("click", () => {
                document.querySelectorAll(`.${styleName}`).forEach(el => {
                    el.classList.remove("selected");
                });
                label.classList.add("selected");
            });
            label.dataset.listenerAttached = "true";
        }
    });

    return labels.length > 0;
}

applyStyle();

const observer = new MutationObserver(() => {
    applyStyle();
});

observer.observe(document.body, {
    childList: true,
    subtree: true
});
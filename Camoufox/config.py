import typing

from browserforge.fingerprints import Screen

WAIT_UNTIL: typing.Optional[typing.Literal["domcontentloaded", "load", "networkidle"]] = "networkidle"

MAX_CONCURRENT_TABS = 8
AUTO_RESTART_MINUTES = 5
RESTART_DELAY_SECONDS = 5

ACTIVITY_TIMEOUT_MINUTES = 1
TIMEOUT_CHECK_INTERVAL_SECONDS = 30

CLOUDFLARE_FIND_SECONDS = 25
CLOUDFLARE_ATTEMPTS = 3
CLOUDFLARE_TIMEOUTS = 25000

MAX_RETRIES_PAGE = 5
RETRY_DELAY_MIN = 1
RETRY_DELAY_MAX = 5

BROWSER_OPTIONS = {
    "headless": "virtual",
    "os": ["windows", "macos", "linux"],
    "humanize": True,
    "enable_cache": False,
    "locale": "en-US",
    "geoip": True,
    "disable_coop": True,
    "i_know_what_im_doing": True
}

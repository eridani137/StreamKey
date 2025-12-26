import { type Browser } from 'wxt/browser';
import Config from './config';
type Rule = Browser.declarativeNetRequest.Rule;

let currentAuthToken: string | null = null;

const baseTwitchRules = [
  {
    "id": 1,
    "priority": 1,
    "action": {
      "type": "redirect",
      "redirect": {
        "regexSubstitution": "https://service.streamkey.ru/playlist/?auth=\\0&\\1"
      }
    },
    "condition": {
      "regexFilter": "^https://usher\\.ttvnw\\.net/api/channel/hls/[^?]+\\.m3u8\\?(.*)",
      "resourceTypes": ["xmlhttprequest", "media"]
    }
  },
  {
    "id": 2,
    "priority": 1,
    "action": {
      "type": "redirect",
      "redirect": {
        "regexSubstitution": "https://service.streamkey.ru/playlist/?auth=\\0&\\1"
      }
    },
    "condition": {
      "regexFilter": "^https://usher\\.ttvnw\\.net/api/v2/channel/hls/[^?]+\\.m3u8\\?(.*)",
      "resourceTypes": ["xmlhttprequest", "media"]
    }
  },
  {
    "id": 3,
    "priority": 1,
    "action": {
      "type": "redirect",
      "redirect": {
        "regexSubstitution": "https://service.streamkey.ru/playlist/vod/?vod_id=\\1&auth=\\0&\\2"
      }
    },
    "condition": {
      "regexFilter": "^https://usher\\.ttvnw\\.net/vod/([^/]+)\\.m3u8\\?(.*)",
      "resourceTypes": ["xmlhttprequest", "media"]
    }
  },
  {
    "id": 4,
    "priority": 1,
    "action": {
      "type": "redirect",
      "redirect": {
        "regexSubstitution": "https://service.streamkey.ru/playlist/vod/?vod_id=\\1&auth=\\0&\\2"
      }
    },
    "condition": {
      "regexFilter": "^https://usher\\.ttvnw\\.net/vod/v2/([^/]+)\\.m3u8\\?(.*)",
      "resourceTypes": ["xmlhttprequest", "media"]
    }
  }
];

function escapeRegexSubstitution(str: string): string {
  return str.replace(/[\\+*?.(){}|[\]^$]/g, '\\$&');
}

async function updateRulesWithAuth(): Promise<void> {
  const authValue = currentAuthToken || '';
  const escapedAuth = escapeRegexSubstitution(authValue);
  
  const rulesWithAuth: Rule[] = baseTwitchRules.map(rule => ({
    ...rule,
    action: {
      ...rule.action,
      redirect: {
        ...rule.action.redirect,
        regexSubstitution: rule.action.redirect.regexSubstitution.replace(
          '\\0', 
          escapedAuth
        )
      }
    }
  })) as Rule[];

  const currentRules = await browser.declarativeNetRequest.getDynamicRules();
  const ruleIds = currentRules.map(rule => rule.id);
  
  await browser.declarativeNetRequest.updateDynamicRules({
    removeRuleIds: ruleIds,
    addRules: rulesWithAuth
  });
  
  console.log(`Правила обновлены с auth: ${currentAuthToken ? 'установлен' : 'пустой'}`);
}

export async function enableDynamicRules(): Promise<void> {
  try {
    if (browser.declarativeNetRequest) {

      const twitchAuth = (
        await browser.cookies.get({
          url: Config.urls.twitchUrl,
          name: 'auth-token',
        })
      )?.value;

      currentAuthToken = twitchAuth || null;
      console.log('auth:', currentAuthToken);
      
      await updateRulesWithAuth();
      console.log('Динамические правила Twitch активированы');
    } else {
      console.warn('declarativeNetRequest не поддерживается');
    }
  } catch (err) {
    console.error('Ошибка активации динамических правил:', err);
  }
}

export async function disableDynamicRules(): Promise<void> {
  try {
    if (browser.declarativeNetRequest) {
      const currentRules = await browser.declarativeNetRequest.getDynamicRules();
      const ruleIds = currentRules.map(rule => rule.id);
      
      await browser.declarativeNetRequest.updateDynamicRules({
        removeRuleIds: ruleIds
      });
      
      currentAuthToken = null;
      console.log('Динамические правила Twitch деактивированы');
    }
  } catch (err) {
    console.error('Ошибка деактивации динамических правил:', err);
  }
}
import rules from '@/public/rules.json';
import { type Browser } from 'wxt/browser';

type Rule = Browser.declarativeNetRequest.Rule;

export async function enableRuleset(): Promise<void> {
  try {
    if (
      browser.declarativeNetRequest &&
      browser.declarativeNetRequest.updateEnabledRulesets
    ) {
      await browser.declarativeNetRequest.updateEnabledRulesets({
        enableRulesetIds: ['ruleset_1'],
        disableRulesetIds: [],
      });
      console.log('Правила перенаправления активированы');
    } else {
      console.warn('declarativeNetRequest не поддерживается в этом браузере');
    }
  } catch (err) {
    console.error('Ошибка активации правил:', err);
  }
}

export async function disableRuleset(): Promise<void> {
  try {
    if (
      browser.declarativeNetRequest &&
      browser.declarativeNetRequest.updateEnabledRulesets
    ) {
      await browser.declarativeNetRequest.updateEnabledRulesets({
        enableRulesetIds: [],
        disableRulesetIds: ['ruleset_1'],
      });
      console.log('Правила перенаправления деактивированы');
    } else {
      console.warn('declarativeNetRequest не поддерживается в этом браузере');
    }
  } catch (err) {
    console.error('Ошибка деактивации правил:', err);
  }
}

export async function enableAllRules(): Promise<void> {
  const existing = await browser.declarativeNetRequest.getDynamicRules();

  if (existing.length > 0) {
    await browser.declarativeNetRequest.updateDynamicRules({
      removeRuleIds: existing.map((r) => r.id),
    });
  }

  await browser.declarativeNetRequest.updateDynamicRules({
    addRules: rules as Rule[],
  });
}

export async function disableAllRules(): Promise<void> {
  const existing = await browser.declarativeNetRequest.getDynamicRules();

  if (existing.length === 0) return;

  await browser.declarativeNetRequest.updateDynamicRules({
    removeRuleIds: existing.map((r) => r.id),
  });
}

// export async function getDynamicRules(): Promise<Rule[]> {
//   try {
//     const rules = await browser.declarativeNetRequest.getDynamicRules();
//     return rules;
//   } catch (err) {
//     console.error('Ошибка получения динамических правил:', err);
//     return [];
//   }
// }

// export async function removeDynamicRules(ruleIds: number[]): Promise<void> {
//   try {
//     await browser.declarativeNetRequest.updateDynamicRules({
//       addRules: [],
//       removeRuleIds: ruleIds,
//     });
//     console.log(`Удалено правил: ${ruleIds.length}`);
//   } catch (err) {
//     console.error('Ошибка удаления динамических правил:', err);
//     throw err;
//   }
// }

// export async function removeAllDynamicRules(): Promise<void> {
//   try {
//     const currentRules = await getDynamicRules();
//     const ruleIds = currentRules.map((rule) => rule.id);

//     if (ruleIds.length > 0) {
//       await removeDynamicRules(ruleIds);
//       console.log('Все динамические правила удалены');
//     } else {
//       console.log('Нет динамических правил для удаления');
//     }
//   } catch (err) {
//     console.error('Ошибка удаления всех правил:', err);
//     throw err;
//   }
// }

// export async function updateDynamicRules(newRules: Rule[]): Promise<void> {
//   try {
//     const currentRules = await getDynamicRules();
//     const ruleIdsToRemove = currentRules.map((rule) => rule.id);

//     await browser.declarativeNetRequest.updateDynamicRules({
//       removeRuleIds: ruleIdsToRemove,
//       addRules: newRules,
//     });

//     const rules = await getDynamicRules();
//     console.log('Динамические правила обновлены', rules);
//   } catch (err) {
//     console.error('Ошибка обновления динамических правил:', err);
//     throw err;
//   }
// }

// export async function loadTwitchRedirectDynamicRules(): Promise<void> {
//   const auth = (await browser.cookies.get({url: Config.urls.twitchUrl, name: 'auth-token'}))?.value || '';

//   console.log('auth', auth);

//   const rules: Rule[] = [
//     {
//       id: 1,
//       priority: 1,
//       action: {
//         type: 'redirect',
//         redirect: {
//           regexSubstitution: `https://service.streamkey.ru/playlist/?auth=${auth}&\\1`,
//         },
//       },
//       condition: {
//         regexFilter:
//           '^https://usher\\.ttvnw\\.net/api/channel/hls/[^?]+\\.m3u8\\?(.*)',
//         resourceTypes: ['xmlhttprequest', 'media'],
//       },
//     },
//     {
//       id: 2,
//       priority: 1,
//       action: {
//         type: 'redirect',
//         redirect: {
//           regexSubstitution: `https://service.streamkey.ru/playlist/?auth=${auth}&\\1`,
//         },
//       },
//       condition: {
//         regexFilter:
//           '^https://usher\\.ttvnw\\.net/api/v2/channel/hls/[^?]+\\.m3u8\\?(.*)',
//         resourceTypes: ['xmlhttprequest', 'media'],
//       },
//     },
//     {
//       id: 3,
//       priority: 1,
//       action: {
//         type: 'redirect',
//         redirect: {
//           regexSubstitution:
//             `https://service.streamkey.ru/playlist/vod/?vod_id=\\1&auth=${auth}&\\2`,
//         },
//       },
//       condition: {
//         regexFilter: '^https://usher\\.ttvnw\\.net/vod/([^/]+)\\.m3u8\\?(.*)',
//         resourceTypes: ['xmlhttprequest', 'media'],
//       },
//     },
//     {
//       id: 4,
//       priority: 1,
//       action: {
//         type: 'redirect',
//         redirect: {
//           regexSubstitution:
//             `https://service.streamkey.ru/playlist/vod/?vod_id=\\1&auth=${auth}&\\2`,
//         },
//       },
//       condition: {
//         regexFilter: '^https://usher\\.ttvnw\\.net/vod/v2/([^/]+)\\.m3u8\\?(.*)',
//         resourceTypes: ['xmlhttprequest', 'media'],
//       },
//     },
//   ];

//   await updateDynamicRules(rules);
// }

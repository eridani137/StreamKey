type Rule = Awaited<
  ReturnType<typeof browser.declarativeNetRequest.getDynamicRules>
>[number];

export async function getDynamicRules(): Promise<Rule[]> {
  try {
    const rules = await browser.declarativeNetRequest.getDynamicRules();
    console.log('Текущие динамические правила:', rules);
    return rules;
  } catch (err) {
    console.error('Ошибка получения динамических правил:', err);
    return [];
  }
}

export async function addDynamicRules(rules: Rule[]): Promise<void> {
  try {
    await browser.declarativeNetRequest.updateDynamicRules({
      addRules: rules,
      removeRuleIds: [],
    });
    console.log(`Добавлено ${rules.length} динамических правил`);
  } catch (err) {
    console.error('Ошибка добавления динамических правил:', err);
    throw err;
  }
}

export async function removeDynamicRules(ruleIds: number[]): Promise<void> {
  try {
    await browser.declarativeNetRequest.updateDynamicRules({
      addRules: [],
      removeRuleIds: ruleIds,
    });
    console.log(`Удалено правил: ${ruleIds.length}`);
  } catch (err) {
    console.error('Ошибка удаления динамических правил:', err);
    throw err;
  }
}

export async function removeAllDynamicRules(): Promise<void> {
  try {
    const currentRules = await getDynamicRules();
    const ruleIds = currentRules.map((rule) => rule.id);

    if (ruleIds.length > 0) {
      await removeDynamicRules(ruleIds);
      console.log('Все динамические правила удалены');
    } else {
      console.log('Нет динамических правил для удаления');
    }
  } catch (err) {
    console.error('Ошибка удаления всех правил:', err);
    throw err;
  }
}

export async function updateDynamicRules(newRules: Rule[]): Promise<void> {
  try {
    const currentRules = await getDynamicRules();
    const ruleIdsToRemove = currentRules.map((rule) => rule.id);

    await browser.declarativeNetRequest.updateDynamicRules({
      removeRuleIds: ruleIdsToRemove,
      addRules: newRules,
    });
    console.log('Динамические правила обновлены');
  } catch (err) {
    console.error('Ошибка обновления динамических правил:', err);
    throw err;
  }
}

export async function isRuleActive(ruleId: number): Promise<boolean> {
  try {
    const rules = await getDynamicRules();
    return rules.some((rule) => rule.id === ruleId);
  } catch (err) {
    console.error('Ошибка проверки правила:', err);
    return false;
  }
}

export async function getRuleById(ruleId: number): Promise<Rule | null> {
  try {
    const rules = await getDynamicRules();
    return rules.find((rule) => rule.id === ruleId) || null;
  } catch (err) {
    console.error('Ошибка получения правила:', err);
    return null;
  }
}

export async function toggleDynamicRule(
  ruleId: number,
  enabled: boolean
): Promise<void> {
  try {
    const rule = await getRuleById(ruleId);
    if (!rule) {
      console.warn(`Правило с ID ${ruleId} не найдено`);
      return;
    }

    if (enabled) {
      await addDynamicRules([rule]);
    } else {
      await removeDynamicRules([ruleId]);
    }

    console.log(`Правило ${ruleId} ${enabled ? 'включено' : 'выключено'}`);
  } catch (err) {
    console.error('Ошибка переключения правила:', err);
    throw err;
  }
}

export async function loadTwitchRedirectRules(): Promise<void> {
  const rules: Rule[] = [
    {
      id: 1,
      priority: 1,
      action: {
        type: 'redirect',
        redirect: {
          regexSubstitution: 'https://service.streamkey.ru/playlist/?\\1',
        },
      },
      condition: {
        regexFilter:
          '^https://usher\\.ttvnw\\.net/api/channel/hls/[^?]+\\.m3u8\\?(.*)',
        resourceTypes: ['xmlhttprequest', 'media'],
      },
    },
    {
      id: 2,
      priority: 1,
      action: {
        type: 'redirect',
        redirect: {
          regexSubstitution: 'https://service.streamkey.ru/playlist/?\\1',
        },
      },
      condition: {
        regexFilter:
          '^https://usher\\.ttvnw\\.net/api/v2/channel/hls/[^?]+\\.m3u8\\?(.*)',
        resourceTypes: ['xmlhttprequest', 'media'],
      },
    },
    {
      id: 3,
      priority: 1,
      action: {
        type: 'redirect',
        redirect: {
          regexSubstitution:
            'https://service.streamkey.ru/playlist/vod/?vod_id=\\1&\\2',
        },
      },
      condition: {
        regexFilter: '^https://usher\\.ttvnw\\.net/vod/([^/]+)\\.m3u8\\?(.*)',
        resourceTypes: ['xmlhttprequest', 'media'],
      },
    },
  ];

  await updateDynamicRules(rules);
}

export function isDeclarativeNetRequestSupported(): boolean {
  return !!(
    browser.declarativeNetRequest &&
    browser.declarativeNetRequest.updateDynamicRules
  );
}

import { defineConfig } from 'wxt';

// See https://wxt.dev/api/config.html
export default defineConfig({
  modules: ['@wxt-dev/module-vue'],
  manifest: (env) => {
    const manifest = {
      name: 'Твич 1080 | Твич качество 1080 от STREAM KEY',
      version: '1.0.14',
      description:
        'STREAM KEY - твой ключ от мира стриминга. Расширение для твича 1080 возвращает качество 1080p на Twitch в России. Твич 1080 качество в России.',
      permissions: [
        'cookies',
        'storage',
        'declarativeNetRequest',
        'declarativeNetRequestWithHostAccess',
      ],
      host_permissions: [
        'https://*.twitch.tv/*',
        'https://*.ttvnw.net/*',
        'https://*.streamkey.ru/*',
      ],
      web_accessible_resources: [
        {
          matches: ['https://*.twitch.tv/*'],
          resources: ['fonts/*.ttf', 'fonts/*.otf'],
        },
      ],
      declarative_net_request: {
        rule_resources: [
          {
            id: 'ruleset_1',
            enabled: true,
            path: 'rules.json',
          },
        ],
      },
      icons: {
        '16': 'icons/16.png',
        '32': 'icons/32.png',
        '48': 'icons/48.png',
        '128': 'icons/128.png',
      },
    };
    if (env.browser === 'firefox') {
      (manifest as any).browser_specific_settings = {
        gecko: {
          id: 'info@streamkey.ru',
          strict_min_version: '113.0',
        },
      };
    }
    return manifest;
  }
});

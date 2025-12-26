import { defineConfig } from 'wxt';

// See https://wxt.dev/api/config.html
export default defineConfig({
  modules: ['@wxt-dev/module-vue'],
  vite: () => ({
    plugins: [],
    esbuild:
      process.env.NODE_ENV === 'production'
        ? {
            drop: ['console', 'debugger'],
          }
        : undefined,
  }), // TODO
  manifest: (env) => {
    const manifest = {
      name: 'Твич 1080 и 1440 | Вернуть 1080/1440 на Твич от STREAM KEY (не ВПН)',
      permissions: [
        'alarms',
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
  },
});

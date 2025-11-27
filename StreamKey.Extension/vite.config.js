import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'
import JavaScriptObfuscator from 'javascript-obfuscator'

const getTargetBrowser = () => {
  const targetArg = process.argv.find(arg => arg.startsWith('--target='))
  if (targetArg) {
    return targetArg.split('=')[1]
  }
  return 'chrome'
}

const targetBrowser = getTargetBrowser()
console.log(`Целевой браузер для сборки: ${targetBrowser}`)

export default defineConfig(({ mode }) => {
  const isExtension = mode === 'extension' || mode === 'extension-dev'
  const enableObfuscation = mode === 'extension'

  const terserCompressOptions = {
    drop_console: false,
    drop_debugger: false,
    pure_funcs: ['console.log', 'console.info', 'console.debug', 'console.warn', 'console.error'],
  }

  const safeObfuscationOptions = {
    compact: true,
    controlFlowFlattening: false,
    deadCodeInjection: false,
    debugProtection: false,
    selfDefending: false,
    stringArray: true,
    stringArrayEncoding: ['base64'],
    splitStrings: true,
    splitStringsChunkLength: 10,
  }

  const obfuscationOptions = {
    compact: true,
    controlFlowFlattening: true,
    controlFlowFlatteningThreshold: 0.75,
    deadCodeInjection: true,
    deadCodeInjectionThreshold: 0.4,
    debugProtection: true,
    debugProtectionInterval: 1000,
    disableConsoleOutput: false,
    identifierNamesGenerator: 'hexadecimal',
    log: false,
    numbersToExpressions: true,
    renameGlobals: false,
    selfDefending: true,
    simplify: true,
    splitStrings: true,
    splitStringsChunkLength: 10,
    stringArray: true,
    stringArrayCallsTransform: true,
    stringArrayEncoding: ['base64'],
    stringArrayIndexShift: true,
    stringArrayRotate: true,
    stringArrayShuffle: true,
    stringArrayWrappersCount: 2,
    stringArrayWrappersChainedCalls: true,
    stringArrayWrappersParametersMaxCount: 4,
    stringArrayWrappersType: 'function',
    stringArrayThreshold: 0.75,
    transformObjectKeys: true,
    unicodeEscapeSequence: false
  }

  return {
    root: resolve(__dirname, 'src'),

    plugins: [
      vue(),
      enableObfuscation && {
        name: 'javascript-obfuscator',
        apply: 'build',
        enforce: 'post',
        generateBundle(options, bundle) {
          Object.keys(bundle).forEach(fileName => {
            const asset = bundle[fileName]
            if (fileName.endsWith('.js') && asset.type === 'chunk') {
              console.log(`Обфускация файла: ${fileName}`)
              const isBackground = fileName.includes('background')
              const isContent = fileName.includes('content')
              const result = JavaScriptObfuscator.obfuscate(
                asset.code,
                (isBackground || isContent)
                  ? safeObfuscationOptions
                  : obfuscationOptions
              )
              asset.code = result.getObfuscatedCode()
            }
          })
        }
      }
    ].filter(Boolean),

    build: {
      outDir: resolve(__dirname, 'dist', targetBrowser),
      emptyOutDir: true,

      rollupOptions: isExtension ? {
        input: {
          popup: resolve(__dirname, 'src/popup/popup.html'),
          background: resolve(__dirname, 'src/background.js'),
          content: resolve(__dirname, 'src/content.js'),
          config: resolve(__dirname, 'src/config.js'),
          utils: resolve(__dirname, 'src/utils.js'),
        },
        output: {
          entryFileNames: (chunkInfo) => {
            if (chunkInfo.name === 'popup') {
              return 'popup/[name].js';
            }
            return '[name].js';
          },
          chunkFileNames: 'chunks/[name].[hash].js',
          assetFileNames: (assetInfo) => {
            if (assetInfo.fileName && assetInfo.fileName.includes('popup') && assetInfo.fileName.endsWith('.css')) {
              return 'popup/popup.css';
            }
            return 'assets/[name].[ext]';
          }
        }
      } : {
        input: {
          main: resolve(__dirname, 'index.html'),
        }
      },
      minify: 'terser',
      terserOptions: {
        compress: terserCompressOptions,
        mangle: {
          toplevel: true,
          safari10: true,
          properties: {
            regex: /^_/
          }
        },
        format: {
          comments: false
        }
      }
    },
    define: {
      'process.env': {}
    }
  }
})
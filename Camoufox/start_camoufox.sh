#!/bin/bash
set -e

export DISPLAY=:99

# Если Xvfb отсутствует, выводим понятное сообщение и завершаемся
command -v Xvfb >/dev/null 2>&1 || { echo >&2 "❌ Xvfb не найден. Проверьте Dockerfile"; exit 1; }

Xvfb :99 -screen 0 1920x1080x16 -ac -nolisten tcp &
sleep 2                             # ждём, пока дисплей поднимется

exec python3 /app/camoufox_server.py

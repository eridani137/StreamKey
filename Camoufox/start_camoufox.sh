#!/bin/bash
set -e

export DISPLAY=:99

# Проверка Xvfb
command -v Xvfb >/dev/null 2>&1 || { echo >&2 "❌ Xvfb не найден. Проверьте Dockerfile"; exit 1; }

# Запуск виртуального дисплея
Xvfb :99 -screen 0 1920x1080x16 -ac -nolisten tcp &
sleep 5

# 1️⃣ Запуск Camoufox один раз headless, чтобы создал профиль и установил расширения
camoufox --headless "about:blank" &
CAMOUFOX_PID=$!
sleep 15   # даём время на установку расширений
kill $CAMOUFOX_PID || true

# 2️⃣ Папка профиля Camoufox
PROFILE_DIR="/root/.camoufox/profile"

# 3️⃣ Вывод установленных расширений
echo "📦 Проверка установленных расширений:"
if [ -d "$PROFILE_DIR/extensions" ]; then
    ls -l "$PROFILE_DIR/extensions"
else
    echo "⚠️ Папка extensions не найдена. Расширения, возможно, не установлены."
fi

# 4️⃣ Запуск основного сервера
exec python3 /app/camoufox_server.py
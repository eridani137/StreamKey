import asyncio
import logging
import os
import random
import re
import shutil
from datetime import datetime
from typing import Callable

from playwright.async_api import Page, Browser

import config
from cloudflare import check_cloudflare, bypass_cloudflare
from configure_logger import configure
from exceptions import CloudflareRestartException

logger = logging.getLogger(__name__)
configure(logger)


async def safe_goto(page: Page, url: str) -> bool:
    for attempt in range(config.MAX_RETRIES_PAGE):
        try:
            logger.info(f"➡️ Переход на {url} (попытка {attempt + 1}/{config.MAX_RETRIES_PAGE})")
            await page.goto(url, wait_until=config.WAIT_UNTIL)
            logger.info(f"✅ Успешный переход на {url}")
            if await check_cloudflare(page):
                await bypass_cloudflare(page)
            return True
        except CloudflareRestartException:
            logger.warning("🔄 Получен сигнал на перезапуск")
            raise
        except Exception as e:
            logger.warning(f"⚠️ Ошибка при переходе на {url} (попытка {attempt + 1}/{config.MAX_RETRIES_PAGE}): {e}")

            if attempt < config.MAX_RETRIES_PAGE - 1:
                delay = random.uniform(config.RETRY_DELAY_MIN, config.RETRY_DELAY_MAX)
                logger.info(f"⏳ Ожидание {delay:.2f} секунд перед повторной попыткой...")
                await asyncio.sleep(delay)
                try:
                    await page.reload(timeout=30000)
                except:
                    logger.warning("⚠️ Не удалось обновить страницу")
            else:
                logger.error(f"❌ Все попытки исчерпаны для {url}")
                return False
    return False


async def save_page_html(page: Page, path: str, name_generator: Callable[[str], str]):
    if await check_cloudflare(page):
        raise CloudflareRestartException

    name = name_generator(page.url)
    content = await page.content()
    filename = f"{name}.html"
    filepath = os.path.join(path, filename)

    with open(filepath, "w", encoding="utf-8") as file:
        file.write(content)
    logger.info(f"💾 Сохранена страница: {filepath}")


async def cleanup_browser_gracefully(browser, worker_tabs):
    """Корректное закрытие всех вкладок и браузера"""
    logger.info("🚪 Начинаю корректное закрытие браузера...")

    try:
        if worker_tabs:
            await close_worker_tabs(worker_tabs)
            worker_tabs.clear()

        await asyncio.sleep(1)

        await browser.close()

        await asyncio.sleep(1)

    except Exception as e:
        logger.warning(f"⚠️🚪 Ошибка при корректном закрытии браузера: {e}")

    logger.info("✅🚪 Корректное закрытие браузера завершено")


async def create_worker_tabs(browser: Browser) -> list[Page]:
    worker_tabs = []
    for i in range(config.MAX_CONCURRENT_TABS):
        worker_page = await browser.new_page()
        worker_tabs.append(worker_page)
        logger.info(f"📑✨ Создана рабочая вкладка #{i + 1}")
    return worker_tabs


async def close_worker_tabs(worker_tabs: list[Page]):
    i = 0
    for worker_page in worker_tabs:
        try:
            await worker_page.close()
            logger.info(f"📑💨 Рабочая вкладка #{i + 1} закрыта")
        except Exception as e:
            logger.warning(f"⚠️📑 Ошибка при закрытии рабочей вкладки #{i + 1}: {e}")
        finally:
            i += 1


async def simulate_human_mouse_movement(page: Page, duration: int = 2):
    """
    Имитирует естественное движение мыши в течение указанного времени
    """
    try:
        logger.info(f"🖱️ Начинаю имитацию движения мыши на {duration} секунд...")

        # Получаем размеры страницы
        viewport = page.viewport_size
        width = viewport.get('width', 1920)
        height = viewport.get('height', 1080)

        start_time = asyncio.get_event_loop().time()

        # Начальная позиция мыши
        current_x = random.randint(100, width - 100)
        current_y = random.randint(100, height - 100)

        # Перемещаем мышь в начальную позицию
        await page.mouse.move(current_x, current_y)

        while (asyncio.get_event_loop().time() - start_time) < duration:
            # Генерируем случайное направление движения
            direction_x = random.choice([-1, 1])
            direction_y = random.choice([-1, 1])

            # Случайное расстояние движения (от 20 до 150 пикселей)
            distance = random.randint(20, 150)

            # Вычисляем новые координаты
            new_x = current_x + (direction_x * distance)
            new_y = current_y + (direction_y * distance)

            # Ограничиваем координаты границами экрана
            new_x = max(50, min(width - 50, new_x))
            new_y = max(50, min(height - 50, new_y))

            # Плавное движение мыши
            steps = random.randint(3, 8)  # Количество промежуточных шагов
            for step in range(steps):
                intermediate_x = current_x + (new_x - current_x) * (step + 1) / steps
                intermediate_y = current_y + (new_y - current_y) * (step + 1) / steps

                await page.mouse.move(intermediate_x, intermediate_y)
                # Небольшая задержка между шагами
                await asyncio.sleep(random.uniform(0.01, 0.05))

            # Обновляем текущие координаты
            current_x, current_y = new_x, new_y

            # Случайная пауза между движениями
            await asyncio.sleep(random.uniform(0.1, 0.3))

            # Иногда делаем более длительную паузу (имитация чтения/размышления)
            if random.random() < 0.15:  # 15% вероятность
                await asyncio.sleep(random.uniform(0.5, 1.5))

        logger.info("✅🖱️ Имитация движения мыши завершена")

    except Exception as e:
        logger.warning(f"⚠️🖱️ Ошибка при имитации движения мыши: {e}")

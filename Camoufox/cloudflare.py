import asyncio
import logging
import random

from playwright.async_api import Page

import config
from configure_logger import configure
from exceptions import CloudflareRestartException

logger = logging.getLogger(__name__)
configure(logger)


async def bypass_cloudflare(page: Page):
    """
    Улучшенная функция обхода Cloudflare с таймаутами и восстановлением
    """
    try:
        # Проверяем загрузку страницы с таймаутом
        try:
            await asyncio.wait_for(
                page.wait_for_load_state(config.WAIT_UNTIL),
                timeout=config.CLOUDFLARE_TIMEOUTS
            )
        except asyncio.TimeoutError:
            logger.warning("⏳ Таймаут ожидания загрузки страницы")
            # Продолжаем, возможно страница частично загружена

        # Проверяем наличие Cloudflare
        if not await check_cloudflare(page):
            logger.info("✅ Cloudflare не обнаружен")
            return True

        logger.warning("🛡️ Обнаружена защита Cloudflare. Запускаю процедуру обхода...")

        # Имитируем движение мыши при обнаружении Cloudflare
        # await simulate_human_mouse_movement(page)

        for attempt in range(config.CLOUDFLARE_ATTEMPTS):
            logger.info(f"🛡️ Попытка обхода {attempt + 1}/{config.CLOUDFLARE_ATTEMPTS}...")

            try:
                # Поиск iframe с таймаутом
                challenge_frame = None
                logger.info("🔍 Поиск iframe Cloudflare...")

                for search_attempt in range(config.CLOUDFLARE_FIND_SECONDS):
                    try:
                        # Обновляем список фреймов
                        await asyncio.sleep(1)  # Небольшая пауза для обновления DOM

                        if not await check_cloudflare(page):
                            return True

                        for frame in page.frames:
                            if frame.url and frame.url.startswith("https://challenges.cloudflare.com"):
                                challenge_frame = frame
                                logger.info(f"✅🔍 Найден iframe Cloudflare: {frame.url}")
                                break

                        if challenge_frame:
                            break

                        # Также проверяем iframe по селектору
                        iframe_element = await page.query_selector('iframe[src*="cloudflare"]')
                        if iframe_element:
                            logger.info("✅🔍 Найден iframe Cloudflare по селектору")
                            break

                        if search_attempt % 5 == 0:
                            logger.info(f"⏳🔍 Поиск iframe... ({search_attempt + 1}/{config.CLOUDFLARE_FIND_SECONDS})")

                        await asyncio.sleep(1)

                    except Exception as e:
                        logger.warning(f"⚠️🔍 Ошибка при поиске iframe (попытка {search_attempt + 1}): {e}")
                        await asyncio.sleep(1)

                if not challenge_frame:
                    logger.error(f"❌🔍 Не удалось найти iframe Cloudflare за {config.CLOUDFLARE_FIND_SECONDS} секунд")

                    # Попытка альтернативного метода - поиск по селектору
                    try:
                        iframe_element = await asyncio.wait_for(
                            page.query_selector('iframe[src*="cloudflare"]'),
                            timeout=config.CLOUDFLARE_TIMEOUTS
                        )
                        if iframe_element:
                            logger.info("💡 Используем альтернативный метод с iframe элементом")
                            await _handle_iframe_element(page, iframe_element)
                            if await _wait_for_cloudflare_completion(page):
                                return True
                    except Exception as e:
                        logger.error(f"❌💡 Альтернативный метод не удался: {e}")

                    continue

                # Получаем координаты iframe
                logger.info("🗺️ Iframe Cloudflare найден. Получаю его координаты")
                try:
                    frame_element = await asyncio.wait_for(
                        challenge_frame.frame_element(),
                        timeout=config.CLOUDFLARE_TIMEOUTS
                    )

                    if not frame_element:
                        logger.error("❌ Не удалось получить элемент iframe")
                        continue

                    await asyncio.sleep(7)

                    bounding_box = await asyncio.wait_for(
                        frame_element.bounding_box(),
                        timeout=config.CLOUDFLARE_TIMEOUTS
                    )

                    if not bounding_box:
                        logger.error("❌ Не удалось получить размеры iframe")
                        await asyncio.sleep(5)
                        continue

                except asyncio.TimeoutError:
                    logger.error("⏳❌ Таймаут при получении координат iframe")
                    continue
                except Exception as e:
                    logger.error(f"❌ Ошибка при получении координат iframe: {e}")
                    continue

                # Небольшое движение мыши перед кликом
                checkbox_x = bounding_box["x"] + 25
                checkbox_y = bounding_box["y"] + bounding_box["height"] / 2

                # Подводим мышь к чекбоксу с небольшим отклонением
                pre_click_x = checkbox_x + random.randint(-10, 10)
                pre_click_y = checkbox_y + random.randint(-10, 10)
                await page.mouse.move(pre_click_x, pre_click_y)
                await asyncio.sleep(random.uniform(0.2, 0.5))

                # Кликаем по чекбоксу
                try:
                    logger.info(f"🖱️🎯 Клик по координатам: ({checkbox_x}, {checkbox_y})")
                    await asyncio.wait_for(
                        page.mouse.click(x=checkbox_x, y=checkbox_y),
                        timeout=config.CLOUDFLARE_TIMEOUTS
                    )

                except asyncio.TimeoutError:
                    logger.error("⏳❌ Таймаут при клике по чекбоксу")
                    continue
                except Exception as e:
                    logger.error(f"❌🖱️ Ошибка при клике по чекбоксу: {e}")
                    continue

                # Ждем завершения проверки
                logger.info("⏳🛡️ Ожидание завершения проверки Cloudflare...")
                if await _wait_for_cloudflare_completion(page):
                    logger.info("✅🛡️ Cloudflare успешно пройден!")
                    return True
                else:
                    logger.warning(
                        f"⚠️🛡️ Проверка Cloudflare не пройдена (попытка {attempt + 1}/{config.CLOUDFLARE_ATTEMPTS})")

            except Exception as e:
                logger.error(f"❌ Ошибка во время попытки обхода №{attempt + 1}: {type(e).__name__}: {e}")

            # Проверяем, не прошли ли мы защиту случайно
            try:
                if not await check_cloudflare(page):
                    logger.info("🤔✅ Похоже, защита была пройдена во время обработки ошибки. Продолжаем")
                    return True
            except Exception as e:
                logger.warning(f"⚠️ Ошибка при финальной проверке: {e}")

            # Пауза перед следующей попыткой
            if attempt < config.CLOUDFLARE_ATTEMPTS - 1:
                wait_time = 3 + attempt  # Увеличиваем время ожидания
                logger.info(f"⏳ Пауза {wait_time} секунд перед следующей попыткой...")
                await asyncio.sleep(wait_time)

        logger.critical(f"🔥🛡️ Не удалось обойти защиту Cloudflare после {config.CLOUDFLARE_ATTEMPTS} попыток")
        raise CloudflareRestartException("Сбой обхода Cloudflare для перезапуска")

    except CloudflareRestartException:
        raise
    except Exception as e:
        logger.error(f"💥 Критическая ошибка в функции bypass_cloudflare: {type(e).__name__}: {e}")
        raise CloudflareRestartException(f"Непредвиденная ошибка в bypass_cloudflare: {e}")


async def _handle_iframe_element(page: Page, iframe_element):
    """
    Обработка iframe элемента как альтернативный метод
    """
    try:
        bounding_box = await asyncio.wait_for(
            iframe_element.bounding_box(),
            timeout=5
        )

        if bounding_box:
            checkbox_x = bounding_box["x"] + 25
            checkbox_y = bounding_box["y"] + bounding_box["height"] / 2
            await page.mouse.click(x=checkbox_x, y=checkbox_y)
            logger.info("🖱️💡 Клик по iframe элементу выполнен")
        else:
            # Если не получили координаты, пробуем кликнуть по самому элементу
            await iframe_element.click()
            logger.info("🖱️💡 Клик по iframe элементу (без координат) выполнен")

    except Exception as e:
        logger.error(f"❌💡 Ошибка при обработке iframe элемента: {e}")
        raise


async def _wait_for_cloudflare_completion(page: Page, max_wait_seconds=35):
    """
    Ожидание завершения проверки Cloudflare с промежуточными проверками
    """
    try:
        # Небольшая начальная пауза
        await asyncio.sleep(2)

        for second in range(max_wait_seconds):
            try:
                # Проверяем состояние загрузки
                if second % 5 == 0:
                    try:
                        await asyncio.wait_for(
                            page.wait_for_load_state(config.WAIT_UNTIL),
                            timeout=3
                        )
                    except asyncio.TimeoutError:
                        pass  # Продолжаем ожидание

                # Проверяем, прошли ли мы защиту
                if not await check_cloudflare(page):
                    logger.info(f"✅🛡️ Cloudflare пройден через {second + 1} секунд")
                    return True

                # Проверяем на ошибки доступа
                try:
                    error_elements = await page.query_selector_all('text="Access denied", text="Доступ запрещен"')
                    if error_elements:
                        logger.error("🚫 Получен отказ в доступе от Cloudflare")
                        return False
                except Exception:
                    pass

                # Логируем прогресс каждые 5 секунд
                if second % 5 == 0 and second > 0:
                    logger.info(f"⏳🛡️ Ожидание завершения проверки... ({second}/{max_wait_seconds} сек)")

                await asyncio.sleep(1)

            except Exception as e:
                logger.warning(f"⚠️ Ошибка при ожидании завершения проверки: {e}")
                await asyncio.sleep(1)

        logger.warning(f"⏳❌ Таймаут ожидания завершения проверки Cloudflare ({max_wait_seconds} сек)")
        return False

    except Exception as e:
        logger.error(f"💥 Критическая ошибка при ожидании завершения: {e}")
        return False


async def check_cloudflare(page: Page) -> bool:
    # if "challenge" in page.url:
    #     logger.info("🤖 Обнаружена страница с капчей (по URL)")
    #     return True

    content = await page.content()
    if "подтвердите, что вы человек" in content.lower() or "challenge.cloudflare.com" in content.lower():
        logger.warning(f"🤖 Обнаружена капча по контенту страницы")
        return True

    title = await page.title()
    if title and ("security check" in title.lower() or
                  "cloudflare" in title.lower() or
                  "проверка безопасности" in title.lower() or
                  "just a moment..." in title.lower() or
                  "один момент..." in title.lower() or
                  "site connection is secure" in title.lower() or
                  "checking if the site connection" in title.lower()):
        logger.warning(f"🤖 Обнаружена капча по заголовку страницы: {title}")
        return True

    has_captcha = await page.evaluate('''
                () => {
                    return document.querySelector('.cf-turnstile') !== null || 
                           document.querySelector('#challenge-form') !== null ||
                           document.querySelector('iframe[src*="challenges.cloudflare.com"]') !== null ||
                           document.querySelector('iframe[src*="turnstile"]') !== null;
                }
            ''')

    if has_captcha:
        logger.warning("🤖 Обнаружены элементы капчи на странице")
        return True

    return False

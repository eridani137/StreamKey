import asyncio
import logging
from contextlib import asynccontextmanager
from typing import Optional

from camoufox.async_api import AsyncCamoufox
from fastapi import FastAPI, HTTPException, Response
from pydantic import BaseModel
import uvicorn

from scalar_fastapi import get_scalar_api_reference

import config
from browser_utils import safe_goto
from configure_logger import configure

logger = logging.getLogger(__name__)
configure(logger)

# ---------------- Models ----------------
class URLRequest(BaseModel):
    url: str
    wait_time: int = 3


class HTMLResponse(BaseModel):
    url: str
    html: str
    status: str
    page_title: Optional[str] = None
    final_url: Optional[str] = None


# ---------------- Globals & sync primitives ----------------
# Глобальный объект браузера (один на процесс)
browser: Optional[AsyncCamoufox] = None

# Блокировка для последовательной инициализации/рестарта браузера.
_browser_lock = asyncio.Lock()

# Ограничение одновременно открытых страниц. Настрой под доступную память/ресурсы.
# Пример: 6 — уменьшай если хост жалуется на память.
_pages_semaphore = asyncio.Semaphore(6)


# ---------------- Helper functions ----------------
async def is_browser_alive() -> bool:
    """Проверка, что браузер жив и готов к работе."""
    global browser
    if browser is None:
        return False

    try:
        # Простая проверка доступности: атрибут contexts и опционально вызов version()
        _ = getattr(browser, "contexts", None)
        # optionally try a cheap call if available
        try:
            # некоторые реализации могут поддерживать version() или user_agent()
            if hasattr(browser, "version"):
                await browser.version()
        except Exception:
            # не критично — браузер может быть в живом состоянии, но version() недоступен
            pass
        return True
    except Exception as e:
        logger.warning("Браузер недоступен в is_browser_alive: %s", e)
        return False


async def ensure_browser() -> AsyncCamoufox:
    """
    Гарантирует наличие живого браузера. Использует блокировку чтобы избежать гонок.
    Попытки инстанцирования: 2.
    """
    global browser

    # fast-path
    if browser is not None and await is_browser_alive():
        return browser

    async with _browser_lock:
        # кто-то другой мог инициализировать пока мы ждали
        if browser is not None and await is_browser_alive():
            return browser

        # если есть старый объект — попытка корректного закрытия
        if browser:
            try:
                await browser.__aexit__(None, None, None)
            except Exception as e:
                logger.warning("Ошибка при закрытии старого браузера перед реинициализацией: %s", e)
            finally:
                browser = None

        last_exc = None
        for attempt in range(2):
            try:
                logger.info("Инициализация Camoufox браузера (attempt %d)...", attempt + 1)
                browser = await AsyncCamoufox(**config.BROWSER_OPTIONS).__aenter__()
                logger.info("Camoufox успешно инициализирован")
                return browser
            except Exception as e:
                logger.exception("Не удалось инициализировать браузер (attempt %d): %s", attempt + 1, e)
                last_exc = e
                browser = None
                # небольшая пауза перед повторной попыткой
                await asyncio.sleep(1)

        # если не удалось после всех попыток
        raise HTTPException(503, f"Не удалось инициализировать браузер: {last_exc!s}")


# ---------------- FastAPI lifespan & app ----------------
@asynccontextmanager
async def lifespan(app: FastAPI):
    """Инициализация и корректное закрытие Camoufox при старте/остановке приложения."""
    global browser
    try:
        browser = await AsyncCamoufox(**config.BROWSER_OPTIONS).__aenter__()
        logger.info("Camoufox-браузер инициализирован при старте приложения")
    except Exception as exc:
        logger.error("Не удалось запустить Camoufox при старте: %s", exc)
        browser = None

    yield

    if browser:
        try:
            await browser.__aexit__(None, None, None)
            logger.info("Camoufox-браузер корректно закрыт при завершении приложения")
        except Exception as e:
            logger.error("Ошибка при закрытии браузера в lifespan: %s", e)


app = FastAPI(
    title="Camoufox HTTP server",
    lifespan=lifespan,
    description="API для управления браузером Camoufox",
    version="1.0.0"
)


@app.get("/docs", include_in_schema=False)
async def scalar_html():
    """Генерация документации API с помощью Scalar."""
    return get_scalar_api_reference(
        openapi_url=app.openapi_url,
        title=app.title,
    )


@app.get("/health")
async def health() -> dict[str, str]:
    """Проверка состояния сервиса."""
    browser_ready = await is_browser_alive()
    return {
        "status": "ok",
        "service": "camoufox-server",
        "browser_ready": str(browser_ready)
    }


# ---------------- Core endpoints ----------------
@app.post("/fetch-html", response_model=HTMLResponse)
async def fetch_html(req: URLRequest):
    """
    Получение HTML-контента страницы.
    Защита: семафор ограничивает число одновременно открытых страниц.
    Повторная попытка new_page() при падении браузера.
    """
    current_browser = await ensure_browser()

    page = None
    acquired = False
    try:
        logger.info(f"⏩ HTML-запрос: {req.url}")

        # Ограничим число одновременно открытых страниц
        await _pages_semaphore.acquire()
        acquired = True

        # Попытка создать страницу — если browser умер, пробуем реинициализировать и повторить один раз
        try:
            page = await current_browser.new_page()
        except Exception as e_newpage:
            logger.warning("new_page() упал: %s — пробуем реинициализировать браузер и повторить once", e_newpage)
            try:
                # Попытка реинициализировать browser
                current_browser = await ensure_browser()
                page = await current_browser.new_page()
            except Exception as e2:
                logger.exception("Повторный new_page() тоже упал: %s", e2)
                raise HTTPException(500, f"Browser.new_page failed: {e2!s}")

        if not await safe_goto(page, req.url):
            raise HTTPException(400, "Не удалось загрузить страницу")

        # Дополнительное ожидание для рендеринга
        await page.wait_for_timeout(req.wait_time * 1000)

        html = await page.content()
        page_title = await page.title()
        final_url = page.url

        logger.info(f"✅ HTML получен ({len(html)} символов, title: {page_title})")

        return HTMLResponse(
            url=req.url,
            html=html,
            status="success",
            page_title=page_title,
            final_url=final_url
        )
    except HTTPException:
        # пробрасываем HTTPException как есть
        raise
    except Exception as exc:
        logger.exception("Ошибка загрузки %s: %s", req.url, exc)
        # при фатальной ошибке - попытка зачистить глобальный браузер, чтобы следующая попытка реинициализировала его
        async with _browser_lock:
            try:
                if browser:
                    await browser.__aexit__(None, None, None)
            except Exception:
                logger.warning("Ошибка закрытия браузера после исключения")
            finally:
                # явно очистить
                try:
                    # mypy / typing
                    globals_browser = globals()
                    globals_browser["browser"] = None
                except Exception:
                    pass
        raise HTTPException(500, f"Ошибка: {exc!s}")
    finally:
        if page:
            try:
                await page.close()
            except Exception as e:
                logger.warning(f"Ошибка закрытия страницы: {e}")
        if acquired:
            _pages_semaphore.release()


@app.post("/fetch-screenshot")
async def fetch_screenshot(req: URLRequest):
    """
    Получение скриншота страницы (PNG).
    Повторная попытка new_page() при падении браузера и семафор.
    """
    current_browser = await ensure_browser()

    page = None
    acquired = False
    try:
        logger.info(f"⏩ Screenshot-запрос: {req.url}")

        await _pages_semaphore.acquire()
        acquired = True

        try:
            page = await current_browser.new_page()
        except Exception as e_newpage:
            logger.warning("new_page() упал в fetch_screenshot: %s — пробуем реинициализировать и повторить", e_newpage)
            try:
                current_browser = await ensure_browser()
                page = await current_browser.new_page()
            except Exception as e2:
                logger.exception("Повторный new_page() в fetch_screenshot тоже упал: %s", e2)
                raise HTTPException(500, f"Browser.new_page failed: {e2!s}")

        # установим viewport
        try:
            await page.set_viewport_size({"width": 1920, "height": 1080})
        except Exception:
            # некоторые реализации могут не поддерживать set_viewport_size
            pass

        if not await safe_goto(page, req.url):
            raise HTTPException(400, "Не удалось загрузить страницу")

        await page.wait_for_timeout(req.wait_time * 1000)

        screenshot = await page.screenshot(
            full_page=True,
            type="png"
        )

        logger.info(f"✅ Скриншот получен ({len(screenshot)} байт)")

        return Response(
            content=screenshot,
            media_type="image/png",
            headers={
                "Content-Disposition": f"attachment; filename=screenshot_{abs(hash(req.url))}.png"
            }
        )
    except HTTPException:
        raise
    except Exception as exc:
        logger.exception("Ошибка создания скриншота %s: %s", req.url, exc)
        async with _browser_lock:
            try:
                if browser:
                    await browser.__aexit__(None, None, None)
            except Exception:
                logger.warning("Ошибка закрытия браузера после исключения в screenshot")
            finally:
                try:
                    globals()["browser"] = None
                except Exception:
                    pass
        raise HTTPException(500, f"Ошибка создания скриншота: {exc!s}")
    finally:
        if page:
            try:
                await page.close()
            except Exception as e:
                logger.warning(f"Ошибка закрытия страницы: {e}")
        if acquired:
            _pages_semaphore.release()


@app.get("/browser-info")
async def browser_info():
    """Получение информации о браузере."""
    if not await is_browser_alive():
        return {"browser_ready": False, "error": "Браузер не инициализирован или недоступен"}

    try:
        contexts_count = len(browser.contexts) if (browser and hasattr(browser, 'contexts')) else 0

        version = "unknown"
        user_agent = "unknown"

        try:
            if hasattr(browser, "version"):
                version = await browser.version()
        except Exception:
            pass

        try:
            if hasattr(browser, "user_agent"):
                user_agent = await browser.user_agent()
        except Exception:
            pass

        return {
            "browser_ready": True,
            "version": version,
            "user_agent": user_agent,
            "contexts_count": contexts_count
        }
    except Exception as e:
        return {"browser_ready": False, "error": str(e)}


@app.post("/restart-browser")
async def restart_browser():
    """Принудительный перезапуск браузера."""
    global browser
    async with _browser_lock:
        try:
            if browser:
                try:
                    await browser.__aexit__(None, None, None)
                except Exception as e:
                    logger.warning(f"Ошибка при закрытии старого браузера: {e}")
                finally:
                    browser = None

            browser = await AsyncCamoufox(**config.BROWSER_OPTIONS).__aenter__()
            logger.info("Браузер перезапущен успешно")
            return {"status": "success", "message": "Браузер перезапущен"}
        except Exception as e:
            logger.exception("Ошибка перезапуска браузера: %s", e)
            browser = None
            raise HTTPException(500, f"Ошибка перезапуска браузера: {str(e)}")


# ---------------- Run ----------------
if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8080, log_level="info")

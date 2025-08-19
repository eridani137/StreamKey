import logging
from contextlib import asynccontextmanager

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


class URLRequest(BaseModel):
    url: str
    wait_time: int = 3


class HTMLResponse(BaseModel):
    url: str
    html: str
    status: str
    page_title: str | None = None
    final_url: str | None = None


browser: AsyncCamoufox | None = None


async def is_browser_alive() -> bool:
    """Проверка, что браузер жив и готов к работе."""
    global browser
    if browser is None:
        return False
    
    try:
        # Попытка получить версию как простая проверка живости
        await browser.version()
        return True
    except Exception as e:
        logger.warning(f"Браузер недоступен: {e}")
        return False


async def ensure_browser() -> AsyncCamoufox:
    """Гарантирует наличие живого браузера."""
    global browser
    
    if browser is None or not await is_browser_alive():
        logger.info("Инициализация нового браузера...")
        try:
            # Закрыть старый браузер если он есть
            if browser:
                try:
                    await browser.__aexit__(None, None, None)
                except:
                    pass
            
            browser = await AsyncCamoufox(**config.BROWSER_OPTIONS).__aenter__()
            logger.info("Новый браузер успешно инициализирован")
        except Exception as e:
            logger.error(f"Не удалось инициализировать браузер: {e}")
            browser = None
            raise HTTPException(503, f"Не удалось инициализировать браузер: {e}")
    
    return browser


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Инициализация и корректное закрытие Camoufox."""
    global browser
    try:
        browser = await AsyncCamoufox(**config.BROWSER_OPTIONS).__aenter__()
        logger.info("Camoufox-браузер инициализирован при старте")
    except Exception as exc:
        logger.error("Не удалось запустить Camoufox при старте: %s", exc)
        browser = None

    yield

    if browser:
        try:
            await browser.__aexit__(None, None, None)
            logger.info("Camoufox-браузер закрыт")
        except Exception as e:
            logger.error(f"Ошибка при закрытии браузера: {e}")


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


@app.post("/fetch-html", response_model=HTMLResponse)
async def fetch_html(req: URLRequest):
    """Получение HTML-контента страницы."""
    
    # Гарантируем наличие живого браузера
    current_browser = await ensure_browser()
    
    page = None
    try:
        logger.info(f"⏩ HTML-запрос: {req.url}")

        page = await current_browser.new_page()

        if not await safe_goto(page, req.url):
            raise HTTPException(400, "Не удалось загрузить страницу")

        # Дополнительное ожидание
        await page.wait_for_timeout(req.wait_time * 1000)

        # Получение данных
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
    except Exception as exc:
        logger.error("Ошибка загрузки %s: %s", req.url, exc)
        raise HTTPException(500, f"Ошибка: {exc!s}")
    finally:
        if page:
            try:
                await page.close()
            except Exception as e:
                logger.warning(f"Ошибка закрытия страницы: {e}")


@app.post("/fetch-screenshot")
async def fetch_screenshot(req: URLRequest):
    """Получение скриншота страницы."""
    
    # Гарантируем наличие живого браузера
    current_browser = await ensure_browser()

    page = None
    try:
        logger.info(f"⏩ Screenshot-запрос: {req.url}")

        page = await current_browser.new_page()
        await page.set_viewport_size({"width": 1920, "height": 1080})

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
                "Content-Disposition": f"attachment; filename=screenshot_{hash(req.url)}.png"
            }
        )

    except Exception as exc:
        logger.error("Ошибка скриншота %s: %s", req.url, exc)
        raise HTTPException(500, f"Ошибка создания скриншота: {exc!s}")
    finally:
        if page:
            try:
                await page.close()
            except Exception as e:
                logger.warning(f"Ошибка закрытия страницы: {e}")


@app.get("/browser-info")
async def browser_info():
    """Получение информации о браузере."""
    if not await is_browser_alive():
        return {"browser_ready": False, "error": "Браузер не инициализирован или недоступен"}

    try:
        version = await browser.version()
        user_agent = await browser.user_agent()

        return {
            "browser_ready": True,
            "version": version,
            "user_agent": user_agent,
            "contexts_count": len(browser.contexts)
        }
    except Exception as e:
        return {"browser_ready": False, "error": str(e)}


@app.post("/restart-browser")
async def restart_browser():
    """Принудительный перезапуск браузера."""
    global browser
    try:
        # Закрытие старого браузера
        if browser:
            try:
                await browser.__aexit__(None, None, None)
            except:
                pass

        # Инициализация нового браузера
        browser = await AsyncCamoufox(**config.BROWSER_OPTIONS).__aenter__()

        logger.info("Браузер перезапущен успешно")
        return {"status": "success", "message": "Браузер перезапущен"}

    except Exception as e:
        logger.error(f"Ошибка перезапуска браузера: {e}")
        browser = None
        raise HTTPException(500, f"Ошибка перезапуска браузера: {str(e)}")


if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8080, log_level="info")

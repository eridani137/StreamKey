#!/usr/bin/env python3
import logging
from contextlib import asynccontextmanager

from camoufox.async_api import AsyncCamoufox
from fastapi import FastAPI, HTTPException, Response
from pydantic import BaseModel
import uvicorn

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

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

@asynccontextmanager
async def lifespan(app: FastAPI):
    """Инициализация и корректное закрытие Camoufox."""
    global browser
    try:
        browser = await AsyncCamoufox(headless=True, humanize=True, geoip=True).__aenter__()
        logger.info("Camoufox-браузер инициализирован")
    except Exception as exc:
        logger.error("Не удалось запустить Camoufox: %s", exc)

    yield

    if browser:
        await browser.__aexit__(None, None, None)
        logger.info("Camoufox-браузер закрыт")

app = FastAPI(title="Camoufox HTTP server", lifespan=lifespan)

@app.get("/health")
async def health() -> dict[str, str]:
    return {"status": "ok", "service": "camoufox-server", "browser_ready": str(browser is not None)}

@app.post("/fetch-html", response_model=HTMLResponse)
async def fetch_html(req: URLRequest):
    global browser
    if browser is None:
        raise HTTPException(503, "Браузер не инициализирован")

    page = None
    try:
        logger.info(f"⏩ HTML-запрос: {req.url}")
        
        page = await browser.new_page()
        
        # Переход на страницу
        response = await page.goto(req.url, wait_until="networkidle", timeout=30000)
        if not response:
            raise HTTPException(400, "Не удалось загрузить страницу")
        
        # Дополнительное ожидание
        await page.wait_for_timeout(3000)
        
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
    global browser
    if browser is None:
        raise HTTPException(503, "Браузер не инициализирован")

    page = None
    try:
        logger.info(f"⏩ Screenshot-запрос: {req.url}")
        
        page = await browser.new_page()
        
        # Установка viewport для консистентных скриншотов
        await page.set_viewport_size({"width": 1920, "height": 1080})
        
        # Переход на страницу
        await page.goto(req.url, wait_until="networkidle", timeout=30000)
        
        # Дополнительное ожидание
        await page.wait_for_timeout(3000)
        
        # Получение скриншота
        screenshot = await page.screenshot(
            full_page=True,
            type="png"
        )
        
        logger.info(f"✅ Скриншот получен ({len(screenshot)} байт)")
        
        # Возврат как бинарные данные PNG
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
    global browser
    if not browser:
        return {"browser_ready": False, "error": "Браузер не инициализирован"}
    
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
    global browser
    try:
        # Закрытие старого браузера
        if browser:
            await browser.__aexit__(None, None, None)
        
        # Инициализация нового браузера
        browser = await AsyncCamoufox(
            headless=True,
            humanize=True,
            geoip=True
        ).__aenter__()
        
        logger.info("Браузер перезапущен успешно")
        return {"status": "success", "message": "Браузер перезапущен"}
        
    except Exception as e:
        logger.error(f"Ошибка перезапуска браузера: {e}")
        browser = None
        raise HTTPException(500, f"Ошибка перезапуска браузера: {str(e)}")

if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8080, log_level="info")
FROM python:3.12-slim

WORKDIR /app
ENV DEBIAN_FRONTEND=noninteractive
ENV PYTHONUNBUFFERED=1

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        xvfb \
        libgtk-3-0 \
        libx11-xcb1 \
        libasound2 \
        curl && \
    rm -rf /var/lib/apt/lists/*

RUN pip install --no-cache-dir camoufox[geoip] fastapi uvicorn

RUN python -m camoufox fetch

COPY Camoufox/camoufox_server.py Camoufox/start_camoufox.sh /app/
RUN chmod +x /app/start_camoufox.sh

EXPOSE 8080

ENTRYPOINT ["/app/start_camoufox.sh"]
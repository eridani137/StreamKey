FROM python:3.13-slim

WORKDIR /app
ENV DEBIAN_FRONTEND=noninteractive
ENV PYTHONUNBUFFERED=1

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        xvfb \
        libgtk-3-0 \
        libx11-xcb1 \
        libasound2 \
        curl \
        dos2unix && \
    rm -rf /var/lib/apt/lists/*

RUN pip install --no-cache-dir camoufox[geoip] scalar-fastapi colorlog fastapi uvicorn

RUN python -m camoufox fetch

COPY Camoufox/ /app/
RUN dos2unix /app/start_camoufox.sh && chmod +x /app/start_camoufox.sh

EXPOSE 8080

ENTRYPOINT ["bash", "/app/start_camoufox.sh"]
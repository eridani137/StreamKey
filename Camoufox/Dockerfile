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

# Копируем приложение
COPY Camoufox/ /app/

# Создаём папку для политик + копируем policies.json
RUN mkdir -p /usr/lib/camoufox/distribution/
COPY Camoufox/policies.json /usr/lib/camoufox/distribution/policies.json

# Копируем расширения (.xpi)
COPY Camoufox/addons/ /app/addons/

RUN dos2unix /app/start_camoufox.sh && chmod +x /app/start_camoufox.sh

EXPOSE 8080

ENTRYPOINT ["bash", "/app/start_camoufox.sh"]

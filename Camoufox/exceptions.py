class CloudflareRestartException(Exception):
    """Исключение для перезапуска скрипта при критическом сбое Cloudflare."""
    pass


class CloudflareInWorkerException(Exception):
    """Сигнализирует об обнаружении Cloudflare в рабочей вкладке."""
    pass


class AutoRestartException(Exception):
    """Исключение для автоматического перезапуска по таймеру"""
    pass


class NoModelsAddedException(Exception):
    """Исключение для случая, когда модели не добавляются в течение заданного времени"""
    pass

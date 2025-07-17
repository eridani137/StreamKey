Write-Host "Начало сборки StreamKey Extension для всех платформ..." -ForegroundColor Green

$targets = "chrome", "firefox"
$buildMode = "dev"

foreach ($target in $targets) {
    Write-Host "--------------------------------------------------------" -ForegroundColor Yellow
    Write-Host "Сборка для: $($target.ToUpper())" -ForegroundColor Yellow
    Write-Host "--------------------------------------------------------"

    Write-Host "Запуск сборки Vite ($buildMode)..." -ForegroundColor Blue
    $npmCommand = "build:${target}-${buildMode}"
    npm run $npmCommand

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Ошибка сборки Vite для $target!" -ForegroundColor Red
        exit 1
    }

    $distPath = "dist/$target"

    Write-Host "Копирование манифеста и правил для $target..." -ForegroundColor Blue
    
    $manifestSource = "manifest.${target}.json"
    Copy-Item $manifestSource -Destination "$distPath/manifest.json" -Force
    
    Copy-Item "src/rules.json" -Destination $distPath -Force
}

Write-Host "========================================================" -ForegroundColor Green
Write-Host "Сборка завершена успешно для всех платформ!" -ForegroundColor Green
Write-Host "Готовые расширения находятся в папках 'dist/chrome' и 'dist/firefox'" -ForegroundColor Cyan
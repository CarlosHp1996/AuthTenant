#!/usr/bin/env pwsh

# Script para verificar e limpar a porta 5076 antes do debug
Write-Host "Verificando se a porta 5076 está em uso..." -ForegroundColor Yellow

$portCheck = netstat -ano | findstr :5076
if ($portCheck) {
    Write-Host "Porta 5076 está em uso. Identificando processo..." -ForegroundColor Red
    
    # Extrai o PID do processo
    $pid = ($portCheck | Select-String "LISTENING" | ForEach-Object { $_.Line.Split() | Where-Object { $_ -match "^\d+$" } } | Select-Object -First 1)
    
    if ($pid) {
        Write-Host "Finalizando processo PID: $pid" -ForegroundColor Yellow
        taskkill /PID $pid /F
        
        Start-Sleep -Seconds 2
        
        # Verifica novamente
        $portCheckAfter = netstat -ano | findstr :5076
        if (-not $portCheckAfter) {
            Write-Host "Porta 5076 liberada com sucesso!" -ForegroundColor Green
        } else {
            Write-Host "Ainda há processos usando a porta 5076. Pode ser necessário verificar manualmente." -ForegroundColor Red
        }
    }
} else {
    Write-Host "Porta 5076 está livre!" -ForegroundColor Green
}

Write-Host "Agora você pode iniciar o debug no VS Code (F5)." -ForegroundColor Cyan

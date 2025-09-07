#!/usr/bin/env pwsh

# Script simples para executar a API e abrir o navegador
Write-Host "Iniciando AuthTenant API..." -ForegroundColor Green

# Navega para a pasta do projeto
Set-Location "C:\Users\Carlos Henrique\Desktop\PROJETOSNOVOS\AcademiaCrawler\Backend\AuthTenant\AuthTenant.API\AuthTenant.API"

# Executa o projeto usando o profile HTTPS (que inclui launchBrowser)
dotnet run --launch-profile https

Write-Host "API finalizada!" -ForegroundColor Green

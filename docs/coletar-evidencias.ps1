# Gera as evidencias do CP4/CP5 em docs/evidencias/.
# Pre-requisitos: API rodando (dotnet run --project Recommenda.API) e MySQL no ar.
# Uso (na raiz do repo):  powershell -ExecutionPolicy Bypass -File docs\coletar-evidencias.ps1

param([string]$BaseUrl = "http://localhost:5283")

$ErrorActionPreference = "Stop"
$out = Join-Path $PSScriptRoot "evidencias"
New-Item -ItemType Directory -Force -Path $out | Out-Null
$bodyFile = Join-Path $env:TEMP "recommenda-body.json"

function Save-Curl([string]$name, [string[]]$curlArgs) {
    $file = Join-Path $out $name
    $result = & curl.exe -s -i @curlArgs
    $result | Out-File -FilePath $file -Encoding utf8
    Write-Host "-> docs/evidencias/$name"
}

function Write-Body([string]$json) {
    [IO.File]::WriteAllText($bodyFile, $json)
}

# 1) /health Healthy
Save-Curl "01-health-healthy.txt" @("$BaseUrl/health")

# 2) Artista para semear albuns
$suffix = Get-Random -Maximum 99999
Write-Body "{""name"":""Artista Evidencia $suffix"",""bio"":""Seed CP5"",""country"":""Brasil""}"
$artistJson = & curl.exe -s -X POST "$BaseUrl/api/artist" -H "Content-Type: application/json" --data-binary "@$bodyFile"
$artistId = ($artistJson | ConvertFrom-Json).id
Write-Host "Artista criado: $artistId"

# 3) 10 POSTs de album (dentro do limite) + 11o POST -> 429
$codes = @()
for ($i = 1; $i -le 10; $i++) {
    $year = 2000 + $i
    Write-Body "{""title"":""Album $suffix-$i"",""releaseDate"":""$year-01-01T00:00:00"",""artistId"":""$artistId""}"
    $code = & curl.exe -s -o NUL -w "%{http_code}" -X POST "$BaseUrl/api/album" -H "Content-Type: application/json" --data-binary "@$bodyFile"
    $codes += "POST /api/album #$i -> $code"
}
$codes | Out-File -FilePath (Join-Path $out "02-seed-posts-album.txt") -Encoding utf8
Write-Host "-> docs/evidencias/02-seed-posts-album.txt"

Write-Body "{""title"":""Album $suffix-11"",""releaseDate"":""2011-01-01T00:00:00"",""artistId"":""$artistId""}"
Save-Curl "03-rate-limit-429.txt" @("-X", "POST", "$BaseUrl/api/album", "-H", "Content-Type: application/json", "--data-binary", "@$bodyFile")

# 4) /health continua 200 depois do estouro
Save-Curl "04-health-apos-429.txt" @("$BaseUrl/health")

# 5) Versionamento
Save-Curl "05-v1-query-string.txt"  @("$BaseUrl/api/album?api-version=1.0")
Save-Curl "06-v1-header.txt"        @("-H", "X-Api-Version: 1.0", "$BaseUrl/api/album")
Save-Curl "07-v2-sem-versao.txt"    @("$BaseUrl/api/album")
Save-Curl "08-v2-query-string.txt"  @("$BaseUrl/api/album?api-version=2.0&page=1&pageSize=5")

# 6) Paginacao
Save-Curl "09-v2-page1-size2.txt"   @("$BaseUrl/api/album?page=1&pageSize=2")
Save-Curl "10-v2-page2-size2.txt"   @("$BaseUrl/api/album?page=2&pageSize=2")
Save-Curl "11-v2-page0-400.txt"     @("$BaseUrl/api/album?page=0")
Save-Curl "12-v2-pagesize9999-400.txt" @("$BaseUrl/api/album?pageSize=9999")
Save-Curl "13-v2-page-enorme-200.txt"  @("$BaseUrl/api/album?page=99999&pageSize=2")

# 7) Swagger (documentos por versao)
& curl.exe -s "$BaseUrl/swagger/v1.0/swagger.json" | Out-File -FilePath (Join-Path $out "14-swagger-v1.0.json") -Encoding utf8
& curl.exe -s "$BaseUrl/swagger/v2.0/swagger.json" | Out-File -FilePath (Join-Path $out "15-swagger-v2.0.json") -Encoding utf8
Write-Host "-> docs/evidencias/14-swagger-v1.0.json e 15-swagger-v2.0.json"

# 8) Erro tratado pelo GlobalExceptionHandler (404 com traceId)
Save-Curl "16-erro-404-traceid.txt" @("$BaseUrl/api/album/00000000-0000-0000-0000-000000000000")

Write-Host ""
Write-Host "Pronto. Falta: print do Swagger, /health com banco parado e saida do dotnet test (ver README)."

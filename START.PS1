function Get-Windows11DownloadUrl {
    <#
    .SYNOPSIS
        Получает прямую ссылку на ISO Windows 11 (последняя версия, Pro, русский, x64)
    #>
    # Параметры (можно изменить при необходимости)
    $ProductEditionId = "3262"      # Windows 11 Pro (x64) для 25H2
    $Locale           = "ru-ru"
    $LanguageName     = "Russian"
    $ArchType         = 1            # 1 = x64

    # Настройка TLS (для старых Windows)
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

    # Константы из Fido
    $org_id     = "y6jn8c31"
    $profileId  = "606624d44113"
    $instanceId = "560dc9f3-1aa5-4a2f-b63c-9e18f8d0e175"

    # Генерация уникальной сессии
    $sessionId = [guid]::NewGuid()

    # 1. Запрос к vlscppe.microsoft.com/tags
    Invoke-WebRequest -UseBasicParsing "https://vlscppe.microsoft.com/tags?org_id=$org_id&session_id=$sessionId" | Out-Null

    # 2. Получение параметров w и rticks из mdt.js
    $mdtJs = Invoke-WebRequest -UseBasicParsing "https://ov-df.microsoft.com/mdt.js?instanceId=$instanceId&PageId=si&session_id=$sessionId"
    $content = $mdtJs.Content
    if ($content -match '[?&]w=([A-F0-9]+)') { $w = $matches[1] } else { throw "Не удалось извлечь w" }
    if ($content -match 'rticks\=\"\+?(\d+)') { $rticks = $matches[1] } else { throw "Не удалось извлечь rticks" }

    # 3. Ответ на ov-df.microsoft.com
    $mdt = [DateTimeOffset]::Now.ToUnixTimeMilliseconds()
    Invoke-WebRequest -UseBasicParsing "https://ov-df.microsoft.com/?session_id=$sessionId&CustomerId=$instanceId&PageId=si&w=$w&mdt=$mdt&rticks=$rticks" | Out-Null

    # 4. Получение списка SKU (языков) для выбранного продукта
    $skuInfo = Invoke-RestMethod -UseBasicParsing "https://www.microsoft.com/software-download-connector/api/getskuinformationbyproductedition?profile=$profileId&productEditionId=$ProductEditionId&SKU=undefined&friendlyFileName=undefined&Locale=$Locale&sessionID=$sessionId"

    $targetSku = $skuInfo.Skus | Where-Object { $_.Language -eq $LanguageName }
    if (-not $targetSku) { throw "Язык '$LanguageName' не найден" }
    $skuId = $targetSku.Id

    # 5. Получение ссылок на скачивание для выбранного SKU
    $headers = @{ Referer = "https://www.microsoft.com/software-download/windows11" }
    $linksInfo = Invoke-RestMethod -UseBasicParsing -Headers $headers "https://www.microsoft.com/software-download-connector/api/GetProductDownloadLinksBySku?profile=$profileId&productEditionId=undefined&SKU=$skuId&friendlyFileName=undefined&Locale=$Locale&sessionID=$sessionId"

    $targetLink = $linksInfo.ProductDownloadOptions | Where-Object { $_.DownloadType -eq $ArchType }
    if (-not $targetLink) { throw "Архитектура (тип $ArchType) не найдена" }

    return $targetLink.Uri
}

# Пример использования
$url = Get-Windows11DownloadUrl
Write-Host "Ссылка на ISO: $url"
# При необходимости скопировать в буфер:
Set-Clipboard $url
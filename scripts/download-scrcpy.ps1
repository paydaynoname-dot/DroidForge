$ErrorActionPreference = 'Stop'

$version = 'v5.0.1'
$archiveUrl = "https://github.com/Genymobile/scrcpy/releases/download/$version/scrcpy-win64-$version.zip"
$expectedSha256 = 'b12a2c4ee8be317422451fc7dcf8ee20a71b5ea7ef9ad73ddd825a25316227a5'
$archivePath = Join-Path $env:TEMP "DroidForge-scrcpy-$version.zip"
$extractPath = Join-Path $env:TEMP "DroidForge-scrcpy-$version"
$destinationPath = Join-Path $PSScriptRoot '..\scrcpy'

Write-Output "Downloading official scrcpy $version from Genymobile GitHub…"
Invoke-WebRequest -Uri $archiveUrl -OutFile $archivePath
$actualSha256 = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualSha256 -ne $expectedSha256) {
    throw "The scrcpy archive checksum did not match. Expected $expectedSha256; received $actualSha256."
}

Expand-Archive -LiteralPath $archivePath -DestinationPath $extractPath -Force
$scrcpyExe = Get-ChildItem -LiteralPath $extractPath -Filter 'scrcpy.exe' -File -Recurse | Select-Object -First 1
if (-not $scrcpyExe) { throw 'The official archive did not contain scrcpy.exe.' }

$destinationPath = [System.IO.Path]::GetFullPath($destinationPath)
New-Item -ItemType Directory -Force -Path $destinationPath | Out-Null
Copy-Item -Path (Join-Path $scrcpyExe.DirectoryName '*') -Destination $destinationPath -Recurse -Force
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/Genymobile/scrcpy/$version/LICENSE" -OutFile (Join-Path $destinationPath 'LICENSE')

Write-Output "scrcpy $version is ready at $destinationPath"

$ErrorActionPreference = "Stop"

$project = Split-Path -Parent $PSScriptRoot
$packageDirectory = Join-Path $project "Builds\Packages\v1.4.0"
$windowsSource = Join-Path $project "Builds\Windows\v1.4.0"
$webglSource = Join-Path $project "Builds\WebGL\v1.4.0\*"
$androidSource = Join-Path $project "Builds\Android\MazeEscape-v1.4.0.apk"
$windowsArchive = Join-Path $packageDirectory "MazeEscape-Windows-v1.4.0.zip"
$webglArchive = Join-Path $packageDirectory "MazeEscape-WebGL-v1.4.0.zip"
$androidPackage = Join-Path $packageDirectory "MazeEscape-Android-v1.4.0.apk"
$checksumFile = Join-Path $packageDirectory "SHA256SUMS.txt"

New-Item -ItemType Directory -Force -Path $packageDirectory | Out-Null
$windowsItems = Get-ChildItem -LiteralPath $windowsSource | Where-Object { $_.Name -notlike "release-smoke*" }
Compress-Archive -Path $windowsItems.FullName -DestinationPath $windowsArchive -CompressionLevel Optimal -Force
Compress-Archive -Path $webglSource -DestinationPath $webglArchive -CompressionLevel Optimal -Force
Copy-Item -LiteralPath $androidSource -Destination $androidPackage -Force

$lines = @()
foreach ($file in @($windowsArchive, $webglArchive, $androidPackage)) {
    $hash = Get-FileHash -LiteralPath $file -Algorithm SHA256
    $lines += "$($hash.Hash.ToLowerInvariant())  $([System.IO.Path]::GetFileName($file))"
}
[System.IO.File]::WriteAllLines($checksumFile, $lines, [System.Text.UTF8Encoding]::new($false))

Get-ChildItem -LiteralPath $packageDirectory -File | Select-Object Name, Length
Get-Content -LiteralPath $checksumFile

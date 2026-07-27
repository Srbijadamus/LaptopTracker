schtasks /End /TN "LaptopTracker-App"
Start-Sleep 3
Get-Process dotnet -EA SilentlyContinue | Where-Object {
  ($_.Modules | Where-Object ModuleName -eq 'LaptopTracker.dll')
} | Stop-Process -Force -EA SilentlyContinue
Start-Sleep 2
Set-Location "C:\LaptopTracker\LaptopTracker"
dotnet publish -c Release -o "C:\LaptopTracker\publish"
if ($LASTEXITCODE -ne 0) { Write-Host "BUILD FAILED - not restarting"; exit 1 }
schtasks /Run /TN "LaptopTracker-App"
Start-Sleep 6
Write-Host "Live DLL:" (Get-Item "C:\LaptopTracker\publish\LaptopTracker.dll").LastWriteTime

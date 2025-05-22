# DueTime validation script

Write-Host "===== DueTime Validation =====" -ForegroundColor Cyan
Write-Host "Running code format check..." -ForegroundColor Yellow
dotnet format --verify-no-changes

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Code formatting issues detected. Please run 'dotnet format' to fix." -ForegroundColor Red
    exit 1
}
else {
    Write-Host "✅ Code formatting verified" -ForegroundColor Green
}

Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build -c Release /warnaserror

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed" -ForegroundColor Red
    exit 1
}
else {
    Write-Host "✅ Build successful" -ForegroundColor Green
}

Write-Host "Running tests..." -ForegroundColor Yellow
dotnet test --no-build -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Tests failed" -ForegroundColor Red
    exit 1
}
else {
    Write-Host "✅ All tests passed" -ForegroundColor Green
}

Write-Host "===== Validation Complete =====" -ForegroundColor Cyan
Write-Host "DueTime is ready for deployment!" -ForegroundColor Green 
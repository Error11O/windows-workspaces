param(
    [string]$OutputPath = (Join-Path $PSScriptRoot "artifacts")
)

$ErrorActionPreference = "Stop"

$executable = [IO.Path]::GetFullPath((Join-Path $OutputPath "workspaces.exe"))
$runningInstances = @(
    Get-Process -Name "workspaces" -ErrorAction SilentlyContinue |
        Where-Object {
            try {
                [IO.Path]::GetFullPath($_.Path) -ieq $executable
            }
            catch {
                $false
            }
        }
)

foreach ($instance in $runningInstances) {
    Write-Host "Stopping running publish output (PID $($instance.Id))..."
    Stop-Process -Id $instance.Id -Force
    Wait-Process -Id $instance.Id -Timeout 10 -ErrorAction SilentlyContinue
}

dotnet publish (Join-Path $PSScriptRoot "workspaces.csproj") `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $OutputPath `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if (-not (Test-Path -LiteralPath $executable)) {
    throw "Publish completed without workspaces.exe."
}

Write-Host "Created $executable"

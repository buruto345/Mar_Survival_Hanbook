param([switch]$UseExistingSimulation)
$ErrorActionPreference = 'Stop'
$discoveryRoot = Split-Path -Parent $PSScriptRoot
$previousSql = $env:MARS_RUN_SQLSERVER_TESTS
$previousSimulation = $env:MARS_RUN_NASA_SIMULATION
$simulationProcess = $null
Push-Location $discoveryRoot
try {
    if (!$UseExistingSimulation) {
        if (Get-NetTCPConnection -LocalPort 4010 -State Listen -ErrorAction SilentlyContinue) {
            throw 'Port 4010 is occupied. Stop its owner or explicitly use -UseExistingSimulation for this repository mock.'
        }
        $reportDir = Join-Path $discoveryRoot 'Backend/Mars_Survival_Hanbook.Tests/TestResults'
        New-Item -ItemType Directory -Force $reportDir | Out-Null
        $simulationProcess = Start-Process -FilePath (Get-Command postman -ErrorAction Stop).Source -WindowStyle Hidden -PassThru -WorkingDirectory $discoveryRoot -ArgumentList 'simulation run postman/simulations/nasa.sim.yaml --port 4010 --no-history --output ndjson' -RedirectStandardOutput (Join-Path $reportDir 'simulation.ndjson') -RedirectStandardError (Join-Path $reportDir 'simulation.err')
    }
    $ready = $false
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        try { $null = Invoke-RestMethod 'http://127.0.0.1:4010/nasa/requests' -TimeoutSec 1; $ready = $true; break } catch { Start-Sleep -Milliseconds 500 }
    }
    if (!$ready) { throw 'NASA simulation did not become ready.' }
    $env:MARS_RUN_SQLSERVER_TESTS = '1'
    $env:MARS_RUN_NASA_SIMULATION = '1'
    dotnet test Backend/Mars_Survival_Hanbook.Tests --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=discovery.trx'
    if ($LASTEXITCODE -ne 0) { throw 'Discover Mars tests failed.' }
    dotnet build Backend/Mars_Survival_Hanbook --no-restore -warnaserror
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    dotnet ef migrations has-pending-model-changes --project Backend/Mars_Survival_Hanbook --no-build
    if ($LASTEXITCODE -ne 0) { throw 'EF model differs from migration.' }
} finally {
    if ($simulationProcess) {
        # Only stop the process tree created by this script.
        function Stop-OwnedProcessTree([int]$RootProcessId) {
            foreach ($child in @(Get-CimInstance Win32_Process -Filter "ParentProcessId = $RootProcessId" -ErrorAction SilentlyContinue)) {
                Stop-OwnedProcessTree $child.ProcessId
            }
            Stop-Process -Id $RootProcessId -Force -ErrorAction SilentlyContinue
        }
        Stop-OwnedProcessTree $simulationProcess.Id
    }
    $env:MARS_RUN_SQLSERVER_TESTS = $previousSql
    $env:MARS_RUN_NASA_SIMULATION = $previousSimulation
    Pop-Location
}


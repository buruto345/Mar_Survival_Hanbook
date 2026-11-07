param([string]$BaseUrl = 'http://127.0.0.1:5086')
$ErrorActionPreference = 'Stop'
$discoveryRoot = Split-Path -Parent $PSScriptRoot
Push-Location $discoveryRoot
try {
    New-Item -ItemType Directory -Force postman/specs | Out-Null
    Invoke-WebRequest -UseBasicParsing "$BaseUrl/swagger/v1/swagger.json" -OutFile postman/specs/discover-mars.openapi.json
    postman spec lint postman/specs/discover-mars.openapi.json --issueType syntax --no-report-events
    if ($LASTEXITCODE -ne 0) { throw 'OpenAPI lint failed.' }
    $generatedRoot = [IO.Path]::GetFullPath((Join-Path $discoveryRoot 'postman/collections/Discover Mars API'))
    if (!$generatedRoot.StartsWith([IO.Path]::GetFullPath($discoveryRoot) + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe collection path.' }
    if (Test-Path -LiteralPath $generatedRoot) { Remove-Item -LiteralPath $generatedRoot -Recurse -Force }
    postman spec generate collection postman/specs/discover-mars.openapi.json -n 'Discover Mars API' --folder-strategy Tags --force
    if ($LASTEXITCODE -ne 0) { throw 'Collection generation failed.' }
    # Generated random payload examples are scaffolding, not verified API responses.
    $collectionRoot = (Resolve-Path 'postman/collections/Discover Mars API').Path
    $resourceDirs = @(Get-ChildItem -LiteralPath $collectionRoot -Recurse -Directory | Where-Object { $_.Name.EndsWith('.resources') -and $_.Name -ne '.resources' })
    foreach ($directory in $resourceDirs) {
        $resolved = [IO.Path]::GetFullPath($directory.FullName)
        if (!$resolved.StartsWith($collectionRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe generated directory.' }
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
    foreach ($request in Get-ChildItem -LiteralPath $collectionRoot -Recurse -Filter '*.request.yaml') {
        $content = [IO.File]::ReadAllText($request.FullName) -replace '(?m)^examples:.*\r?\n', ''
        [IO.File]::WriteAllText($request.FullName, $content, (New-Object Text.UTF8Encoding $false))
    }
    $definitionPath = Join-Path $collectionRoot '.resources/definition.yaml'
    $definition = [IO.File]::ReadAllText($definitionPath).Replace('baseUrl: /', "baseUrl: '$BaseUrl'")
    [IO.File]::WriteAllText($definitionPath, $definition, (New-Object Text.UTF8Encoding $false))
    postman collection lint $collectionRoot
    if ($LASTEXITCODE -ne 0) { throw 'Generated collection lint failed.' }
} finally { Pop-Location }


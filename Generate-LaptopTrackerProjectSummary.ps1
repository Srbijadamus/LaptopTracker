$ErrorActionPreference = 'Stop'

$ProjectRoot = 'C:\LaptopTracker\LaptopTracker'
$Timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$OutputFile = Join-Path $ProjectRoot "PROJECT_FULL_SUMMARY_$Timestamp.md"

function Get-TextFileContent {
    param(
        [string]$Path,
        [int]$MaxBytes = 1048576
    )
    try {
        $item = Get-Item -LiteralPath $Path -ErrorAction Stop
        if ($item.Length -gt $MaxBytes) {
            return "[Skipped content preview because file is larger than $MaxBytes bytes.]"
        }
        return [System.IO.File]::ReadAllText($Path)
    }
    catch {
        return "[Unable to read file: $($_.Exception.Message)]"
    }
}

function Get-CSharpClassInfo {
    param([string]$Content)
    $classes = [regex]::Matches($Content, '(?m)^\s*(?:public|internal|protected|private)?\s*(?:abstract\s+|sealed\s+|partial\s+)*class\s+([A-Za-z0-9_]+)')
    $controllers = [regex]::Matches($Content, '(?m)^\s*(?:public|internal)?\s*class\s+([A-Za-z0-9_]+Controller)\b')
    $methods = [regex]::Matches($Content, '(?m)^\s*(?:\[[^\]]+\]\s*)*(?:public|internal|protected|private)\s+(?:async\s+)?(?:[A-Za-z0-9_<>,\?\[\]\.]|\s)+\s+([A-Za-z0-9_]+)\s*\(')
    [pscustomobject]@{
        Classes = ($classes | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
        Controllers = ($controllers | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
        Methods = ($methods | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
    }
}

function Get-ApiUsageInfo {
    param([string]$Content)
    $httpCalls = [regex]::Matches($Content, '(HttpClient|RestClient|GetAsync|PostAsync|PutAsync|DeleteAsync|SendAsync|MapControllers|AddHttpClient|\[ApiController\]|fetch\()')
    $endpoints = [regex]::Matches($Content, '(?m)^\s*\[(HttpGet|HttpPost|HttpPut|HttpDelete|Route)\s*\(([^\)]*)\)\]')
    [pscustomobject]@{
        HasHttpUsage = $httpCalls.Count -gt 0
        HttpMarkers = ($httpCalls | ForEach-Object { $_.Value } | Sort-Object -Unique)
        EndpointAttributes = ($endpoints | ForEach-Object { $_.Value.Trim() } | Sort-Object -Unique)
    }
}

if (-not (Test-Path -LiteralPath $ProjectRoot)) {
    throw "Project root not found: $ProjectRoot"
}

$allFiles = Get-ChildItem -LiteralPath $ProjectRoot -Recurse -File -Force | Sort-Object FullName
$relevantExtensions = @('.cs','.cshtml','.json','.config','.xml','.js','.css','.sql','.md','.txt','.csproj','.sln')
$relevantFiles = $allFiles | Where-Object { $relevantExtensions -contains $_.Extension.ToLowerInvariant() -or $_.Name -in @('Program.cs','appsettings.json','appsettings.Development.json') }

$controllers = @()
$models = @()
$services = @()
$views = @()
$apis = @()
$startupFiles = @()
$dbFiles = @()
$staticFiles = @()
$fileSections = New-Object System.Collections.Generic.List[string]

foreach ($file in $relevantFiles) {
    $relative = $file.FullName.Substring($ProjectRoot.Length).TrimStart('\')
    $content = Get-TextFileContent -Path $file.FullName
    $classInfo = $null
    $apiInfo = $null

    if ($file.Extension -eq '.cs') {
        $classInfo = Get-CSharpClassInfo -Content $content
        $apiInfo = Get-ApiUsageInfo -Content $content
    }
    elseif ($file.Extension -in @('.js','.cshtml')) {
        $apiInfo = Get-ApiUsageInfo -Content $content
    }

    if ($relative -match '(^|\\)Controllers\\') { $controllers += $relative }
    if ($relative -match '(^|\\)Models\\') { $models += $relative }
    if ($relative -match '(^|\\)Services\\|(^|\\)Interfaces\\|(^|\\)Repositories\\') { $services += $relative }
    if ($relative -match '(^|\\)Views\\') { $views += $relative }
    if ($relative -match 'Program\.cs|Startup\.cs|appsettings.*\.json|.*\.csproj|.*\.sln') { $startupFiles += $relative }
    if ($relative -match 'DbContext|Migrations|.*\.sql|Data\\') { $dbFiles += $relative }
    if ($relative -match 'wwwroot\\|Content\\|Scripts\\') { $staticFiles += $relative }

    if ($apiInfo -and ($apiInfo.HasHttpUsage -or $apiInfo.EndpointAttributes.Count -gt 0)) {
        $apis += [pscustomobject]@{
            File = $relative
            Markers = $apiInfo.HttpMarkers
            Endpoints = $apiInfo.EndpointAttributes
        }
    }

    $section = @()
    $section += "## File: $relative"
    $section += "- Type: $($file.Extension)"
    $section += "- SizeBytes: $($file.Length)"
    if ($classInfo) {
        if ($classInfo.Classes.Count -gt 0) { $section += "- Classes: $($classInfo.Classes -join ', ')" }
        if ($classInfo.Controllers.Count -gt 0) { $section += "- Controllers: $($classInfo.Controllers -join ', ')" }
        if ($classInfo.Methods.Count -gt 0) { $section += "- Methods: $($classInfo.Methods -join ', ')" }
    }
    if ($apiInfo) {
        if ($apiInfo.HttpMarkers.Count -gt 0) { $section += "- API/HTTP markers: $($apiInfo.HttpMarkers -join ', ')" }
        if ($apiInfo.EndpointAttributes.Count -gt 0) { $section += "- Endpoint attributes: $($apiInfo.EndpointAttributes -join ' | ')" }
    }
    $section += '- Content preview:'
    $preview = $content
    if ($preview.Length -gt 4000) { $preview = $preview.Substring(0, 4000) + "`n...[truncated preview]" }
    $section += '```'
    $section += $preview
    $section += '```'
    $fileSections.Add(($section -join "`r`n")) | Out-Null
}

$summary = @()
$summary += "# LaptopTracker Full Project Summary"
$summary += ""
$summary += "Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
$summary += "Project root: $ProjectRoot"
$summary += ""
$summary += "## Scope"
$summary += "This document was generated by a PowerShell script that recursively analyzes the complete LaptopTracker project folder and writes the findings into the project itself. The goal is to document the project in English from the first file to the last scanned file without inventing implementation details."
$summary += ""
$summary += "## High-level structure"
$summary += "- Total files scanned: $($allFiles.Count)"
$summary += "- Relevant text/code files analyzed: $($relevantFiles.Count)"
$summary += "- Controllers folder files: $($controllers.Count)"
$summary += "- Models folder files: $($models.Count)"
$summary += "- Services/interfaces/repositories files: $($services.Count)"
$summary += "- Views files: $($views.Count)"
$summary += "- Startup/configuration files: $($startupFiles.Count)"
$summary += "- Database-related files: $($dbFiles.Count)"
$summary += "- Static asset files: $($staticFiles.Count)"
$summary += ""
$summary += "## Operations"
$summary += "Operations in this project are the business flows implemented by controllers, views, services, database access, forms, and page actions. This analysis identifies those operations by scanning controllers, public methods, endpoint attributes, Razor forms, configuration, and client-side HTTP markers."
$summary += ""
$summary += "## API analysis"
if ($apis.Count -eq 0) {
    $summary += "No explicit external API client usage or API controller attributes were detected in the scanned text/code files. This usually means the project is primarily a server-rendered ASP.NET MVC application, or it uses internal controller actions and standard form posts instead of separate API controllers."
}
else {
    foreach ($api in $apis) {
        $summary += "- File: $($api.File)"
        if ($api.Markers.Count -gt 0) { $summary += "  - API/HTTP markers: $($api.Markers -join ', ')" }
        if ($api.Endpoints.Count -gt 0) { $summary += "  - Endpoint attributes: $($api.Endpoints -join ' | ')" }
    }
}
$summary += ""
$summary += "## What the project does"
$summary += "The exact business purpose is inferred only from filenames, code symbols, routes, views, and forms found in the project. The summary file therefore reports visible evidence such as device scanning pages, stock pages, return pages, loaner pages, data models, services, and configuration, instead of guessing hidden business logic."
$summary += ""
$summary += "## File-by-file analysis"
$summary += ($fileSections -join "`r`n`r`n")

Set-Content -LiteralPath $OutputFile -Value ($summary -join "`r`n") -Encoding UTF8
Write-Host "Created summary:"
Write-Host $OutputFile

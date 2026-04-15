<#
.SYNOPSIS
  Deletes folders under results\ except the names you keep (default: best, current).

.DESCRIPTION
  Use with a stable workflow:
  - Train with:  .\Start-MLSmoke.ps1 -RunId current
  - When a run is your new best: .\Promote-MlRun.ps1 -SourceRunId current   (or copy a timestamped run into best first)
  - Prune old runs: .\Prune-MlResults.ps1

.EXAMPLE
  .\Prune-MlResults.ps1 -Keep best,current -WhatIf
  .\Prune-MlResults.ps1 -Keep best,current,actor-20260407-012857
#>
param(
    [string[]]$Keep = @('best', 'current'),
    [switch]$WhatIf
)

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$results = Join-Path $repoRoot "results"

if (-not (Test-Path $results)) {
    Write-Host "No results directory at $results"
    exit 0
}

$keepSet = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($n in $Keep) {
    if (-not [string]::IsNullOrWhiteSpace($n)) {
        [void]$keepSet.Add($n.Trim())
    }
}

Get-ChildItem -Path $results -Directory -ErrorAction SilentlyContinue | ForEach-Object {
    if ($keepSet.Contains($_.Name)) {
        return
    }

    Write-Host "Remove: $($_.FullName)"
    if (-not $WhatIf) {
        Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction Stop
    }
}

if ($WhatIf) {
    Write-Host "(WhatIf: no folders were deleted.)" -ForegroundColor Yellow
}
else {
    Write-Host "Done. Kept: $($Keep -join ', ')" -ForegroundColor Green
}

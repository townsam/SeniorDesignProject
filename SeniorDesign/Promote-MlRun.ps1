<#
.SYNOPSIS
  Copy results\<SourceRunId> to results\best (replaces previous best folder).

.EXAMPLE
  .\Promote-MlRun.ps1 -SourceRunId current
  .\Promote-MlRun.ps1 -SourceRunId actor-20260407-012857
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$SourceRunId
)

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $repoRoot "results\$SourceRunId"
$dst = Join-Path $repoRoot "results\best"

if (-not (Test-Path $src)) {
    Write-Error "Source run not found: $src"
    exit 1
}

if (Test-Path $dst) {
    Write-Host "Replacing existing results\best"
    Remove-Item -LiteralPath $dst -Recurse -Force
}

Write-Host "Copying $src -> $dst"
Copy-Item -LiteralPath $src -Destination $dst -Recurse -Force
Write-Host "Done. TensorBoard (single run): tensorboard --logdir `"$dst`"" -ForegroundColor Green

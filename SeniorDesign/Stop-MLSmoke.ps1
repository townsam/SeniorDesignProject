param(
    [string]$RunId = "",
    [int]$TensorBoardPort = 6006,
    [switch]$DryRun
)

function Get-MatchingProcesses {
    param(
        [string[]]$Patterns
    )

    $all = Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -ne $null }
    foreach ($proc in $all) {
        foreach ($pattern in $Patterns) {
            if ($proc.CommandLine -like "*$pattern*") {
                [PSCustomObject]@{
                    ProcessId   = $proc.ProcessId
                    Name        = $proc.Name
                    CommandLine = $proc.CommandLine
                }
                break
            }
        }
    }
}

function Get-ProcessByListeningPort {
    param(
        [int]$Port
    )

    if ($Port -le 0) {
        return @()
    }

    try {
        $listeners = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction Stop |
            Select-Object -ExpandProperty OwningProcess -Unique

        if (-not $listeners) {
            return @()
        }

        $all = Get-CimInstance Win32_Process
        return $all |
            Where-Object { $listeners -contains $_.ProcessId } |
            ForEach-Object {
                [PSCustomObject]@{
                    ProcessId   = $_.ProcessId
                    Name        = $_.Name
                    CommandLine = $_.CommandLine
                }
            }
    }
    catch {
        return @()
    }
}

$patterns = @(
    "mlagents.trainers.learn",
    "tensorboard.main",
    "-m tensorboard",
    "tensorboard --logdir",
    "\\tensorboard.exe"
)

if ($RunId -ne "") {
    $patterns += "--run-id '$RunId'"
    $patterns += "--run-id $RunId"
}

if ($TensorBoardPort -gt 0) {
    $patterns += "--port $TensorBoardPort"
}

$patternMatches = Get-MatchingProcesses -Patterns $patterns
$portMatches = Get-ProcessByListeningPort -Port $TensorBoardPort

$matches = @($patternMatches + $portMatches) |
    Sort-Object ProcessId -Unique

if (-not $matches -or $matches.Count -eq 0) {
    Write-Host "No matching ML smoke/training processes found."
    exit 0
}

Write-Host "Found the following matching processes:"
$matches | ForEach-Object {
    Write-Host "- PID $($_.ProcessId) [$($_.Name)]"
}

if ($DryRun) {
    Write-Host "DryRun enabled. No processes were stopped."
    exit 0
}

foreach ($proc in $matches) {
    try {
        Stop-Process -Id $proc.ProcessId -Force -ErrorAction Stop
        Write-Host "Stopped PID $($proc.ProcessId) [$($proc.Name)]"
    }
    catch {
        Write-Warning "Failed to stop PID $($proc.ProcessId): $($_.Exception.Message)"
    }
}

Write-Host "Done."

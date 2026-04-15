param(
    # Tip: use -RunId current for day-to-day runs, then Prune-MlResults.ps1 (keeps best + current).
    [string]$RunId = "actor-smoke",
    [int]$TimeScale = 10,
    [int]$TensorBoardPort = 6006,
    # Relative to repo root. Default "results" lists every run folder; use "results\<your-run-id>" to show only one run.
    [string]$TensorBoardLogDir = "results",
    [switch]$Resume,
    [switch]$NoOverwrite
)

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonExe = Join-Path $repoRoot ".venv-mlagents\Scripts\python.exe"
$trainerConfig = Join-Path $repoRoot "Assets\ML-Agents\actor_ppo.yaml"
$resultsDir = Join-Path $repoRoot "results"

if (-not (Test-Path $pythonExe)) {
    Write-Error "Python executable not found at '$pythonExe'. Run .\Setup-MLAgentsVenv.ps1 once to create the venv and install pins (torch<2.9, etc.)."
    exit 1
}

if (-not (Test-Path $trainerConfig)) {
    Write-Error "Trainer config not found at '$trainerConfig'."
    exit 1
}

if (-not (Test-Path $resultsDir)) {
    New-Item -ItemType Directory -Path $resultsDir | Out-Null
}

$trainerCommand = "Set-Location '$repoRoot'; & '$pythonExe' -m mlagents.trainers.learn 'Assets/ML-Agents/actor_ppo.yaml' --run-id '$RunId' --time-scale $TimeScale"
if ($Resume) {
    $trainerCommand += " --resume"
}
elseif (-not $NoOverwrite) {
    $trainerCommand += " --force"
}

$tensorBoardResolved = Join-Path $repoRoot $TensorBoardLogDir
$tensorBoardCommand = "Set-Location '$repoRoot'; & '$pythonExe' -m tensorboard.main --logdir `"$tensorBoardResolved`" --port $TensorBoardPort"

Write-Host ""
Write-Host "=== ML-Agents smoke ===" -ForegroundColor Cyan
if ($Resume) {
    Write-Host "Mode: --resume (continuing existing results\$RunId)" -ForegroundColor Magenta
}
elseif ($NoOverwrite) {
    Write-Host "Mode: no --force (will error if results\$RunId already exists)" -ForegroundColor Magenta
}
else {
    Write-Host "Mode: --force (overwrites previous results\$RunId if present)" -ForegroundColor DarkGray
}
Write-Host "Opening TWO extra PowerShell windows: (1) TRAINER  (2) TensorBoard" -ForegroundColor Yellow
Write-Host "Watch window (1) for 'Connected to Unity' and rising step counts." -ForegroundColor Yellow
Write-Host ""

Write-Host "Starting ML-Agents smoke trainer..."
Start-Process -FilePath "powershell.exe" -ArgumentList @(
    "-NoExit",
    "-ExecutionPolicy", "Bypass",
    "-Command", $trainerCommand
)

Start-Sleep -Seconds 1

Write-Host "Starting TensorBoard..."
Start-Process -FilePath "powershell.exe" -ArgumentList @(
    "-NoExit",
    "-ExecutionPolicy", "Bypass",
    "-Command", $tensorBoardCommand
)

Start-Sleep -Seconds 2
Start-Process "http://localhost:$TensorBoardPort/" | Out-Null

Write-Host ""
Write-Host "TensorBoard: http://localhost:$TensorBoardPort/" -ForegroundColor Green
Write-Host "  logdir: $tensorBoardResolved" -ForegroundColor DarkGray
Write-Host "  Curves need ~500+ steps; use left sidebar run checkboxes (name = folder under results\). Match to Run ID: $RunId -> results\$RunId\" -ForegroundColor DarkGray
Write-Host ""
Write-Host "In Unity: open a training scene, press Play, click Start (Simulation)." -ForegroundColor Green
Write-Host "Actor Behavior Type must be Default (not Heuristic Only) for learning." -ForegroundColor Green
Write-Host "Disk: Promote-MlRun.ps1 copies a run -> results\best; Prune-MlResults.ps1 deletes other runs (keeps best,current)." -ForegroundColor DarkGray
Write-Host ""

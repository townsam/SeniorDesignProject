$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$venvPython = Join-Path $repoRoot ".venv-mlagents\Scripts\python.exe"
$venvPip = Join-Path $repoRoot ".venv-mlagents\Scripts\pip.exe"
$req = Join-Path $repoRoot "requirements-mlagents.txt"

if (-not (Test-Path $req)) {
    Write-Error "Missing requirements file: $req"
    exit 1
}

if (-not (Test-Path $venvPython)) {
    Write-Host "Creating venv at .venv-mlagents ..."
    python -m venv (Join-Path $repoRoot ".venv-mlagents")
}

Write-Host "Upgrading pip and installing ML-Agents stack (torch<2.9 for stable ONNX export)..."
& $venvPip install --upgrade pip
& $venvPip install -r $req

Write-Host ""
Write-Host "Done. Use Start-MLSmoke.ps1 or: .venv-mlagents\Scripts\python.exe -m mlagents.trainers.learn ..."
Write-Host "Stack pins torch<2.9 for ONNX checkpoints. If you upgrade PyTorch to 2.9+, run:"
Write-Host "  .venv-mlagents\Scripts\python.exe tools\patch_mlagents_onnx_dynamo_false.py"
Write-Host ""

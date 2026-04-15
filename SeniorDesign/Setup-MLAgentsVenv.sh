#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "$0")" && pwd)"
venv_dir="$repo_root/.venv-mlagents"
req="$repo_root/requirements-mlagents.txt"

if [[ ! -f "$req" ]]; then
  echo "Missing requirements file: $req" >&2
  exit 1
fi

pick_python() {
  if [[ -n "${PYTHON:-}" ]]; then
    echo "$PYTHON"
    return 0
  fi
  if command -v python3.10 >/dev/null 2>&1; then
    echo "python3.10"
    return 0
  fi
  if command -v python3 >/dev/null 2>&1; then
    echo "python3"
    return 0
  fi
  echo "python3"
}

python_bin="$(pick_python)"
if ! command -v "$python_bin" >/dev/null 2>&1; then
  echo "Python not found: $python_bin." >&2
  echo "Install Python 3.10 and rerun (Homebrew: brew install python@3.10), or set PYTHON=python3.10." >&2
  exit 1
fi

py_ver="$("$python_bin" -c 'import sys; print(f"{sys.version_info.major}.{sys.version_info.minor}.{sys.version_info.micro}")')"
py_mm="$("$python_bin" -c 'import sys; print(f"{sys.version_info.major}.{sys.version_info.minor}")')"
py_micro="$("$python_bin" -c 'import sys; print(sys.version_info.micro)')"
if [[ "$py_mm" != "3.10" || "$py_micro" -gt 12 ]]; then
  echo "This project pins ML-Agents 1.1.0, which requires Python 3.10.1–3.10.12." >&2
  echo "Selected interpreter: $python_bin ($py_ver)" >&2
  echo "" >&2
  echo "Fix:" >&2
  echo "  - Install Python 3.10.12 specifically (recommended: pyenv):" >&2
  echo "      pyenv install 3.10.12" >&2
  echo "      pyenv local 3.10.12" >&2
  echo "  - Rerun with: PYTHON=python3.10 ./Setup-MLAgentsVenv.sh" >&2
  echo "" >&2
  echo "If you already created .venv-mlagents with the wrong Python, delete it first:" >&2
  echo "  rm -rf .venv-mlagents" >&2
  exit 1
fi

if [[ ! -x "$venv_dir/bin/python" ]]; then
  echo "Creating venv at .venv-mlagents ..."
  "$python_bin" -m venv "$venv_dir"
else
  venv_ver="$("$venv_dir/bin/python" -c 'import sys; print(f"{sys.version_info.major}.{sys.version_info.minor}.{sys.version_info.micro}")' 2>/dev/null || true)"
  venv_mm="$("$venv_dir/bin/python" -c 'import sys; print(f"{sys.version_info.major}.{sys.version_info.minor}")' 2>/dev/null || true)"
  venv_micro="$("$venv_dir/bin/python" -c 'import sys; print(sys.version_info.micro)' 2>/dev/null || true)"
  if [[ "$venv_mm" != "3.10" || -z "$venv_micro" || "$venv_micro" -gt 12 ]]; then
    echo "Existing venv at .venv-mlagents is not Python 3.10.1–3.10.12 (found $venv_ver)." >&2
    echo "Delete it and recreate with Python 3.10:" >&2
    echo "  rm -rf .venv-mlagents && PYTHON=python3.10 ./Setup-MLAgentsVenv.sh" >&2
    exit 1
  fi
fi

echo "Upgrading pip and installing ML-Agents stack (torch<2.9 for stable ONNX export)..."
"$venv_dir/bin/pip" install --upgrade pip

"$venv_dir/bin/pip" install "setuptools<81" wheel

uname_s="$(uname -s || true)"
if [[ "$uname_s" == "Darwin" ]]; then
  echo "macOS detected: installing grpcio from wheel (avoids grpcio<=1.48.2 source build)."
  "$venv_dir/bin/pip" install "grpcio>=1.62,<2"

  echo "Installing core pinned deps..."
  "$venv_dir/bin/pip" install --no-build-isolation -r "$req" --no-deps

  echo "Installing ML-Agents deps (excluding grpcio pin)..."
  "$venv_dir/bin/pip" install \
    "mlagents-envs==1.1.0" --no-deps

  "$venv_dir/bin/pip" install \
    "h5py>=2.9.0" \
    "Pillow>=4.2.1" \
    "pyyaml>=3.1.0" \
    "tensorboard>=2.14" \
    "six>=1.16" \
    "attrs>=19.3.0" \
    "huggingface-hub>=0.14" \
    "cattrs>=1.1.0,<1.7" \
    "cloudpickle" \
    "gym>=0.21.0" \
    "pettingzoo==1.15.0" \
    "filelock>=3.4.0"

  echo "Installing ML-Agents trainer..."
  "$venv_dir/bin/pip" install "mlagents==1.1.0" --no-deps
else
  # Default path (Windows/Linux): keep the fully pinned + resolver-driven install.
  "$venv_dir/bin/pip" install --no-build-isolation -r "$req"
fi

echo ""
echo "Done."
echo "Trainer: $venv_dir/bin/python -m mlagents.trainers.learn Assets/ML-Agents/actor_ppo.yaml --run-id actor-smoke --time-scale 10"
echo "TensorBoard: $venv_dir/bin/python -m tensorboard.main --logdir results --port 6006"
echo ""
echo "If you intentionally upgrade PyTorch to 2.9+, run:"
echo "  $venv_dir/bin/python tools/patch_mlagents_onnx_dynamo_false.py"
echo ""

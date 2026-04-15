#!/usr/bin/env bash
set -euo pipefail

run_id="${RUN_ID:-actor-smoke}"
time_scale="${TIME_SCALE:-10}"
tb_port="${TENSORBOARD_PORT:-6006}"
tb_logdir="${TENSORBOARD_LOGDIR:-results}"
mode="${MODE:-force}" # force|resume|no_overwrite

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
python_exe="${repo_root}/.venv-mlagents/bin/python"
trainer_config="${repo_root}/Assets/ML-Agents/actor_ppo.yaml"

if [[ ! -x "${python_exe}" ]]; then
  echo "Python executable not found at '${python_exe}'." >&2
  echo "Run ./Setup-MLAgentsVenv.sh once to create the venv and install pins (torch<2.9, etc.)." >&2
  exit 1
fi

if [[ ! -f "${trainer_config}" ]]; then
  echo "Trainer config not found at '${trainer_config}'." >&2
  exit 1
fi

mkdir -p "${repo_root}/results"

trainer_args=( -m mlagents.trainers.learn "Assets/ML-Agents/actor_ppo.yaml" --run-id "${run_id}" --time-scale "${time_scale}" )
case "${mode}" in
  resume) trainer_args+=( --resume ) ;;
  no_overwrite) ;;
  force|*) trainer_args+=( --force ) ;;
esac

tb_resolved="${repo_root}/${tb_logdir}"
tb_args=( -m tensorboard.main --logdir "${tb_resolved}" --port "${tb_port}" )

echo ""
echo "=== ML-Agents smoke (macOS) ==="
echo "Run ID: ${run_id}"
echo "Mode: ${mode}"
echo "Trainer: ${python_exe} ${trainer_args[*]}"
echo "TensorBoard: http://localhost:${tb_port}/ (logdir: ${tb_resolved})"
echo ""
echo "Starting trainer + TensorBoard in background..."

(
  cd "${repo_root}"
  "${python_exe}" "${trainer_args[@]}"
) >"${repo_root}/Logs/ml-trainer-${run_id}.log" 2>&1 &
trainer_pid=$!

(
  cd "${repo_root}"
  "${python_exe}" "${tb_args[@]}"
) >"${repo_root}/Logs/tensorboard-${run_id}.log" 2>&1 &
tb_pid=$!

echo "${trainer_pid}" >"${repo_root}/Logs/ml-trainer-${run_id}.pid"
echo "${tb_pid}" >"${repo_root}/Logs/tensorboard-${run_id}.pid"

echo "PIDs: trainer=${trainer_pid}, tensorboard=${tb_pid}"
echo "Logs:"
echo "  ${repo_root}/Logs/ml-trainer-${run_id}.log"
echo "  ${repo_root}/Logs/tensorboard-${run_id}.log"
echo ""
echo "In Unity: press Play, then Start/Simulate. Agents must be Behavior Type = Default for learning."
echo ""

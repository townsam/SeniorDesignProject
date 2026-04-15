#!/usr/bin/env bash
set -euo pipefail

run_id="${RUN_ID:-actor-smoke}"
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

trainer_pid_file="${repo_root}/Logs/ml-trainer-${run_id}.pid"
tb_pid_file="${repo_root}/Logs/tensorboard-${run_id}.pid"

stop_pid() {
  local pid="$1"
  if kill -0 "${pid}" >/dev/null 2>&1; then
    kill "${pid}" >/dev/null 2>&1 || true
    sleep 0.5
    if kill -0 "${pid}" >/dev/null 2>&1; then
      kill -9 "${pid}" >/dev/null 2>&1 || true
    fi
    echo "Stopped PID ${pid}"
  else
    echo "PID ${pid} not running"
  fi
}

echo ""
echo "Stopping ML smoke processes for run-id '${run_id}'..."

if [[ -f "${trainer_pid_file}" ]]; then
  stop_pid "$(cat "${trainer_pid_file}")"
  rm -f "${trainer_pid_file}"
else
  echo "No trainer pid file at ${trainer_pid_file}"
fi

if [[ -f "${tb_pid_file}" ]]; then
  stop_pid "$(cat "${tb_pid_file}")"
  rm -f "${tb_pid_file}"
else
  echo "No tensorboard pid file at ${tb_pid_file}"
fi

echo "Done."
echo ""

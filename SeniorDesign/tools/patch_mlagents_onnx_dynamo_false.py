"""
Only needed if you use PyTorch 2.9+ with mlagents 1.1.0.

This repo pins torch<2.9 in requirements-mlagents.txt so checkpoints export without
onnxscript. If you remove that cap, run this after pip upgrades:

  .venv-mlagents\\Scripts\\python.exe tools\\patch_mlagents_onnx_dynamo_false.py

It adds dynamo=False to torch.onnx.export in ml-agents (legacy TorchScript exporter).
"""
from __future__ import annotations

import sys
from pathlib import Path

MARKER = "dynamo=False,"
NEEDLE = "                dynamic_axes=self.dynamic_axes,\n            )"
REPLACEMENT = (
    "                dynamic_axes=self.dynamic_axes,\n"
    "                # PyTorch 2.9+ dynamo export needs onnxscript; ml-agents pins older onnx/protobuf.\n"
    "                dynamo=False,\n"
    "            )"
)


def main() -> int:
    root = Path(__file__).resolve().parents[1]
    rel = Path("Lib/site-packages/mlagents/trainers/torch_entities/model_serialization.py")
    candidates = [
        root / ".venv-mlagents" / rel,
        root / ".venv-mlagents" / "lib" / "site-packages" / "mlagents" / "trainers" / "torch_entities" / "model_serialization.py",
    ]
    path = next((p for p in candidates if p.is_file()), None)
    if path is None:
        print("Could not find model_serialization.py under .venv-mlagents", file=sys.stderr)
        return 1

    text = path.read_text(encoding="utf-8")
    if MARKER in text:
        print(f"Already patched: {path}")
        return 0

    if NEEDLE not in text:
        print(f"Unexpected file contents (needle missing): {path}", file=sys.stderr)
        return 1

    path.write_text(text.replace(NEEDLE, REPLACEMENT, 1), encoding="utf-8")
    print(f"Patched: {path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

#!/usr/bin/env bash
# Farenin gizli yaklasma politikasini egitir.
# Gerekenler: ML/.venv (Python 3.10, mlagents 1.1.0) ve RatTraining build'i.
#   uv venv -p 3.10 ML/.venv
#   uv pip install --python ML/.venv/bin/python --override ML/overrides.txt mlagents==1.1.0 matplotlib "setuptools<70"  # overrides: grpcio>=1.60, torch==2.2.2
set -euo pipefail
cd "$(dirname "$0")"

RUN_ID="${1:-rat_stealth}"
ENV_PATH="../YazLabUnity/Builds/RatTraining/RatTraining.app"

caffeinate -i .venv/bin/mlagents-learn config/rat_stealth.yaml \
  --env="$ENV_PATH" \
  --run-id="$RUN_ID" \
  --results-dir=results \
  --force

#!/bin/bash
_GSD_SHIM_NAME="gsd-tools.cjs"
_GSD_RUNTIME_ROOT="C:/Users/mehdi/Documents/PFA MotherSon/Motherson_Box_Management"
GSD_TOOLS="C:/Users/mehdi/.config/opencode/gsd-core/bin/${_GSD_SHIM_NAME}"
gsd_run() { node "$GSD_TOOLS" "$@"; }
INIT=$(gsd_run query docs-init)
if [[ "$INIT" == @file:* ]]; then INIT=$(cat "${INIT#@file:}"); fi
AGENT_SKILLS=$(gsd_run query agent-skills gsd-doc-writer)
echo "===INIT_START==="
echo "$INIT"
echo "===INIT_END==="
echo "===SKILLS_START==="
echo "$AGENT_SKILLS"
echo "===SKILLS_END==="

#!/bin/bash
# Shell Script to Run the FastAPI Service using the local .venv

VENV_NAME=".venv"
SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)

VENV_PATH="$SCRIPT_DIR/$VENV_NAME"
UVICORN_PATH="$VENV_PATH/bin/uvicorn"

echo "Project root: $SCRIPT_DIR"
echo "Attempting to use Uvicorn from virtual environment: $UVICORN_PATH"

if [ ! -f "$UVICORN_PATH" ]; then
    echo "WARNING: Uvicorn not found at $UVICORN_PATH."
    echo "Please ensure you have set up the virtual environment correctly using setup_venv.sh (or equivalent) and installed dependencies."
    echo "If the virtual environment is already active, you can also try running: uvicorn main:app --reload"
    echo "Attempting to use uvicorn from PATH..."
    UVICORN_PATH="uvicorn"
fi

echo "Starting FastAPI service (uvicorn main:app --reload)..."
echo "Service will run at http://127.0.0.1:8000. Press Ctrl+C to stop."

# Execute Uvicorn
"$UVICORN_PATH" main:app --reload

if [ $? -ne 0 ]; then
    echo "ERROR: Failed to start the service."
    echo "Please ensure the virtual environment is active, or '$UVICORN_PATH' is correct and executable."
    echo "You can try activating the virtual environment manually: source $VENV_PATH/bin/activate"
    echo "Then run: uvicorn main:app --reload"
fi 
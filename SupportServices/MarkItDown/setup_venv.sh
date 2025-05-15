#!/bin/bash
# Shell Script to Setup Python Virtual Environment and Install Dependencies

VENV_NAME=".venv"
REQUIREMENTS_FILE="requirements.txt"

# Get the directory where the script is located
SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)

# Construct full paths
VENV_PATH="$SCRIPT_DIR/$VENV_NAME"
REQUIREMENTS_PATH="$SCRIPT_DIR/$REQUIREMENTS_FILE"

echo "Project root: $SCRIPT_DIR"
echo "Virtual environment name: $VENV_NAME"
echo "Requirements file: $REQUIREMENTS_FILE"

# Check if requirements.txt exists
if [ ! -f "$REQUIREMENTS_PATH" ]; then
    echo "ERROR: Requirements file '$REQUIREMENTS_FILE' not found in the project root '$SCRIPT_DIR'."
    echo "Please ensure '$REQUIREMENTS_FILE' exists in the script's directory."
    exit 1
fi

# Check for Python 3 installation
if ! command -v python3 &> /dev/null; then
    echo "ERROR: python3 not found. Please ensure Python 3 is installed."
    echo "You can download Python from https://www.python.org/downloads/"
    exit 1
fi
PYTHON_EXECUTABLE=$(command -v python3)
echo "Found Python executable: $PYTHON_EXECUTABLE"

# Create virtual environment if it doesn't exist
if [ ! -d "$VENV_PATH/bin" ]; then
    echo "Creating virtual environment '$VENV_NAME'..."
    "$PYTHON_EXECUTABLE" -m venv "$VENV_PATH"
    if [ $? -ne 0 ]; then
        echo "ERROR: Failed to create virtual environment."
        exit 1
    fi
    echo "Virtual environment created successfully: $VENV_PATH"
else
    echo "Virtual environment '$VENV_NAME' already exists."
fi

# Install dependencies using pip from the virtual environment
PIP_EXECUTABLE="$VENV_PATH/bin/pip"

echo "Installing dependencies using pip from '$PIP_EXECUTABLE'..."
"$PIP_EXECUTABLE" install -r "$REQUIREMENTS_PATH"

if [ $? -ne 0 ]; then
    echo "ERROR: Failed to install dependencies. Please check the error messages."
    echo "You can try activating the virtual environment manually and running 'pip install -r $REQUIREMENTS_FILE'"
    echo "Manual activation command: source $VENV_PATH/bin/activate"
    exit 1
fi

echo ""
echo "---------------------------------------------------------------------"
echo "Python virtual environment setup complete and dependencies installed!"
echo ""
echo "To activate the virtual environment in this terminal session, run:"
echo "source $VENV_PATH/bin/activate"
echo ""
echo "Once activated, your shell prompt will typically show ($VENV_NAME)."
echo "Then you can run commands like 'python main.py' or 'uvicorn main:app --reload'."
echo "---------------------------------------------------------------------"

# The script ends here. The virtual environment will not remain active after this script exits
# unless the user sources the activate script as instructed. 
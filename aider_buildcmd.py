#!/usr/bin/env python3
"""
Simple build helper script for the DotnetEventBus repository.

Running this script will:

1. Restore NuGet packages.
2. Build the solution.
3. Run all unit tests.

It is intentionally lightweight and does not require any external
dependencies beyond the .NET SDK being installed and available on the
PATH.

Usage:
    python3 aider_buildcmd.py          # Run the full build pipeline
    python3 aider_buildcmd.py --test   # Only run tests
"""

import argparse
import subprocess
import sys
from pathlib import Path

# --------------------------------------------------------------------------- #
# Helper functions
# --------------------------------------------------------------------------- #
def run_cmd(command: list[str], cwd: Path | None = None) -> int:
    """Run a command and stream its output to the console."""
    try:
        result = subprocess.run(
            command,
            cwd=cwd,
            check=False,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
        )
        print(result.stdout)
        return result.returncode
    except FileNotFoundError:
        print(f"Error: command not found: {command[0]}")
        return 1


def restore_packages(root: Path) -> int:
    """Restore NuGet packages for the solution."""
    print("Restoring NuGet packages...")
    return run_cmd(["dotnet", "restore"], cwd=root)


def build_solution(root: Path) -> int:
    """Build the solution in Release configuration."""
    print("Building solution...")
    return run_cmd(["dotnet", "build", "--configuration", "Release", "--no-restore"], cwd=root)


def test_solution(root: Path) -> int:
    """Run all unit tests."""
    print("Running unit tests...")
    return run_cmd(["dotnet", "test", "--configuration", "Release", "--no-build", "--logger:trx"], cwd=root)


# --------------------------------------------------------------------------- #
# Main entry point
# --------------------------------------------------------------------------- #
def main() -> int:
    parser = argparse.ArgumentParser(description="DotnetEventBus build helper")
    parser.add_argument(
        "--test",
        action="store_true",
        help="Only run tests (skip restore and build)",
    )
    args = parser.parse_args()

    repo_root = Path(__file__).resolve().parent

    if not args.test:
        rc = restore_packages(repo_root)
        if rc != 0:
            return rc

        rc = build_solution(repo_root)
        if rc != 0:
            return rc

    rc = test_solution(repo_root)
    return rc


if __name__ == "__main__":
    sys.exit(main())

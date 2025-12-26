#!/bin/bash
# Bash script to run the database seeding script

echo "=== Database Seeding Script ==="
echo ""

# Change to project root
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$( cd "$SCRIPT_DIR/.." && pwd )"
cd "$PROJECT_ROOT"

# Run the seeding script
echo "Running seeding script..."
dotnet run --project taskedin-be.csproj -- seed

echo ""
echo "Seeding complete!"


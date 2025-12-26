#!/bin/bash
# Bash script to set up and trust the .NET development HTTPS certificate

echo "Setting up HTTPS development certificate..."

# Clean existing certificates
echo ""
echo "Cleaning existing certificates..."
dotnet dev-certs https --clean

# Generate and trust the certificate
echo ""
echo "Generating and trusting development certificate..."
dotnet dev-certs https --trust

if [ $? -eq 0 ]; then
    echo ""
    echo "✓ HTTPS certificate has been set up and trusted!"
    echo ""
    echo "You may need to restart your browser for the changes to take effect."
else
    echo ""
    echo "⚠ Certificate trust may require administrator/sudo privileges."
    echo "Please run: sudo dotnet dev-certs https --trust"
fi

echo ""
echo "Verifying certificate..."
dotnet dev-certs https --check

if [ $? -eq 0 ]; then
    echo ""
    echo "✓ Certificate is valid and trusted!"
else
    echo ""
    echo "⚠ Certificate verification failed. Please run the trust command manually."
fi


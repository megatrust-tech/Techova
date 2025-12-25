#!/bin/bash
# Bash script to generate secure JWT secrets
# Usage: ./scripts/generate-jwt-secret.sh

echo "Generating secure JWT secrets..."
echo ""

# Generate 32-byte (256-bit) secrets for HS256
ACCESS_TOKEN_SECRET=$(openssl rand -base64 32)
REFRESH_TOKEN_SECRET=$(openssl rand -base64 32)

echo "Add these to your .env file:"
echo ""
echo "JWT_ACCESS_TOKEN_SECRET=$ACCESS_TOKEN_SECRET"
echo "JWT_REFRESH_TOKEN_SECRET=$REFRESH_TOKEN_SECRET"
echo ""
echo "Or copy them directly:"
echo ""
echo "Access Token Secret:"
echo "$ACCESS_TOKEN_SECRET"
echo ""
echo "Refresh Token Secret:"
echo "$REFRESH_TOKEN_SECRET"
echo ""


# ---------- Stage 1: Build ----------
FROM node:20-alpine AS build

# Set working directory
WORKDIR /app

# Install app dependencies
COPY package*.json ./
RUN npm ci

# Bundle app source
COPY . .

# Build the application for production
RUN npm run build


# ---------- Stage 2: Runtime (no node_modules folder) ----------
FROM node:20-alpine AS run

# Set working directory
WORKDIR /app

# Set environment variables
ENV NODE_ENV=production
ENV PORT=3000

# Copy standalone server output (includes minimal required files)
COPY --from=build /app/.next/standalone ./

# Copy static + public assets
COPY --from=build /app/.next/static ./.next/static
COPY --from=build /app/public ./public

EXPOSE 3000

# standalone output provides server.js
CMD ["node", "server.js"]

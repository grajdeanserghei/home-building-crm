# syntax=docker/dockerfile:1

# ---- Dependencies ----
FROM node:22-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci

# ---- Build ----
FROM node:22-alpine AS build
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN npm run build

# ---- Runtime ----
FROM node:22-alpine AS runtime
WORKDIR /app
# Version stamp for the page footer. Passed by scripts/build-and-push.sh as build-args
# (APP_VERSION = release semver, GIT_SHA = git short-SHA) and read server-side at request
# time via app/lib/version.ts. Empty when built without the script (footer shows "dev").
ARG APP_VERSION=""
ARG GIT_SHA=""
ENV NODE_ENV=production
ENV PORT=3000
ENV HOSTNAME=0.0.0.0
ENV APP_VERSION=$APP_VERSION
ENV GIT_SHA=$GIT_SHA
RUN addgroup -g 1001 nodejs && adduser -u 1001 -G nodejs -S nextjs
# Next.js standalone output: server.js + minimal node_modules, plus static assets.
COPY --from=build /app/public ./public
COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static
USER nextjs
EXPOSE 3000
CMD ["node", "server.js"]

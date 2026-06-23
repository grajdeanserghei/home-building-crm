#!/usr/bin/env bash
#
# Build and push the three deployable service images: api, mcp, web.
# Automates the manual flow documented in docs/guides/container-images.md.
#
# Tagging strategy:
#   - Every build is tagged with the immutable git short-SHA (e.g. :a1b2c3d).
#     This is the traceable, never-overwritten tag and is safe to pin in a
#     manifest just like a semver tag.
#   - --version adds an explicit semantic-version tag (e.g. v0.1.0). This is the
#     tag the cluster overlay pins for a release (see the guide's checklist).
#     It also creates a matching annotated git tag on HEAD and pushes it, so the
#     released images are traceable to a tagged commit (disable with --no-git-tag).
#   - :latest is NOT pushed by default. The guide mandates pinning exact tags and
#     never relying on latest; pass --latest only if you really want it.
#   - A dirty working tree produces a :<sha>-dirty tag, and the version/latest
#     tags (and the git tag) are SKIPPED so a release never points at
#     uncommitted work.
#
# Builds target linux/amd64 by default (the k3s nodes' architecture).
#
# Usage:
#   scripts/build-and-push.sh [-r REGISTRY] [-v VERSION] [--platform P]
#                             [--latest] [--no-push] [--allow-dirty] [--no-git-tag]
#
#   -r, --registry    Registry + namespace prefix.
#                     Default: registry.crozy.eu/home-project-management
#                     (also via the REGISTRY env var)
#   -v, --version     Release version, e.g. v0.1.0 (also VERSION env var).
#       --platform    docker build target platform. Default: linux/amd64.
#                     Pass an empty string to use docker's default.
#                     (also PLATFORM env var)
#       --latest      Also tag/push :latest (off by default; the guide advises
#                     against relying on it).
#       --no-push     Build images only; do not push (also skips pushing the git tag).
#       --allow-dirty Push version/latest tags even on a dirty tree.
#       --no-git-tag  Do not create/push a git tag for --version.
#   -h, --help        Show this help.
#
# You must be logged in to the registry first:
#   docker login registry.crozy.eu

set -euo pipefail

REGISTRY="${REGISTRY:-registry.crozy.eu/home-project-management}"
VERSION="${VERSION:-}"
PLATFORM="${PLATFORM-linux/amd64}"
PUSH=true
PUSH_LATEST=false
ALLOW_DIRTY=false
GIT_TAG=true

usage() { sed -n '2,41p' "$0" | sed 's/^#\( \|$\)//'; }

while [[ $# -gt 0 ]]; do
  case "$1" in
    -r|--registry)  REGISTRY="$2"; shift 2 ;;
    -v|--version)   VERSION="$2";  shift 2 ;;
    --platform)     PLATFORM="$2"; shift 2 ;;
    --latest)       PUSH_LATEST=true;  shift ;;
    --no-push)      PUSH=false;        shift ;;
    --allow-dirty)  ALLOW_DIRTY=true;  shift ;;
    --no-git-tag)   GIT_TAG=false;     shift ;;
    -h|--help)      usage; exit 0 ;;
    *) echo "Unknown option: $1" >&2; usage; exit 1 ;;
  esac
done

# Always operate from the repo root so the .NET build contexts resolve.
REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"

SHA="$(git rev-parse --short HEAD)"
DIRTY=false
if [[ -n "$(git status --porcelain)" ]]; then
  DIRTY=true
  SHA="${SHA}-dirty"
fi

# The immutable SHA tag is always built. version/latest are "moving"/release
# tags and are suppressed on a dirty tree unless explicitly allowed.
TAGS=("$SHA")
VERSION_RELEASED=false
if [[ "$DIRTY" == true && "$ALLOW_DIRTY" != true ]]; then
  echo "WARNING: working tree is dirty -> tagging '$SHA' only; skipping version/latest." >&2
  echo "         Commit your changes, or pass --allow-dirty to override." >&2
else
  if [[ -n "$VERSION" ]]; then
    TAGS+=("$VERSION")
    VERSION_RELEASED=true
  fi
  $PUSH_LATEST && TAGS+=("latest")
fi

# image-name | dockerfile path | build context  (names per container-images.md)
IMAGES=(
  "api|src/HomeProjectManagement.ApiService/Dockerfile|."
  "mcp|src/HomeProjectManagement.McpServer/Dockerfile|."
  "web|src/web/Dockerfile|src/web"
)

echo "Registry : $REGISTRY"
echo "Tags     : ${TAGS[*]}"
echo "Platform : ${PLATFORM:-<docker default>}"
echo "Push     : $PUSH"
echo

for entry in "${IMAGES[@]}"; do
  IFS='|' read -r NAME DOCKERFILE CONTEXT <<< "$entry"
  REF="${REGISTRY}/${NAME}"

  BUILD_ARGS=(-f "$DOCKERFILE")
  [[ -n "$PLATFORM" ]] && BUILD_ARGS+=(--platform "$PLATFORM")
  for t in "${TAGS[@]}"; do
    BUILD_ARGS+=(-t "${REF}:${t}")
  done

  echo "==> Building $REF"
  docker build "${BUILD_ARGS[@]}" "$CONTEXT"

  if $PUSH; then
    for t in "${TAGS[@]}"; do
      echo "==> Pushing ${REF}:${t}"
      docker push "${REF}:${t}"
    done
  fi
  echo
done

# Create and push a git tag for the released version, so the pushed images map
# to a tagged commit. Only when a clean version build actually happened.
if [[ "$GIT_TAG" == true && "$VERSION_RELEASED" == true ]]; then
  if git rev-parse -q --verify "refs/tags/${VERSION}" >/dev/null; then
    echo "==> Git tag '${VERSION}' already exists; leaving it as-is."
  else
    echo "==> Creating git tag '${VERSION}'"
    git tag -a "$VERSION" -m "Release $VERSION"
  fi

  if $PUSH; then
    echo "==> Pushing git tag '${VERSION}'"
    git push origin "refs/tags/${VERSION}"
  else
    echo "    (--no-push) git tag created locally; not pushed."
  fi
  echo
fi

echo "Done."

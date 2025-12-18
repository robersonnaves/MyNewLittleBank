#!/bin/sh
set -e

# Fix permissions on the notifications directory
# This is needed because Docker volumes are mounted with root ownership
chmod 777 /data/notifications 2>/dev/null || true

# Execute the main application
exec "$@"

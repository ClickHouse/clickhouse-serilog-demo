#!/bin/bash

# Enable local app mode (skips API authentication)
export IS_LOCAL_APP_MODE="DANGEROUSLY_is_local_app_mode💀"

# entry.base.sh unconditionally overrides DEFAULT_SOURCES with OTel defaults.
# Remove those lines so our Docker env var survives.
# (DEFAULT_CONNECTIONS already has a -z guard and works fine.)
sed -i '/^export DEFAULT_SOURCES=/d' /etc/local/entry.base.sh
sed -i '/BETA_CH_OTEL_JSON_SCHEMA_ENABLED/,/^fi$/d' /etc/local/entry.base.sh

# Enable full-text (text) index support
cat > /etc/clickhouse-server/users.d/enable_fts.xml <<'XML'
<clickhouse>
    <profiles>
        <default>
            <enable_full_text_index>1</enable_full_text_index>
        </default>
    </profiles>
</clickhouse>
XML

source "/etc/local/entry.base.sh"

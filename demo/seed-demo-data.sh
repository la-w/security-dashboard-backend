#!/usr/bin/env bash
# Seeds a locally running backend (dotnet run, Development) with the sample SARIF
# reports from the test project, simulating several CI pipeline runs.
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5090}"
API_KEY="${API_KEY:-IAmASecretApiKey}"
DIR="$(cd "$(dirname "$0")/../SecurityDashboard-Backend.Tests/TestFiles" && pwd)"

post() { # path content-type data
  curl --silent --show-error --fail-with-body -o /dev/null -w '%{http_code}' \
    -H "Authorization: ApiKey $API_KEY" -H "Content-Type: $2" --data "$3" "$BASE_URL$1"
}

# Each run uploads fewer reports, so the trend chart shows findings going down.
runs=(
  "1001|2024-06-17T09:00:00Z|FluidAttacks-SAST Semgrep Gitleaks OwaspDependencyCheck Trivy FluidAttacks-DAST OwaspZap"
  "1002|2024-06-20T14:30:00Z|FluidAttacks-SAST Semgrep Gitleaks OwaspDependencyCheck Trivy OwaspZap"
  "1003|2024-06-24T11:15:00Z|FluidAttacks-SAST Semgrep Gitleaks OwaspDependencyCheck OwaspZap"
  "1004|2024-06-27T16:45:00Z|Semgrep Gitleaks OwaspDependencyCheck OwaspZap"
)

for run in "${runs[@]}"; do
  IFS='|' read -r id created tools <<<"$run"
  data=$(printf '{"pipelineId":%s,"createdAt":"%s","branch":"dev","commitHash":"%040d","repository":{"name":"juice-shop","url":"https://example.com/demo/juice-shop"}}' "$id" "$created" "$id")
  echo "Pipeline #$id: register $(post /api/Sarif/RegisterPipelineRun application/json "$data")"
  for t in $tools; do
    echo "  $t: $(post "/api/Sarif/RegisterReport?pipelineId=$id" application/sarif+json "@$DIR/$t.sarif")"
  done
  echo "  deduplicate: $(post "/api/Sarif/RunDeduplication?pipelineId=$id" application/json "$data")"
done

#!/bin/bash

listTools="
Fluid-Attacks-SAST-Results.sarif 
Semgrep-Results.sarif 
gitleaks-report.sarif 
dependency-check-report.sarif 
gl-container-scanning-report.sarif 
Fluid-Attacks-DAST-Results.sarif 
Zap-Report.sarif.json
"

for tool in $listTools; do
  if [ -f "$tool" ]; then
    echo "$tool found. Sending report"
    RESPONSE_CODE=$(curl --location 'https://backend.tbdpwl.dynv6.net/api/Sarif/RegisterReport?pipelineId='"$CI_PIPELINE_ID"'' \
    --silent \
    --show-error \
    --output /dev/null \
    --write-out '%{response_code}' \
    --header 'Content-Type: application/sarif+json' \
    --header 'Authorization: ApiKey '"$BACKEND_API_KEY"'' \
    --data '@'"$tool"'')
    if [ $RESPONSE_CODE = 200 ]
    then
      printf -- "%s\n\n" "Report sent successfully"
    else
      printf -- "%s\n\n" "Response: $RESPONSE_CODE"
    fi
  else
    printf -- "%s\n\n" "$tool not found. Skipping report"
  fi
done

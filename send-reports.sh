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

# Create pipeline data json
pipelineData='{
  "pipelineId": '"$CI_PIPELINE_ID"',
  "createdAt": "'"$CI_PIPELINE_CREATED_AT"'",
  "branch": "'"$CI_COMMIT_REF_NAME"'",
  "commitHash": "'"$CI_COMMIT_SHA"'",
  "repository": {
    "name": "'"$CI_PROJECT_NAME"'",
    "url": "'"$CI_PROJECT_URL"'"
  }
}'

echo "### SENDING REPORTS TO PRODUCTION BACKEND ###"
RESPONSE_CODE=$(curl --location 'https://backend.tbdpwl.dynv6.net/api/Sarif/RegisterPipelineRun' \
  --silent \
  --show-error \
  --output /dev/null \
  --write-out '%{response_code}' \
  --header 'Content-Type: application/json' \
  --header 'Authorization: ApiKey '"$BACKEND_API_KEY"'' \
  --data "$pipelineData")
if [ $RESPONSE_CODE = 200 ]
then
  printf -- "%s\n\n" "Pipeline was successfully registered"
else
  printf -- "%s\n\n" "Pipeline could not be registered - Response: $RESPONSE_CODE"
fi

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

RESPONSE_CODE=$(curl --location 'https://backend.tbdpwl.dynv6.net/api/Sarif/RunDeduplication?pipelineId='"$CI_PIPELINE_ID"'' \
  --silent \
  --show-error \
  --output /dev/null \
  --write-out '%{response_code}' \
  --header 'Content-Type: application/json' \
  --header 'Authorization: ApiKey '"$BACKEND_API_KEY"'' \
  --data "$pipelineData")
if [ $RESPONSE_CODE = 200 ]
then
  printf -- "%s\n\n" "Deduplication was successfully executed"
else
  printf -- "%s\n\n" "Dedpulication could not be executed - Response: $RESPONSE_CODE"
fi

echo "### SENDING REPORTS TO DEV BACKEND ###"
RESPONSE_CODE=$(curl --location 'https://backend-dev.tbdpwl.dynv6.net/api/Sarif/RegisterPipelineRun' \
  --silent \
  --show-error \
  --output /dev/null \
  --write-out '%{response_code}' \
  --header 'Content-Type: application/json' \
  --header 'Authorization: ApiKey IAmASecretApiKey' \
  --data "$pipelineData")
if [ $RESPONSE_CODE = 200 ]
then
  printf -- "%s\n\n" "Pipeline was successfully registered"
else
  printf -- "%s\n\n" "Pipeline could not be registered - Response: $RESPONSE_CODE"
fi

for tool in $listTools; do
  if [ -f "$tool" ]; then
    echo "$tool found. Sending report"
    RESPONSE_CODE=$(curl --location 'https://backend-dev.tbdpwl.dynv6.net/api/Sarif/RegisterReport?pipelineId='"$CI_PIPELINE_ID"'' \
    --silent \
    --show-error \
    --output /dev/null \
    --write-out '%{response_code}' \
    --header 'Content-Type: application/sarif+json' \
    --header 'Authorization: ApiKey IAmASecretApiKey' \
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

RESPONSE_CODE=$(curl --location 'https://backend-dev.tbdpwl.dynv6.net/api/Sarif/RunDeduplication?pipelineId='"$CI_PIPELINE_ID"'' \
  --silent \
  --show-error \
  --output /dev/null \
  --write-out '%{response_code}' \
  --header 'Content-Type: application/json' \
  --header 'Authorization: ApiKey IAmASecretApiKey' \
  --data "$pipelineData")
if [ $RESPONSE_CODE = 200 ]
then
  printf -- "%s\n\n" "Deduplication was successfully executed"
else
  printf -- "%s\n\n" "Dedpulication could not be executed - Response: $RESPONSE_CODE"
fi

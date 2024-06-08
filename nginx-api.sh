#!/bin/bash
# More safety, by turning some bugs into errors.
# Without `errexit` you don't need ! and can replace
# ${PIPESTATUS[0]} with a simple $?, but I prefer safety.
set -o errexit -o pipefail -o noclobber -o nounset

# -allow a command to fail with !'s side effect on errexit
# -use return value from ${PIPESTATUS[0]}, because ! hosed $?
! getopt --test > /dev/null 
if [[ ${PIPESTATUS[0]} -ne 4 ]]; then
    echo "I'm sorry, `getopt --test` failed in this environment." >&2
    exit 1
fi

BASE_URL="https://nginx.tbdpwl.dynv6.net/api/"
API_TOKEN=""
# Default Values
HOSTNAME=""
PORT=0


printHelp ()
{
  echo "Usage:  $0 [OPTIONS] COMMAND"
  echo ""
  echo "Commands:"
  echo "  up          Create review environment" 
  echo "  down        Stop review environment" 
  echo ""
  echo "Options:"
  echo "  -h, --help                  Print this help message"
  echo "  -n, --hostname string       Hostname to use"
  echo "  -p, --port integer          Port of application"
}

start ()
{
    requestToken
    getProxyID
    if [ -z "$PROXY_ID" ]
    then
      echo "\$PROXY_ID is empty"
      createProxyHost
    else
      echo "\$PROXY_ID is NOT empty"
      echo "Skip create proxy-host"
    fi
    exit 0
}

createProxyHost ()
{
    echo "Create proxy-host"
    curl --location 'https://nginx.tbdpwl.dynv6.net/api/nginx/proxy-hosts' \
    --header 'Content-Type: application/json' \
    --header 'Authorization: Bearer '"$API_TOKEN"'' \
    --data '{
      "domain_names": [
        "'"$HOSTNAME"'.tbdpwl.dynv6.net"
      ],
      "forward_scheme": "http",
      "forward_host": "'"$HOSTNAME"'",
      "forward_port": '"$PORT"',
      "access_list_id": 0,
      "certificate_id": 0,
      "ssl_forced": false,
      "caching_enabled": true,
      "block_exploits": true,
      "allow_websocket_upgrade": true,
      "http2_support": false,
      "hsts_enabled": false,
      "hsts_subdomains": false,
      "advanced_config": ""
      }'

}

stop ()
{
    requestToken
    getProxyID
    ANSWER=$(curl --location --request DELETE "https://nginx.tbdpwl.dynv6.net/api/nginx/proxy-hosts/$PROXY_ID" \
--header "Authorization: Bearer $API_TOKEN")
    echo $ANSWER
    
}

getProxyID ()
{
    ANSWER=$(curl --location "https://nginx.tbdpwl.dynv6.net/api/nginx/proxy-hosts" \
        --header "Authorization: Bearer $API_TOKEN")
    PROXY_ID=$(jq '.[] | select (.forward_host == "'"$HOSTNAME"'")'.id <<< $ANSWER)
    echo $PROXY_ID
}

requestToken ()
{
    echo "Requesting new token"
    ANSWER=$(curl --location 'https://nginx.tbdpwl.dynv6.net/api/tokens' \
    --header 'charset: UTF-8' \
    --header 'Content-Type: application/json' \
    --data-raw '{
	"identity": "cicd@tbdpwl.dynv6.net",
	"secret": "'"$NGINX_API_PASSWORD"'",
	"expiry": "1h"
    }')
    JQ_OUT=$(jq '.token' <<< $ANSWER)
    API_TOKEN=$(tr -d '"' <<< $JQ_OUT)
}

LONGOPTS=help,hostname:,port:
OPTIONS=hn:p:

# -regarding ! and PIPESTATUS see above
# -temporarily store output to be able to check for errors
# -activate quoting/enhanced mode (e.g. by writing out `--options`)
# -pass arguments only via   -- "$@"   to separate them correctly
! PARSED=$(getopt --options=$OPTIONS --longoptions=$LONGOPTS --name "$0" -- "$@")
if [[ ${PIPESTATUS[0]} -ne 0 ]]; then
    # e.g. return value is 1
    #  then getopt has complained about wrong arguments to stdout
    exit 1
fi
# read getopt's output this way to handle the quoting right:
eval set -- "$PARSED"

# now enjoy the options in order and nicely split until we see --
while true; do
    case "$1" in
        -n|--hostname)
            HOSTNAME="$2"
            shift 2
            ;;
        -p|--port)
            PORT=$2
            shift 2
            ;;
        -h|--help)
            printHelp
            exit 0
            ;;
        --)
            shift
            break
            ;;
        *)
            echo "Programming error" >&2
            exit 5
            ;;
    esac
done

# handle non-option arguments
if [[ $# -ne 1 ]]; then
    echo "A single command is required." >&2
    echo ""
    printHelp
    exit 1
fi

if [[ $1 != "up" && $1 != "down" ]]; then
  printf -- "%s\n" "Invalid command: $1" >&2
  echo ""
  printHelp
  exit 1
fi

case "$1" in
    up)
      printf -- "%s\n" "Bringing review environment *$HOSTNAME* up"
      start
      ;;
    down)
      printf -- "%s\n" "Bringing review environment *$HOSTNAME* down"
      stop
      ;;
    *)
      echo "Programming Error" >&2
      exit 1
      ;;
esac

echo "Exit"

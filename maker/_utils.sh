#

function getarg() {
  local name="$1"
  shift  # Remove the name argument from the list

  for arg in "$@"; do
    if [[ "$arg" == "--"$name"="* ]]; then
      echo "${arg#*=}"
      return 0
    fi
  done
  return 1
}

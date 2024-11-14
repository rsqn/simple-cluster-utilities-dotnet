#!/bin/bash

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

solution_name=$(getarg name "$@") 
if [[ -z "$solution_name" ]]; then
    read -p "Enter the solution name: " solution_name
fi

echo "solution name is $solution_name"

library_name="$solution_name"

# Set the test project name based on the library name
test_name="${library_name}.Tests"

# Create the solution
dotnet new sln --name "$solution_name"

# Create the library project
dotnet new classlib --name "$library_name"
dotnet sln add "$library_name"/"$library_name".csproj

# Create the test project
dotnet new xunit --name "$test_name"
dotnet sln add "$test_name"/"$test_name".csproj

# Add a reference to the library project from the test project
dotnet add "$test_name"/"$test_name".csproj reference "$library_name"/"$library_name".csproj
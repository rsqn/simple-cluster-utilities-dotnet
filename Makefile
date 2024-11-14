.PHONY: %
.EXPORT_ALL_VARIABLES:

BUILD_CONFIGURATION:=release
SOLUTION := simple.cluster.utilities.sln
default: build
# sed -i "s/^version=.*/version=$(grep -oP '^version=\K\d+\.\d+\.\d+' build.properties | awk -F. '{print $1, $2, ($3+1)}' OFS='.')/" build.properties && echo "Version updated to: $(grep -oP '^version=\K\d+\.\d+\.\d+' build.properties)"

VERSION:=$(shell cat VERSION)

NUGET_FEED_NAME:=BFG.Nuget.2
NUGET_FEED_SOURCE:=https://pkgs.dev.azure.com/Bell-Financial/_packaging/BFG.Nuget.2/nuget/v3/index.json
NUGET_NUPKG_FILE:=simple.cluster.utilities/bin/Release/simple.cluster.utilities.$(VERSION).nupkg

COVERAGE_THRESHOLD := 99
COVERAGE_REPORT := coverage.cobertura.xml

###############################
# BUILD TARGETS
###############################

version.patch:
	@echo $(shell echo $(shell cat VERSION) | awk -F. '{$$3=$$3+1; print $$1"."$$2"."$$3}') > VERSION
	@echo $(shell cat VERSION)

clean:
	find . -type d \( -name obj -o -name TestResults -name bin \) -exec rm -rf {} +
	dotnet nuget locals all --clear

logout:
	dotnet nuget remove source "$(NUGET_FEED_NAME)"  

login: logout
	@read -p "Enter username: " username; \
	read -p "Enter personal access token: " pat; \
	dotnet nuget add source "$(NUGET_FEED_SOURCE)"  --name "$(NUGET_FEED_NAME)" --username $$username --password $$pat  --store-password-in-clear-text

install:
	dotnet restore $(SOLUTION)
	
build: 
	dotnet build --no-restore $(SOLUTION)

test: build
	dotnet test $(SOLUTION)  --collect "Code coverage" --collect:"XPlat Code Coverage" /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Threshold=$(COVERAGE_THRESHOLD) --logger "trx;LogFileName=test_results.trx"


package: clean install build
	dotnet pack $(SOLUTION)


# see local-publish.bat
# publish.local.windows:
# 	VERSION=$(shell type VERSION)
# 	NUGET_NUPKG_FILE=simple.cluster.utilities/bin/Release/simple.cluster.utilities.$(VERSION).nupkg
# 	echo PKG is $(NUGET_NUPKG_FILE)
# 	#dotnet nuget push "$(NUGET_NUPKG_FILE)" -s "$(USERPROFILE)/.nuget/packages/" -k 0

publish.local: 
	dotnet nuget push "$(NUGET_NUPKG_FILE)" -s "$(HOME)/.nuget/packages/" -k 0
	#dotnet nuget push bin\Debug\MyLibrary.1.0.0.nupkg -s C:\MyLocalNuGetFeed -k 0

publish: package
	dotnet nuget push --source "$(NUGET_FEED_SOURCE)" --api-key az "$(NUGET_NUPKG_FILE)" 


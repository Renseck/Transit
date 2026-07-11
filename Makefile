PUBLISH_DIR = C:\inetpub\wwwroot\Transit
APPCMD = C:\Windows\system32\inetsrv\appcmd.exe

TRANSIT_PROJ     	= src/Transit/Transit.csproj
TRANSIT_TEST_PROJ   = tests/Transit.Tests/Transit.Tests.csproj
USERMGR_PROJ 		= src/tools/UserManager/UserManager.csproj

.PHONY: build test run publish add-user fingerprint-cleanup-steps

build:
	dotnet build $(TRANSIT_PROJ)

test:
	dotnet test $(TRANSIT_TEST_PROJ)

run:
	dotnet run --project $(TRANSIT_PROJ)

publish:
	dotnet publish $(TRANSIT_PROJ) -c Release -o $(PUBLISH_DIR)
	@echo Done. Run 'iisreset' on the server to pick up changes.

publish-restart:
	@$(APPCMD) list apppool | rg "Transit" | rg -q "Stopped" || $(APPCMD) stop apppool /apppool.name:"Transit"
	dotnet publish $(TRANSIT_PROJ) -c Release -o $(PUBLISH_DIR)
	$(APPCMD) start apppool /apppool.name:"Transit"

add-user:
	dotnet run --project $(USERMGR_PROJ)

rebuild-test: build test
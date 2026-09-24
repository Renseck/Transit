PUBLISH_DIR = C:\inetpub\wwwroot\Transit
APPCMD      = C:\Windows\system32\inetsrv\appcmd.exe
APPPOOL     = Transit

SLN        = Transit.slnx
WEB_PROJ   = src/Transit.Web/Transit.Web.csproj
INFRA_PROJ = src/Transit.Infrastructure/Transit.Infrastructure.csproj
TEST_PROJ  = tests/Transit.Tests/Transit.Tests.csproj

.PHONY: build test run publish publish-restart migrate db-update rebuild-test install

build:
	dotnet build $(SLN)

test:
	dotnet test $(TEST_PROJ)

run:
	dotnet run --project $(WEB_PROJ)

publish:
	dotnet publish $(WEB_PROJ) -c Release -o $(PUBLISH_DIR)
	@echo Done. Run 'iisreset' on the server to pick up changes.

publish-restart:
	@$(APPCMD) list apppool | rg "$(APPPOOL)" | rg -q "Stopped" || $(APPCMD) stop apppool /apppool.name:"$(APPPOOL)"
	dotnet publish $(WEB_PROJ) -c Release -o $(PUBLISH_DIR)
	$(APPCMD) start apppool /apppool.name:"$(APPPOOL)"

# EF Core migrations (schema is also auto-applied on app startup).
# Usage: make migrate NAME=InitialCreate
migrate:
	dotnet ef migrations add $(NAME) --project $(INFRA_PROJ) --startup-project $(WEB_PROJ)

db-update:
	dotnet ef database update --project $(INFRA_PROJ) --startup-project $(WEB_PROJ)

rebuild-test: build test

install:
	powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Install-Transit.ps1

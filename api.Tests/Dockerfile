# Build and run the xUnit test project.
# Context must be the repo root so both api/ and api.Tests/ are accessible.
#
#   docker build -f api.Tests/Dockerfile -t prms-api-tests .
#   docker run --rm prms-api-tests

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS test
WORKDIR /repo

# Restore dependencies for both projects first (better layer caching)
COPY api/PerformanceReviewApi.csproj        api/
COPY api.Tests/api.Tests.csproj             api.Tests/
RUN dotnet restore api.Tests/api.Tests.csproj

# Copy full source
COPY api/     api/
COPY api.Tests/ api.Tests/

WORKDIR /repo/api.Tests
CMD ["dotnet", "test", "--no-restore", "--logger", "console;verbosity=normal"]

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS test
WORKDIR /src

# Copy solution files
COPY api/PerformanceReviewApi.csproj                                    api/
COPY tests/PerformanceReviewApi.Tests/PerformanceReviewApi.Tests.csproj tests/PerformanceReviewApi.Tests/

RUN dotnet restore tests/PerformanceReviewApi.Tests/PerformanceReviewApi.Tests.csproj

COPY api/   api/
COPY tests/ tests/

WORKDIR /src/tests/PerformanceReviewApi.Tests
CMD ["dotnet", "test", "--no-restore", "--logger", "trx;LogFileName=results.trx", "--results-directory", "/results"]

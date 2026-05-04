.PHONY: help build test clean release restore format lint verify install docs

# Colors for output
BLUE := \033[0;34m
GREEN := \033[0;32m
YELLOW := \033[0;33m
RED := \033[0;31m
NC := \033[0m # No Color

PROJECT_NAME := DotnetEventBus
SOLUTION_FILE := DotnetEventBus.sln
BUILD_CONFIG := Debug
RELEASE_CONFIG := Release
OUTPUT_DIR := ./bin
ARTIFACTS_DIR := ./artifacts

help:
	@echo "$(BLUE)$(PROJECT_NAME) - Build and Development Tasks$(NC)"
	@echo ""
	@echo "$(YELLOW)Available targets:$(NC)"
	@echo "  $(GREEN)help$(NC)              - Show this help message"
	@echo "  $(GREEN)build$(NC)             - Build the solution in Debug mode"
	@echo "  $(GREEN)release$(NC)           - Build the solution in Release mode"
	@echo "  $(GREEN)test$(NC)              - Run all unit tests"
	@echo "  $(GREEN)test-verbose$(NC)      - Run tests with detailed output"
	@echo "  $(GREEN)test-coverage$(NC)     - Run tests with code coverage"
	@echo "  $(GREEN)clean$(NC)             - Clean build artifacts"
	@echo "  $(GREEN)restore$(NC)           - Restore NuGet packages"
	@echo "  $(GREEN)format$(NC)            - Format code using dotnet format"
	@echo "  $(GREEN)lint$(NC)              - Run code analysis"
	@echo "  $(GREEN)verify$(NC)            - Verify code quality (format + lint + test)"
	@echo "  $(GREEN)install$(NC)           - Install NuGet package locally"
	@echo "  $(GREEN)docs$(NC)              - Generate API documentation"
	@echo "  $(GREEN)publish$(NC)           - Create NuGet package"
	@echo "  $(GREEN)all$(NC)               - Clean, restore, build, and test"
	@echo ""

# Default target
.DEFAULT_GOAL := help

build:
	@echo "$(BLUE)[Build] Compiling $(PROJECT_NAME) in Debug mode...$(NC)"
	@dotnet build $(SOLUTION_FILE) -c $(BUILD_CONFIG) --nologo
	@echo "$(GREEN)✓ Build completed successfully$(NC)"

release:
	@echo "$(BLUE)[Release] Compiling $(PROJECT_NAME) in Release mode...$(NC)"
	@dotnet build $(SOLUTION_FILE) -c $(RELEASE_CONFIG) --nologo
	@echo "$(GREEN)✓ Release build completed successfully$(NC)"

restore:
	@echo "$(BLUE)[Restore] Restoring NuGet packages...$(NC)"
	@dotnet restore $(SOLUTION_FILE)
	@echo "$(GREEN)✓ Packages restored successfully$(NC)"

test:
	@echo "$(BLUE)[Test] Running unit tests...$(NC)"
	@dotnet test $(SOLUTION_FILE) -c $(BUILD_CONFIG) --nologo --verbosity minimal
	@echo "$(GREEN)✓ All tests passed$(NC)"

test-verbose:
	@echo "$(BLUE)[Test] Running unit tests with detailed output...$(NC)"
	@dotnet test $(SOLUTION_FILE) -c $(BUILD_CONFIG) --nologo --verbosity normal
	@echo "$(GREEN)✓ Tests completed$(NC)"

test-coverage:
	@echo "$(BLUE)[Test] Running tests with code coverage...$(NC)"
	@dotnet test $(SOLUTION_FILE) -c $(BUILD_CONFIG) \
		--nologo \
		--collect:"XPlat Code Coverage" \
		--results-directory=$(OUTPUT_DIR)/coverage
	@echo "$(GREEN)✓ Coverage report generated in $(OUTPUT_DIR)/coverage$(NC)"

clean:
	@echo "$(BLUE)[Clean] Removing build artifacts...$(NC)"
	@dotnet clean $(SOLUTION_FILE) --nologo
	@rm -rf $(OUTPUT_DIR) $(ARTIFACTS_DIR)
	@find . -type d -name "obj" -exec rm -rf {} + 2>/dev/null || true
	@find . -type d -name ".vs" -exec rm -rf {} + 2>/dev/null || true
	@echo "$(GREEN)✓ Cleanup completed$(NC)"

format:
	@echo "$(BLUE)[Format] Formatting C# code...$(NC)"
	@dotnet format $(SOLUTION_FILE) --verbosity diagnostic
	@echo "$(GREEN)✓ Code formatted$(NC)"

lint:
	@echo "$(BLUE)[Lint] Running code analysis...$(NC)"
	@dotnet build $(SOLUTION_FILE) -c $(BUILD_CONFIG) /p:EnforceCodeStyleInBuild=true --nologo
	@echo "$(GREEN)✓ Code analysis completed$(NC)"

verify: clean restore format lint build test
	@echo "$(GREEN)✓ All verification checks passed$(NC)"

install: release
	@echo "$(BLUE)[Install] Installing NuGet package...$(NC)"
	@mkdir -p $(ARTIFACTS_DIR)
	@dotnet pack src/$(PROJECT_NAME)/$(PROJECT_NAME).csproj \
		-c $(RELEASE_CONFIG) \
		-o $(ARTIFACTS_DIR) \
		--nologo
	@echo "$(GREEN)✓ Package created in $(ARTIFACTS_DIR)$(NC)"

docs:
	@echo "$(BLUE)[Docs] Generating API documentation...$(NC)"
	@mkdir -p $(ARTIFACTS_DIR)/docs
	@dotnet build src/$(PROJECT_NAME)/$(PROJECT_NAME).csproj \
		-c $(RELEASE_CONFIG) \
		-p:GenerateDocumentationFile=true \
		--nologo
	@echo "$(GREEN)✓ Documentation generated$(NC)"

publish: release
	@echo "$(BLUE)[Publish] Creating NuGet package...$(NC)"
	@mkdir -p $(ARTIFACTS_DIR)
	@dotnet pack src/$(PROJECT_NAME)/$(PROJECT_NAME).csproj \
		-c $(RELEASE_CONFIG) \
		-o $(ARTIFACTS_DIR) \
		--include-source \
		--include-symbols \
		--nologo
	@echo "$(GREEN)✓ Package ready in $(ARTIFACTS_DIR)$(NC)"
	@echo "$(YELLOW)Push with: dotnet nuget push $(ARTIFACTS_DIR)/*.nupkg$(NC)"

all: clean restore build test
	@echo "$(GREEN)✓ Complete build pipeline finished$(NC)"

# Watch mode - rebuilds on file changes
watch:
	@echo "$(BLUE)[Watch] Monitoring for changes...$(NC)"
	@dotnet watch --project src/$(PROJECT_NAME)/$(PROJECT_NAME).csproj -- build

# Run specific test file
test-file:
	@echo "$(YELLOW)Usage: make test-file FILE=path/to/test$(NC)"
	@if [ -z "$(FILE)" ]; then \
		echo "$(RED)Error: FILE variable not set$(NC)"; \
		exit 1; \
	fi
	@dotnet test $(FILE) -c $(BUILD_CONFIG) --nologo

# Performance benchmark
benchmark:
	@echo "$(BLUE)[Benchmark] Running performance tests...$(NC)"
	@dotnet test $(SOLUTION_FILE) -c $(RELEASE_CONFIG) \
		--filter "Category=Performance" \
		--nologo

# Code statistics
stats:
	@echo "$(BLUE)[Stats] Calculating project statistics...$(NC)"
	@find src -name "*.cs" -type f | wc -l | xargs echo "Total C# files:"
	@find src -name "*.cs" -type f -exec cat {} \; | wc -l | xargs echo "Total lines of code:"
	@find tests -name "*.cs" -type f | wc -l | xargs echo "Total test files:"
	@find tests -name "*.cs" -type f -exec cat {} \; | wc -l | xargs echo "Total test LOC:"

.PHONY: help build release test test-verbose test-coverage clean restore format lint verify install docs publish all watch test-file benchmark stats

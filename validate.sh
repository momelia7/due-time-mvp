#!/bin/bash
# DueTime validation script

echo -e "\033[1;36m===== DueTime Validation =====\033[0m"
echo -e "\033[1;33mRunning code format check...\033[0m"
dotnet format --verify-no-changes

if [ $? -ne 0 ]; then
    echo -e "\033[1;31m❌ Code formatting issues detected. Please run 'dotnet format' to fix.\033[0m"
    exit 1
else
    echo -e "\033[1;32m✅ Code formatting verified\033[0m"
fi

echo -e "\033[1;33mBuilding solution...\033[0m"
dotnet build -c Release /warnaserror

if [ $? -ne 0 ]; then
    echo -e "\033[1;31m❌ Build failed\033[0m"
    exit 1
else
    echo -e "\033[1;32m✅ Build successful\033[0m"
fi

echo -e "\033[1;33mRunning tests...\033[0m"
dotnet test --no-build -c Release

if [ $? -ne 0 ]; then
    echo -e "\033[1;31m❌ Tests failed\033[0m"
    exit 1
else
    echo -e "\033[1;32m✅ All tests passed\033[0m"
fi

echo -e "\033[1;36m===== Validation Complete =====\033[0m"
echo -e "\033[1;32mDueTime is ready for deployment!\033[0m" 
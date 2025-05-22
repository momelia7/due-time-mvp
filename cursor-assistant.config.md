# Cursor Assistant Configuration for DueTime

## Project Overview

DueTime is an automatic time tracking application built with .NET 6.0 that helps users understand and categorize how they spend their time on the computer.

## Code Style Guidelines

- Use C# 10.0 features (compatible with .NET 6.0)
- Follow Microsoft's C# coding conventions
- Use XML documentation comments for public APIs
- Prefer async/await for asynchronous operations
- Use nullable reference types (`string?` etc.)
- Use dependency injection for service components

## Architecture Notes

The application follows a layered architecture:

1. **DueTime.Tracking**: Core tracking logic and interfaces
2. **DueTime.Data**: Data access and repository implementations
3. **DueTime.UI**: WPF user interface

## Important Implementation Details

- Windows native API is used for window and activity tracking
- SQLite database provides local storage
- Windows DPAPI is used for secure storage of API keys
- OpenAI API integration for optional AI features
- Windows Registry used for startup configuration

## Generate Code With These Considerations

- Ensure all generated code is properly error-handled
- Include XML documentation for public methods
- Follow the established naming conventions
- Consider potential threading issues in UI code
- Respect the privacy-first design principles
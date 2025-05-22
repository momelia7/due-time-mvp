# Contributing to DueTime

## Development with AI Assistance

This project was built using AI-assisted development tools, particularly Cursor. When contributing, consider these guidelines:

### Using AI Tools Effectively

1. **Idempotent Commands**: Ensure all code generation prompts are designed to be idempotent - they should produce the same outcome if run multiple times and not duplicate code.

2. **Self-contained Prompts**: Include file paths and sufficient context in your prompts so the AI can generate appropriate code.

3. **Consistent Style**: Follow the established code style and architecture patterns in the project.

4. **Testing AI-generated Code**: All code should be testable. Write or update tests for any new functionality.

### Architecture Considerations

- **Business Logic Layer**: `DueTime.Tracking` contains core tracking logic
- **Data Access Layer**: `DueTime.Data` contains repositories and data models
- **Presentation Layer**: `DueTime.UI` contains WPF user interface
- **Settings and Configuration**: User preferences and license state managed through `SettingsManager`

### Key Design Patterns

- **Repository Pattern**: Used for data access
- **Dependency Injection**: Services are passed to classes that need them
- **Observer Pattern**: Event-based communication between tracking service and UI
- **MVVM-like Structure**: Though not strict MVVM, follows separation of UI and logic

### Trial and Licensing

The app implements a 30-day trial period for premium features:
- Core tracking functionality remains free
- AI-powered features are gated behind trial/license

### Privacy Considerations

- All user data is stored locally by default
- AI features are opt-in only
- User can clear all data at any time

## Pull Request Process

1. Ensure your code builds without errors or warnings
2. Update tests to cover your new functionality
3. Update documentation if needed
4. Submit a pull request with a clear description of changes

## Testing Guidelines

- Unit tests should be provided for all new functionality
- Test both happy path and edge cases
- Run the entire test suite before submitting a PR

```
dotnet test
```

## Build and Release Process

To build the application:

```
dotnet build -c Release
```

To run all tests:

```
dotnet test
``` 
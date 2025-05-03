# DueTime Architecture Documentation

## Architecture Overview

DueTime is built with a modular, layered architecture that separates concerns between data access, business logic, and presentation. This design ensures maintainability, extensibility, and testability.

### Key Components:

1. **UI Layer (WPF Application)**
   - Implements the user interface using WPF with XAML
   - Follows a simplified MVVM pattern with UserControls for different views
   - Communicates with the data and tracking layers through well-defined interfaces

2. **Data Layer**
   - SQLite database for local storage
   - Repository pattern for data access abstraction
   - Secure storage for sensitive data using Windows DPAPI

3. **Tracking Layer**
   - Windows-specific implementation of activity tracking
   - Independent of UI layer for potential background operation
   - Uses system hooks for window tracking

## Technical Choices

### .NET 6
- Modern C# features
- Cross-platform compatibility (for non-Windows-specific components)
- Long-term support

### WPF
- Rich desktop UI framework for Windows
- XAML-based UI with separation of concerns
- Built-in data binding support

### SQLite
- Lightweight, file-based database requiring no server
- Good performance for single-user application
- WAL journal mode for improved concurrency
- Reliable and well-tested in production environments

### Repository Pattern
- Abstracts data access logic
- Enables potential future database technology changes
- Facilitates testing with mock implementations

## Performance Considerations

The application is designed with performance in mind:

- **Memory Usage**: Target < 50MB in idle state
- **CPU Usage**: Target < 3% CPU when tracking in background
- **Database Performance**: Optimized for typical usage patterns
  - Write operations are relatively infrequent (one time entry every few minutes)
  - Read operations are bulk reads for specific time periods
  - WAL journal mode enables concurrent reading while writing

## Security and Privacy

- User data remains local by default
- Windows DPAPI for encrypting sensitive data like API keys
- Optional AI features requiring explicit opt-in
- Backup/restore functionality with option for encrypted backups
- Data deletion capabilities to respect "right to be forgotten"

## Extensibility

The architecture supports several potential future enhancements:

### Potential Cross-Platform Support
- Tracking layer is Windows-specific but clearly separated
- Data layer is platform-agnostic
- UI could be reimplemented using .NET MAUI or other frameworks

### Team Collaboration
- Database schema could be extended for multi-user support
- Repository implementations could be updated to use a cloud database

### Service Architecture
- Tracking engine could run as a Windows Service
- IPC or shared database would enable UI/service communication

## Code Quality Measures

- Treating warnings as errors
- Nullable reference types enabled
- Strong typing throughout the codebase
- Platform-specific code properly annotated
- Comprehensive automated tests including performance tests

## Current Limitations

- Windows-only due to system hook implementation
- Single-user design
- Local data storage only

## Conclusion

The DueTime architecture is well-suited for the MVP requirements while providing clear paths for future enhancements. The modular design with clean separation of concerns ensures that individual components can be modified or replaced without affecting the entire system. 
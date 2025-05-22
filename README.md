# DueTime

DueTime is an automatic time tracking application that monitors your computer activity and helps you categorize and understand how you spend your digital time.

## Features

- **100% Automatic Tracking**: Monitors active windows and applications without requiring manual start/stop
- **Project Categorization**: Assign time entries to projects manually or automatically with rules
- **AI-Powered Insights**: Optional AI features for automatic categorization and weekly summaries
- **Privacy-First Design**: All data stays local by default; AI features are opt-in
- **System Tray Integration**: Runs in the background with minimal footprint
- **Data Management**: Backup, restore, and export your time data

## Getting Started

### Installation

1. Download the latest release from the [Releases](https://github.com/yourusername/duetime/releases) page
2. Run the installer and follow the prompts
3. DueTime will start automatically after installation

### First Steps

1. Create projects that represent your work categories
2. Set up rules to automatically assign applications to projects
3. Optional: Enable AI features for smarter categorization

## Architecture

DueTime is built with a modular architecture:

- **Tracking Engine**: Core .NET library that monitors system activity
- **Data Layer**: SQLite database for storing time entries, projects, and rules
- **UI Layer**: WPF-based user interface with dashboard, projects, and settings

## Development

This project was developed using AI-assisted tools, particularly Cursor. See [CONTRIBUTING.md](CONTRIBUTING.md) for details on development practices.

### Building from Source

```
git clone https://github.com/yourusername/duetime.git
cd duetime
dotnet build
```

### Running Tests

```
dotnet test
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built with .NET 6.0 and WPF
- Uses SQLite for local data storage
- Optional integration with OpenAI API for AI features

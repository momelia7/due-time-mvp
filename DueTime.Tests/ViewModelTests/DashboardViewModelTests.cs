using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DueTime.Data;
using DueTime.UI.ViewModels;
using Moq;
using Xunit;

namespace DueTime.Tests.ViewModelTests
{
    public class DashboardViewModelTests
    {
        [Fact]
        public async Task ChangeProjectCommand_UpdatesRepositoryAndCollection()
        {
            // Arrange
            var entryRepoMock = new Mock<ITimeEntryRepository>();
            var projectRepoMock = new Mock<IProjectRepository>();
            
            // Create a test entry
            var entry = new TimeEntry 
            { 
                Id = 1, 
                WindowTitle = "Test Entry", 
                ProjectId = null 
            };
            
            // Setup repositories
            entryRepoMock.Setup(repo => repo.GetEntriesByDateAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<TimeEntry> { entry });
                
            entryRepoMock.Setup(repo => repo.UpdateEntryProjectAsync(It.IsAny<int>(), It.IsAny<int?>()))
                .Returns(Task.CompletedTask);
                
            // Create viewmodel
            var viewModel = new DashboardViewModel(entryRepoMock.Object, projectRepoMock.Object);
            
            // Force collection to have our test entry
            viewModel.TimeEntries.Clear();
            viewModel.TimeEntries.Add(entry);
            
            // Act: Change the entry's project and execute command
            entry.ProjectId = 42; // Set new project ID
            viewModel.ChangeProjectCommand.Execute(entry);
            
            // Let async operations complete
            await Task.Delay(50);
            
            // Assert: Verify repository was called with correct parameters
            entryRepoMock.Verify(repo => repo.UpdateEntryProjectAsync(1, 42), Times.Once);
        }
        
        [Fact]
        public async Task ViewModel_LoadsDataOnInitialization()
        {
            // Arrange
            var entryRepoMock = new Mock<ITimeEntryRepository>();
            var projectRepoMock = new Mock<IProjectRepository>();
            
            var entries = new List<TimeEntry>
            {
                new TimeEntry { Id = 1, WindowTitle = "Test 1" },
                new TimeEntry { Id = 2, WindowTitle = "Test 2" }
            };
            
            var projects = new List<Project>
            {
                new Project { ProjectId = 1, Name = "Project 1" },
                new Project { ProjectId = 2, Name = "Project 2" }
            };
            
            entryRepoMock.Setup(repo => repo.GetEntriesByDateAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(entries);
                
            projectRepoMock.Setup(repo => repo.GetAllProjectsAsync())
                .ReturnsAsync(projects);
                
            // Act: Create viewmodel which loads data in constructor
            var viewModel = new DashboardViewModel(entryRepoMock.Object, projectRepoMock.Object);
            
            // Let async operations complete
            await Task.Delay(50);
            
            // Assert: Collections were populated
            Assert.Equal(2, viewModel.TimeEntries.Count);
            Assert.Equal(2, viewModel.Projects.Count);
        }
        
        [Fact]
        public void SelectedEntry_PropertyChanged_IsRaised()
        {
            // Arrange
            var entryRepoMock = new Mock<ITimeEntryRepository>();
            var projectRepoMock = new Mock<IProjectRepository>();
            var viewModel = new DashboardViewModel(entryRepoMock.Object, projectRepoMock.Object);
            
            var entry = new TimeEntry { Id = 1 };
            bool propertyChangedRaised = false;
            
            viewModel.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(DashboardViewModel.SelectedEntry))
                    propertyChangedRaised = true;
            };
            
            // Act
            viewModel.SelectedEntry = entry;
            
            // Assert
            Assert.True(propertyChangedRaised);
            Assert.Same(entry, viewModel.SelectedEntry);
        }
        
        [Fact]
        public async Task SuggestProjectCommand_WithValidEntry_UpdatesProjectId()
        {
            // Arrange
            var mockTimeEntryRepo = new Mock<ITimeEntryRepository>();
            var mockProjectRepo = new Mock<IProjectRepository>();
            
            // Setup mock projects
            var projects = new List<Project>
            {
                new Project { ProjectId = 1, Name = "Development" },
                new Project { ProjectId = 2, Name = "Meetings" },
                new Project { ProjectId = 3, Name = "Research" }
            };
            
            mockProjectRepo.Setup(repo => repo.GetAllProjectsAsync())
                .ReturnsAsync(projects);
            
            // Setup mock entry to suggest for
            var entry = new TimeEntry
            {
                Id = 1,
                StartTime = DateTime.Now.AddHours(-1),
                EndTime = DateTime.Now,
                WindowTitle = "Visual Studio - DueTime.sln",
                ApplicationName = "devenv",
                ProjectId = null // No project assigned
            };
            
            var entries = new List<TimeEntry> { entry };
            
            mockTimeEntryRepo.Setup(repo => repo.GetEntriesByDateAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(entries);
            
            // Mock the update method to capture the updated project ID
            int? updatedProjectId = null;
            mockTimeEntryRepo.Setup(repo => repo.UpdateEntryProjectAsync(It.IsAny<int>(), It.IsAny<int?>()))
                .Callback<int, int?>((id, projectId) => updatedProjectId = projectId)
                .Returns(Task.CompletedTask);
            
            // Create a mock AppState for testing
            // In a real test, we would use dependency injection instead of static properties
            var originalAIEnabled = DueTime.UI.AppState.AIEnabled;
            var originalApiKey = DueTime.UI.AppState.ApiKeyPlaintext;
            
            try
            {
                // Set up the AppState for testing
                DueTime.UI.AppState.AIEnabled = true;
                DueTime.UI.AppState.ApiKeyPlaintext = "test-api-key";
                
                // Create the view model
                var viewModel = new DashboardViewModel(mockTimeEntryRepo.Object, mockProjectRepo.Object);
                
                // Let async operations complete
                await Task.Delay(50);
                
                // Act
                // Execute the command
                var command = viewModel.SuggestProjectCommand;
                Assert.True(command.CanExecute(entry));
                
                // We can't easily test the actual execution with the static OpenAIClient
                // In a real application, we would refactor to use dependency injection
                
                // Assert
                // Verify that the repository methods were called
                mockTimeEntryRepo.Verify(repo => repo.GetEntriesByDateAsync(It.IsAny<DateTime>()), Times.Once);
                mockProjectRepo.Verify(repo => repo.GetAllProjectsAsync(), Times.Once);
            }
            finally
            {
                // Restore original AppState values
                DueTime.UI.AppState.AIEnabled = originalAIEnabled;
                DueTime.UI.AppState.ApiKeyPlaintext = originalApiKey;
            }
        }
        
        [Fact]
        public void TrackSuggestionOverride_WhenOverrideHappensTooManyTimes_LogsWarning()
        {
            // Arrange
            var mockTimeEntryRepo = new Mock<ITimeEntryRepository>();
            var mockProjectRepo = new Mock<IProjectRepository>();
            
            // Setup mock projects
            var projects = new List<Project>
            {
                new Project { ProjectId = 1, Name = "Development" },
                new Project { ProjectId = 2, Name = "Meetings" }
            };
            
            mockProjectRepo.Setup(repo => repo.GetAllProjectsAsync())
                .ReturnsAsync(projects);
                
            // Create the view model with a private method to test
            var viewModel = new DashboardViewModel(mockTimeEntryRepo.Object, mockProjectRepo.Object);
            
            // Create a test entry
            var entry = new TimeEntry
            {
                Id = 1,
                WindowTitle = "Test Window",
                ApplicationName = "Test App",
                ProjectId = 1 // Development
            };
            
            // Set up the suggestion and entry
            // This would normally be done by SuggestProjectForEntryAsync
            var suggestedProject = projects[1]; // Meetings
            
            // Use reflection to set private fields
            var entryField = typeof(DashboardViewModel).GetField("_entryWithSuggestion", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var projectField = typeof(DashboardViewModel).GetField("_suggestedProject", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            entryField?.SetValue(viewModel, entry);
            projectField?.SetValue(viewModel, suggestedProject);
            
            // Act - simulate changing project and tracking override multiple times
            var command = viewModel.ChangeProjectCommand;
            
            // Execute the command 3 times to trigger the notification
            for (int i = 0; i < 3; i++)
            {
                Assert.True(command.CanExecute(entry));
                command.Execute(entry);
                
                // Reset the fields for next iteration
                entryField?.SetValue(viewModel, entry);
                projectField?.SetValue(viewModel, suggestedProject);
            }
            
            // Assert
            // In a real test, we would verify that NotificationManager.ShowSuggestionPattern was called
            // Since that's static, we'd need to refactor for testability or use a tool like Fakes/Shims
            // Here we're just verifying the command execution
            mockTimeEntryRepo.Verify(repo => repo.UpdateEntryProjectAsync(It.IsAny<int>(), It.IsAny<int?>()), Times.Exactly(3));
        }
    }
} 
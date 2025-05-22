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
            
            // Create a time entry
            var entry = new TimeEntry 
            { 
                Id = 1, 
                ProjectId = null, 
                WindowTitle = "Test Window", 
                ApplicationName = "TestApp", 
                StartTime = DateTime.Now.AddHours(-1), 
                EndTime = DateTime.Now 
            };
            
            // Setup repositories
            entryRepoMock.Setup(repo => repo.GetEntriesByDateAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<TimeEntry> { entry });
                
            entryRepoMock.Setup(repo => repo.UpdateEntryProjectAsync(It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync(true);
                
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
        public void ViewModel_LoadsDataOnInitialization()
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
            Task.Delay(50).Wait();
            
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
    }
} 
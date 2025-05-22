using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DueTime.Data;
using DueTime.UI.ViewModels;
using Moq;
using Xunit;

namespace DueTime.Tests.ViewModelTests
{
    public class ProjectsViewModelTests
    {
        [Fact]
        public async Task AddProjectCommand_AddsProjectToRepositoryAndCollection()
        {
            // Arrange
            var projectRepoMock = new Mock<IProjectRepository>();
            var ruleRepoMock = new Mock<IRuleRepository>();
            
            // Setup mock to return a new project ID when adding a project
            projectRepoMock.Setup(repo => repo.AddProjectAsync(It.IsAny<string>()))
                .ReturnsAsync(42); // Mock project ID
                
            // Create the viewmodel
            var viewModel = new ProjectsViewModel(projectRepoMock.Object, ruleRepoMock.Object);
            
            // Set the new project name
            viewModel.NewProjectName = "Test Project";
            
            // Act: Execute the command
            viewModel.AddProjectCommand.Execute(null);
            
            // Let async operations complete
            await Task.Delay(50);
            
            // Assert
            projectRepoMock.Verify(repo => repo.AddProjectAsync("Test Project"), Times.Once);
            Assert.Contains(viewModel.Projects, p => p.Name == "Test Project" && p.ProjectId == 42);
            Assert.Empty(viewModel.NewProjectName); // Name should be cleared
        }
        
        [Fact]
        public async Task AddRuleCommand_AddsRuleToRepositoryAndCollection()
        {
            // Arrange
            var projectRepoMock = new Mock<IProjectRepository>();
            var ruleRepoMock = new Mock<IRuleRepository>();
            
            // Create a test project for the rule
            var project = new Project { ProjectId = 5, Name = "Test Project" };
            
            // Setup mock
            ruleRepoMock.Setup(repo => repo.AddRuleAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(123); // Mock rule ID
                
            // Create viewmodel
            var viewModel = new ProjectsViewModel(projectRepoMock.Object, ruleRepoMock.Object);
            
            // Set rule properties
            viewModel.Projects.Add(project);
            viewModel.NewRulePattern = "Test Pattern";
            viewModel.SelectedProject = project;
            
            // Act: Execute the command
            viewModel.AddRuleCommand.Execute(null);
            
            // Let async operations complete
            await Task.Delay(50);
            
            // Assert
            ruleRepoMock.Verify(repo => repo.AddRuleAsync("Test Pattern", 5), Times.Once);
            Assert.Contains(viewModel.Rules, r => r.Pattern == "Test Pattern" && r.ProjectId == 5);
            Assert.Empty(viewModel.NewRulePattern); // Pattern should be cleared
        }
        
        [Fact]
        public void CanAddProject_ReturnsFalseForEmptyName()
        {
            // Arrange
            var projectRepoMock = new Mock<IProjectRepository>();
            var ruleRepoMock = new Mock<IRuleRepository>();
            var viewModel = new ProjectsViewModel(projectRepoMock.Object, ruleRepoMock.Object);
            
            // Act & Assert
            viewModel.NewProjectName = "";
            Assert.False(viewModel.AddProjectCommand.CanExecute(null));
            
            viewModel.NewProjectName = "   ";
            Assert.False(viewModel.AddProjectCommand.CanExecute(null));
            
            viewModel.NewProjectName = "Valid Name";
            Assert.True(viewModel.AddProjectCommand.CanExecute(null));
        }
        
        [Fact]
        public void CanAddRule_ChecksPatternAndSelectedProject()
        {
            // Arrange
            var projectRepoMock = new Mock<IProjectRepository>();
            var ruleRepoMock = new Mock<IRuleRepository>();
            var viewModel = new ProjectsViewModel(projectRepoMock.Object, ruleRepoMock.Object);
            var project = new Project { ProjectId = 5, Name = "Test Project" };
            
            // Act & Assert: Empty pattern, no project
            viewModel.NewRulePattern = "";
            viewModel.SelectedProject = null;
            Assert.False(viewModel.AddRuleCommand.CanExecute(null));
            
            // Pattern but no project
            viewModel.NewRulePattern = "Pattern";
            viewModel.SelectedProject = null;
            Assert.False(viewModel.AddRuleCommand.CanExecute(null));
            
            // No pattern but has project
            viewModel.NewRulePattern = "";
            viewModel.SelectedProject = project;
            Assert.False(viewModel.AddRuleCommand.CanExecute(null));
            
            // Both valid
            viewModel.NewRulePattern = "Pattern";
            viewModel.SelectedProject = project;
            Assert.True(viewModel.AddRuleCommand.CanExecute(null));
        }
        
        [Fact]
        public async Task DeleteRuleCommand_RemovesRuleFromCollection()
        {
            // Arrange
            var projectRepoMock = new Mock<IProjectRepository>();
            var ruleRepoMock = new Mock<IRuleRepository>();
            var viewModel = new ProjectsViewModel(projectRepoMock.Object, ruleRepoMock.Object);
            
            // Add a rule to delete
            var rule = new Rule { Id = 5, Pattern = "Test", ProjectId = 1 };
            viewModel.Rules.Add(rule);
            
            // Act: Execute delete command
            viewModel.DeleteRuleCommand.Execute(rule);
            
            // Let async operations complete
            await Task.Delay(50);
            
            // Assert: Rule should be removed from collection
            Assert.DoesNotContain(rule, viewModel.Rules);
        }
    }
} 
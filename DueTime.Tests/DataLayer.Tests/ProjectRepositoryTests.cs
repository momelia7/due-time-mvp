using System.Collections.Generic;
using System.Threading.Tasks;
using DueTime.Data;
using Xunit;

namespace DueTime.Tests.DataLayer
{
    public class ProjectRepositoryTests
    {
        [Fact]
        public async Task GetAllProjectsAsync_ReturnsList()
        {
            // Arrange
            var repo = new SQLiteProjectRepository();
            
            // Act
            List<Project> projects = await repo.GetAllProjectsAsync();
            
            // Assert: the result should not be null (even if empty)
            Assert.NotNull(projects);
        }

        [Fact]
        public async Task AddProjectAsync_AddsProjectSuccessfully()
        {
            // Arrange
            var repo = new SQLiteProjectRepository();
            
            // Act
            int projectId = await repo.AddProjectAsync("TestProject");
            
            // Assert: placeholder (projectId should be non-negative if insertion succeeded)
            Assert.True(projectId >= 0);
        }
    }
} 
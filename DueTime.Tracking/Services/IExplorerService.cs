using System.Diagnostics.CodeAnalysis;

namespace DueTime.Tracking.Services
{
    /// <summary>
    /// Service to retrieve the path of the currently active File Explorer window.
    /// </summary>
    public interface IExplorerService
    {
        /// <returns>The full folder path of the active Explorer window, or null if none.</returns>
        string? GetActiveExplorerPath();
    }
} 
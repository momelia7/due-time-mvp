using System;
using System.Windows.Controls;
using DueTime.Data;

namespace DueTime.UI.Views
{
    public partial class DashboardView : System.Windows.Controls.UserControl
    {
        public DateTime Date { get; set; } = DateTime.Today;
        
        public DashboardView()
        {
            InitializeComponent();
            DataContext = this;
            
            // Register event handlers
            EntriesDataGrid.CellEditEnding += EntriesDataGrid_CellEditEnding;
        }
        
        private void EntriesDataGrid_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit && e.Column == ProjectColumn)
            {
                if (e.Row.Item is TimeEntry entry)
                {
                    // Update the database with the new project assignment
                    AppState.EntryRepo.UpdateEntryProjectAsync(entry.Id, entry.ProjectId).Wait();
                }
            }
        }
    }
} 
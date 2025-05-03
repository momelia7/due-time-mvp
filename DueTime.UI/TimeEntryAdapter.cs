using System;
using DueTime.Data;
using DueTime.Tracking;

namespace DueTime.UI
{
    /// <summary>
    /// Adapter class to handle conversion between Tracking.TimeEntry and Data.TimeEntry.
    /// </summary>
    public static class TimeEntryAdapter
    {
        public static DueTime.Data.TimeEntry ConvertToDataModel(DueTime.Tracking.TimeEntry trackingEntry)
        {
            return new DueTime.Data.TimeEntry
            {
                Id = trackingEntry.Id,
                StartTime = trackingEntry.StartTime,
                EndTime = trackingEntry.EndTime,
                WindowTitle = trackingEntry.WindowTitle,
                ApplicationName = trackingEntry.ApplicationName,
                ProjectId = trackingEntry.ProjectId
            };
        }

        public static DueTime.Tracking.TimeEntry ConvertToTrackingModel(DueTime.Data.TimeEntry dataEntry)
        {
            return new DueTime.Tracking.TimeEntry
            {
                Id = dataEntry.Id,
                StartTime = dataEntry.StartTime,
                EndTime = dataEntry.EndTime,
                WindowTitle = dataEntry.WindowTitle,
                ApplicationName = dataEntry.ApplicationName,
                ProjectId = dataEntry.ProjectId,
                ProjectName = dataEntry.ProjectName
            };
        }
    }
} 
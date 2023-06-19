using Microsoft.Graph;

namespace CalendarIntegrationApi
{
    /// <summary>
    /// Gets data for the wner and Join meeting Id from CalendarEventsCollectionPage
    /// </summary>
    public class CalendarEventHelper
    {
        /// <summary>
        /// Organizer Id
        /// </summary>
        public string? OrganizerId { get; set; }

        /// <summary>
        /// Join Meeting JoinWebUrl
        /// </summary>
        public string JoinWebUrl { get; set; }

        /// <summary>
        /// Join Meeting Id
        /// </summary>
        public string MeetingId { get; set; }

        /// <summary>
        /// Attendance Information
        /// </summary>
        public AttendanceInformation AttendanceInformation { get; set; }

        /// <summary>
        /// List of Meetings
        /// </summary>
        public List<OnlineMeeting> OnlineMeetings { get; set; }

        /// <summary>
        /// Collection of categories
        /// </summary>
        public IEnumerable<string> Categories { get; set; }

    }
}
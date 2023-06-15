
using Microsoft.Graph;

namespace CalendarIntegrationApi
{
    /// <summary>
    /// Providers user attendance information
    /// </summary>
    public class AttendanceRecordInformation
    {
        /// <summary>
        /// Time the user attended the meting in seconds 
        /// </summary>
        public IMeetingAttendanceReportAttendanceRecordsCollectionPage AttendanceRecords { get; set; }

        /// <summary>
        /// Information regarding the meeting
        /// </summary>
        public MeetingInformation Meeting {  get; set; }
    }
}

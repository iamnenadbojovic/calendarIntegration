﻿
using Microsoft.Graph;

namespace CalendarIntegrationApi
{
    /// <summary>
    /// Providers user attendance information
    /// </summary>
    public class AttendanceInformation
    {
        /// <summary>
        /// Provides Information for Meeting Attendees
        /// </summary>
        public AtendeeInformation Atendee { get; set; }

        /// <summary>
        /// Information regarding the meeting
        /// </summary>
        public MeetingInformation Meeting { get; set; }
    }

    /// <summary>
    /// Provides Information for Meeting Attendees
    /// </summary>
    public class AtendeeInformation
    {
        /// <summary>
        /// Time the user attended the meting in seconds 
        /// </summary>
        public object? AttendanceIntervals { get; set; }

        /// <summary>
        /// Join date time
        /// </summary>
        public DateTimeOffset? JoinDateTime { get; set; }

        /// <summary>
        /// Leave date time
        /// </summary>
        public DateTimeOffset? LeaveDateTime { get; set; }

        /// <summary>
        /// Attendee Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Attendance Status
        /// </summary>
        public ResponseType? ResponseType { get; set; }

        /// <summary>
        /// Attendance Type
        /// </summary>
        public AttendeeType? Type { get; set; }

    }
}

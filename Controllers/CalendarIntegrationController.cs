using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Linq.Expressions;

namespace CalendarIntegrationApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CalendarIntegrationController : ControllerBase
    {

        private readonly ILogger<CalendarIntegrationController> _logger;
        private readonly GraphServiceClient _graphServiceClient;

        public CalendarIntegrationController(ILogger<CalendarIntegrationController> logger, GraphServiceClient graphServiceClient)
        {
            _logger = logger;
            _graphServiceClient = graphServiceClient;
        }

        [HttpGet(Name = "GetCalendarIntegration"), Route("user")]
        public async Task<ActionResult> Get(string email)
        {
            var users = await GetUsers(_graphServiceClient);
            if (users == null)
            {
                return BadRequest("No users in system");
            }

            var employee = users.Where(a => a.Mail == email).FirstOrDefault();

            if (employee == null)
            {
                return BadRequest("No such employee exists");
            }

            var attendanceResult = new List<CalendarEventHelper>();

            var CalendarEventHelpers = await GetUserCalendarEvents(_graphServiceClient, employee, users);

            foreach (var calendarEventHelper in CalendarEventHelpers)
            {
                var onlineMeetings = await _graphServiceClient.Users[calendarEventHelper.OrganizerId].OnlineMeetings
                    .Request().Filter($"joinMeetingIdSettings/joinMeetingId eq '{calendarEventHelper.JoinMeetingId}'")
                    .WithAppOnly().GetAsync();

                if (onlineMeetings.IsNullOrEmpty())
                {
                    return BadRequest("There are no Online Meetings for this User");
                }

                var onlineMeeting = onlineMeetings[0];

                var attendanceReports = await GetMeetingAttendanceReportsEvents(_graphServiceClient, calendarEventHelper.OrganizerId, onlineMeeting.Id);

                foreach (var attendanceReport in attendanceReports)
                {
                    var attendanceRecords = await GetMeetingAttendanceRecords(_graphServiceClient,
                        calendarEventHelper.OrganizerId, onlineMeeting.Id, attendanceReport.Id);


                    foreach (var attendanceRecord in attendanceRecords)
                    {
                        // automapper
                        if (attendanceRecord.EmailAddress == email)
                        {
                            calendarEventHelper.AttendanceInformation.Atendee.AttendanceInterval = attendanceRecord.TotalAttendanceInSeconds;
                            calendarEventHelper.AttendanceInformation.Meeting = new MeetingInformation
                            {
                                Categories = calendarEventHelper.Categories,
                                StartDateTime = onlineMeeting.StartDateTime,
                                EndDateTime = onlineMeeting.EndDateTime,
                                Subject = onlineMeeting.Subject,
                            };
                        };
                        attendanceResult.Add(calendarEventHelper);
                    }

                }
            }

            return Ok(attendanceResult);
        }

        [HttpGet(Name = "GetCalendarIntegrationAll"), Route("events")]
        public async Task<ActionResult> Get()
        {
            var users = await GetUsers(_graphServiceClient);
            if (users == null)
            {
                return BadRequest("No users in system");
            }

            var CalendarEventHelpers = new List<CalendarEventHelper>();
            var attendanceResult = new List<CalendarEventHelper>();
            foreach (var employee in users)
            {
                var EmployeeUserEvents = await GetUserCalendarEvents(_graphServiceClient, employee, users);
                CalendarEventHelpers = CalendarEventHelpers.Union(EmployeeUserEvents).Distinct().ToList();

                foreach (var calendarEventHelper in CalendarEventHelpers)
                {
                    var onlineMeetings = await _graphServiceClient.Users[calendarEventHelper.OrganizerId].OnlineMeetings
                        .Request().Filter($"joinMeetingIdSettings/joinMeetingId eq '{calendarEventHelper.JoinMeetingId}'")
                        .WithAppOnly().GetAsync();

                    if (onlineMeetings.IsNullOrEmpty())
                    {
                        return BadRequest("There are no Online Meetings for this User");
                    }

                    var onlineMeeting = onlineMeetings[0];

                    var attendanceReports = await GetMeetingAttendanceReportsEvents(_graphServiceClient, calendarEventHelper.OrganizerId, onlineMeeting.Id);

                    foreach (var attendanceReport in attendanceReports)
                    {
                        var attendanceRecords = await GetMeetingAttendanceRecords(_graphServiceClient,
                            calendarEventHelper.OrganizerId, onlineMeeting.Id, attendanceReport.Id);

                        foreach (var attendanceRecord in attendanceRecords)
                        {
                            // automapper
                            if (attendanceRecord.EmailAddress == employee.Mail)
                            {
                                calendarEventHelper.AttendanceInformation.Atendee.Name = employee.DisplayName;
                                calendarEventHelper.AttendanceInformation.Atendee.AttendanceInterval = attendanceRecord.TotalAttendanceInSeconds;
                                calendarEventHelper.AttendanceInformation.Meeting = new MeetingInformation
                                {
                                    Categories = calendarEventHelper.Categories,
                                    StartDateTime = onlineMeeting.StartDateTime,
                                    EndDateTime = onlineMeeting.EndDateTime,
                                    Subject = onlineMeeting.Subject,
                                };
                            };
                            attendanceResult.Add(calendarEventHelper);
                        }
                    }
                }
            }
            return Ok(attendanceResult);
        }

        /// <summary>
        /// Parses meeting id from the meeting event htmlContent
        /// </summary>
        /// <param name="eventsPage">Event Object</param>
        /// <returns>Meeting Id string value</returns>
        public static string MeetingId(Event eventsPage)
        {
            string result = string.Empty;
            try
            {
                var htmlContent = eventsPage.Body.Content;
                var substringLeft = htmlContent[(htmlContent.IndexOf(">Meeting ID:") + 15)..];
                var substringSecond = substringLeft.Substring(substringLeft.IndexOf(">") + 1, substringLeft.IndexOf("<") - substringLeft.IndexOf(">") - 1);
                result = string.Concat(substringSecond.Where(c => !char.IsWhiteSpace(c)));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return result;
        }

        /// <summary>
        /// Retrieve the list of CalendarEvents from all collection pages
        /// </summary>
        /// <param name="graphClient">Graph Client Object</param>
        /// <param name="employee">User Object</param>
        /// <param name="users">List of Users</param>
        /// <returns></returns>
        public static async Task<List<CalendarEventHelper>> GetUserCalendarEvents(GraphServiceClient graphClient, User employee, List<User> users)
        {
            var CalendarEventHelpers = new List<CalendarEventHelper>();

            foreach (var user in users)
            {
                var eventsPage = await graphClient.Users[employee.Id].Calendar.Events.Request()// svi eventovi, ovde bi trebao filter za vreme, ne tresa da se uzimaju svi eventi
                .WithAppOnly().GetAsync();

                foreach (var calendarEvent in eventsPage)
                {
                    if (user.UserPrincipalName == calendarEvent.Organizer.EmailAddress.Address && calendarEvent.IsOnlineMeeting == true)
                    {
                        var attendanceInformation = new AttendanceInformation
                        {
                            Atendee = new AtendeeInformation()
                            {
                                Name = employee.DisplayName,
                                ResponseType = calendarEvent.Attendees.Where(a => a.EmailAddress.Address == employee.Mail)?
                                .FirstOrDefault()?.Status?.Response,
                                Type = calendarEvent.Attendees.Where(a => a.EmailAddress.Address == employee.Mail)?
                                .FirstOrDefault()?.Type
                            }
                        };
                        var calendarEventHelper = new CalendarEventHelper()
                        {
                            OrganizerId = users?.FirstOrDefault(user => user.UserPrincipalName == calendarEvent.Organizer.EmailAddress.Address)?.Id,
                            JoinMeetingId = MeetingId(calendarEvent),
                            Categories = calendarEvent.Categories,
                            AttendanceInformation = attendanceInformation,
                        };

                        CalendarEventHelpers.Add(calendarEventHelper);
                    }
                }
            }
            return CalendarEventHelpers;
        }

        /// <summary>
        /// Gets User events from all the pages
        /// </summary>
        /// <param name="graphClient">GraphServiceClient Object</param>
        /// <returns>Task of List<User></returns>
        public static async Task<List<User>> GetUsers(GraphServiceClient graphClient)
        {
            var usersList = new List<User>();
            IGraphServiceUsersCollectionPage users = await graphClient.Users
                .Request().WithAppOnly()
                .GetAsync();

            usersList.AddRange(users.CurrentPage);

            while (users.NextPageRequest != null)
            {
                users = await users.NextPageRequest.GetAsync();
                usersList.AddRange(users.CurrentPage);
            }
            return usersList;
        }

        /// <summary>
        /// Gets Calendar events from all the pages
        /// </summary>
        /// <param name="graphClient">GraphServiceClient Object</param>
        /// <param name="id">user Id</param>
        /// <returns>Task of List<Event></returns>
        public static async Task<List<Event>> GetCalendarEvents(GraphServiceClient graphClient, string id)
        {
            var eventsList = new List<Event>();
            ICalendarEventsCollectionPage events = await graphClient.Users[id].Calendar.Events.Request() // svi eventovi, ovde bi trebao filter za vreme, ne tresa da se uzimaju svi eventi
                    .WithAppOnly().GetAsync();

            eventsList.AddRange(events.CurrentPage);

            while (events.NextPageRequest != null)
            {
                events = await events.NextPageRequest.GetAsync();
                eventsList.AddRange(events.CurrentPage);
            }
            return eventsList;
        }

        /// <summary>
        /// Gets MeetingAttendanceReports from all the pages
        /// </summary>
        /// <param name="graphClient">GraphServiceClient Object</param>
        /// <param name="organizerId">Organizer Id</param>
        /// <param name="onlineMeetingId">Online Meeting Id</param>
        /// <returns></returns>
        public static async Task<List<MeetingAttendanceReport>> GetMeetingAttendanceReportsEvents
            (GraphServiceClient graphClient, string organizerId, string onlineMeetingId)
        {
            var meetingAttendanceReportsList = new List<MeetingAttendanceReport>();
            var meetingAttendanceReports = await graphClient
                             .Users[organizerId]
                             .OnlineMeetings[onlineMeetingId].AttendanceReports.Request().
                             WithAppOnly().GetAsync();

            meetingAttendanceReportsList.AddRange(meetingAttendanceReports.CurrentPage);

            while (meetingAttendanceReports.NextPageRequest != null)
            {
                meetingAttendanceReports = await meetingAttendanceReports.NextPageRequest.GetAsync();
                meetingAttendanceReportsList.AddRange(meetingAttendanceReports.CurrentPage);
            }
            return meetingAttendanceReportsList;
        }
        // generics?
        /// <summary>
        /// Gets MeetingAttendanceReports from all the pages
        /// </summary>
        /// <param name="graphClient">GraphServiceClient Object</param>
        /// <param name="organizerId">Organizer Id</param>
        /// <param name="onlineMeetingId">Online Meeting Id</param>
        /// <returns></returns>
        public static async Task<List<AttendanceRecord>> GetMeetingAttendanceRecords
            (GraphServiceClient graphClient, string organizerId, string onlineMeetingId, string attendanceReportId)
        {
            var meetingAttendanceRecordsList = new List<AttendanceRecord>();
            var meetingAttendanceRecords = await graphClient
                            .Users[organizerId]
                            .OnlineMeetings[onlineMeetingId]
                            .AttendanceReports[attendanceReportId]
                            .AttendanceRecords.Request().WithAppOnly().GetAsync();

            meetingAttendanceRecordsList.AddRange(meetingAttendanceRecords.CurrentPage);

            while (meetingAttendanceRecords.NextPageRequest != null)
            {
                meetingAttendanceRecords = await meetingAttendanceRecords.NextPageRequest.GetAsync();
                meetingAttendanceRecordsList.AddRange(meetingAttendanceRecords.CurrentPage);
            }
            return meetingAttendanceRecordsList;
        }
    }
}
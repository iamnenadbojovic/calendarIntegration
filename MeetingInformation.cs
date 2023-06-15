using System;

public class MeetingInformation
{
    /// <summary>
    /// Meeting start time
    /// </summary>
    public DateTimeOffset? StartDateTime { get; set; }

    /// <summary>
    /// Meeting end time
    /// </summary>
    public DateTimeOffset? EndDateTime { get; set; }

    /// <summary>
    /// Meeting Subject
    /// </summary>
    public string Subject { get; set; }

    /// <summary>
    /// Collection of categories
    /// </summary>
    public IEnumerable<string> Categories { get; set; }

}

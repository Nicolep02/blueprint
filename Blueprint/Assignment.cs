public class Assignment
{
    public string? Name { get; set; }
    public DateTime? Due_At { get; set; } // Make it nullable for assignments with no due date
    public string? Html_Url { get; set; }
    public string? CourseID { get; set; }
    public string UniqueId => $"{CourseID}-{Name}".Replace(" ", " ").ToLower();
}

public class EdThread
{
    public long Id { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public bool IsPinned { get; set; }
}

public class EdApiResponse
{
    public List<EdThread>? Threads { get; set; }
}
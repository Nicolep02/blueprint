using Newtonsoft.Json;

public class Assignment
{
    public string? Name { get; set; }
    public DateTime? Due_At { get; set; } // Make it nullable for assignments with no due date
    public string? Html_Url { get; set; }
    public string? CourseID { get; set; }
    public string UniqueId => $"{CourseID}-{Name}".Replace(" ", "").ToLower();

    public override string ToString()
    {
        return $"  Name: {Name}\n  Due_At: {Due_At}\n  Html_Url: {Html_Url}\n  CourseID: {CourseID}\n  UniqueId: {UniqueId}\n";
    }
}

public class EdThread
{
    public long Id { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public bool IsPinned { get; set; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }
}

public class EdApiResponse
{
    public List<EdThread>? Threads { get; set; }
}
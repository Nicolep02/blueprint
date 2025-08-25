using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class EdStemService
{
    // Use one HttpClient for efficiency, just like in the CanvasService
    private static readonly HttpClient client = new HttpClient();

    public async Task<List<Assignment>> FetchAssignmentsAsync(List<string> courseIds)
    {
        var allAssignments = new List<Assignment>();
        var edToken = Environment.GetEnvironmentVariable("ED_TOKEN");

        if (string.IsNullOrEmpty(edToken))
        {
            Console.WriteLine("Error: ED_TOKEN environment variable not set.");
            return allAssignments;
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", edToken);

        foreach (var courseId in courseIds)
        {
            Console.WriteLine($"\nFetching threads for Ed Stem course {courseId}...");
            var response = await client.GetAsync($"https://us.edstem.org/api/courses/{courseId}/threads");

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<EdApiResponse>(json);

                if (apiResponse?.Threads != null)
                {
                    var titleKeywords = new Regex("Week|Weekly|Announcement|Logistics", RegexOptions.IgnoreCase);

                    // FIX: Find ALL matching posts, sort by date, then take the newest one.
                    var announcementThread = apiResponse.Threads
                        .Where(t => t.Title != null && titleKeywords.IsMatch(t.Title))
                        .OrderByDescending(t => t.CreatedAt)
                        .FirstOrDefault();

                    if (announcementThread?.Content != null)
                    {
                        Console.WriteLine($"Found matching post: '{announcementThread.Title}'. Parsing content...");
                        // Pass the courseId to the parser
                        var parsedAssignments = ParseEdStemText(announcementThread.Content, courseId);
                        allAssignments.AddRange(parsedAssignments);
                    }
                    else
                    {
                        Console.WriteLine($"Could not find a weekly announcement post in course {courseId}.");
                    }
                }
            }
            else
            {
                Console.WriteLine($"Error fetching from Ed Stem API for course {courseId}: {response.StatusCode}");
            }
        }

        return allAssignments;
    }

    //helper function, now private because only this class needs it.
    public List<Assignment> ParseEdStemText(string postContent, string courseId)
    {
        var assignments = new List<Assignment>();
        var pattern = new Regex(@"(?<name>Lab \d+|Homework \d+|Project \d+|Discussion \d+).*?(is )?due (?<date>.*?PST)");

        foreach (Match match in pattern.Matches(postContent))
        {
            string name = match.Groups["name"].Value.Trim();
            string dateRaw = match.Groups["date"].Value.Trim();
            // We need to add the year. Let's assume the current year for Ed posts.
            string dateStr = dateRaw.Replace("PST", "").Trim();
            dateStr = dateStr.Replace("@ ", "");
            dateStr = $"{dateStr} {DateTime.Now.Year}";

            // Note: We remove "th", "st", "nd", "rd" to make parsing easier.
            dateStr = dateStr.Replace("th", "").Replace("st", "").Replace("nd", "").Replace("rd", "");
            if (DateTime.TryParseExact(dateStr, "ddd M/d h:mmtt yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime dueDate))
            {
                assignments.Add(new Assignment
                {
                    Name = $"Ed: {name}",
                    Due_At = dueDate.ToLocalTime(),
                    CourseID = $"ed-{courseId}",
                    Html_Url = $"https://edstem.org/us/courses/{courseId}/discussion" 
                });
            }
        }
        Console.WriteLine($"Successfully parsed {assignments.Count} assignments from Ed Stem post.");
        return assignments;
    }
}
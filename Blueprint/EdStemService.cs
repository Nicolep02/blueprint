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
                    Console.WriteLine($"Found {apiResponse.Threads.Count} threads to scan...");
                    // NEW LOGIC: Loop through EVERY thread and try to parse it.
                    foreach (var thread in apiResponse.Threads)
                    {   
                        Console.WriteLine($"\n--- Scanning Thread: '{thread.Title}' ---");
                        if (!string.IsNullOrEmpty(thread.Content))
                        {
                            var parsedAssignments = ParseEdStemText(thread.Content, courseId, thread.Title);
                            if (parsedAssignments.Any())
                            {
                                allAssignments.AddRange(parsedAssignments);
                            }
                        }
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
    private List<Assignment> ParseEdStemText(string postContent, string courseId, string threadTitle)
    {
        var assignments = new List<Assignment>();
        // remove html tags
        string cleanContent = Regex.Replace(postContent, "<.*?>", string.Empty);
        // MORE FLEXIBLE PATTERN: Looks for "due" and captures what's after it.
        var datePattern = new Regex(
            @"(?<date>(?:Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday), (?:January|February|March|April|May|June|July|August|September|October|November|December) \d{1,2}) at (?<time>\d{1,2}:\d{2}\s*(?:AM|PM)) PT",
            RegexOptions.IgnoreCase);
        var matches = datePattern.Matches(cleanContent);
        int assignmentCounter = 0;

        Console.WriteLine($"Found {matches.Count} potential deadlines in the post.");

        foreach (Match match in matches)
        {
            // Get the date text
            string dateStr = $"{match.Groups["date"].Value} {match.Groups["time"].Value} {DateTime.Now.Year}";

            // Console.WriteLine($"--- Character Analysis for '{dateStr}' ---");
            // Console.WriteLine($"String Length: {dateStr.Length}");
            // for (int i = 0; i < dateStr.Length; i++)
            // {
            //     char c = dateStr[i];
            //     Console.WriteLine($"Index {i}: '{c}' (ASCII: {(int)c})");
            // }
            // Console.WriteLine("------------------------------------------");
            // // --- END DEBUG ---
    
            if (DateTime.TryParseExact(dateStr, "dddd, MMMM d h:mm tt yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime dueDate))
            {
                string assignmentName = threadTitle;
                if (matches.Count > 1)
                {
                    assignmentName = $"{threadTitle} {(char)('a' + assignmentCounter)}";
                    assignmentCounter += 1;
                }
                // We look for context in the CLEANED content as well
                    int contextPosition = Math.Max(0, match.Index - 150);
                int contextLength = Math.Min(150, match.Index - contextPosition);
                string context = cleanContent.Substring(contextPosition, contextLength);

                assignments.Add(new Assignment
                {
                    Name = $"Ed: {assignmentName}",
                    Due_At = dueDate.ToLocalTime(),
                    CourseID = $"ed-{courseId}",
                    Html_Url = $"https://edstem.org/us/courses/{courseId}/discussion"
                });
            }
        }
        // checking what assignment gets parsed
        if (assignments.Any())
        {
            Console.WriteLine("\n--- Parsed Assignment Details ---");
            foreach (Assignment assignment in assignments)
            {
                // call overridden ToString() method 
                Console.WriteLine(assignment);
            }
            Console.WriteLine("---------------------------------");
        }
        Console.WriteLine($"Parsed {assignments.Count} assignments from Ed Stem post.");
        return assignments;
    }
}
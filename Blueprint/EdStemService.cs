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

    public async Task<List<Assignment>> FetchAssignmentsAsync(string courseId)
    {
        var assignments = new List<Assignment>();
        var edToken = Environment.GetEnvironmentVariable("ED_TOKEN");

        if (string.IsNullOrEmpty(edToken))
        {
            Console.WriteLine("Error: ED_TOKEN environment variable not set.");
            return assignments;
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", edToken);

        Console.WriteLine("\nFetching threads from Ed Stem API...");
        var response = await client.GetAsync($"https://us.edstem.org/api/courses/{courseId}/threads");

        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<EdApiResponse>(json);

            if (apiResponse?.Threads != null)
            {
                // TEMPORARY DEBUGGING CODE
                Console.WriteLine("\n--- All Thread Titles Found via API---");
                foreach (var thread in apiResponse.Threads)
                {
                    Console.WriteLine(thread.Title);
                }
                Console.WriteLine("End of EdSTEM grab");
                //---END OF TEMP DEBUG
                // Find the relevant announcement post by scanning titles for keywords
                var titleKeywords = new Regex("Week|Weekly|Announcement|Announcements|Logistics", RegexOptions.IgnoreCase);
                var announcementThread = apiResponse.Threads.FirstOrDefault(t => t.Title != null && titleKeywords.IsMatch(t.Title));

                if (announcementThread?.Content != null)
                {
                    Console.WriteLine($"Found matching post: '{announcementThread.Title}'. Parsing content...");
                    // We reuse our existing parser!
                    assignments = ParseEdStemText(announcementThread.Content);
                }
                else
                {
                    Console.WriteLine("Could not find a weekly announcement post via API.");
                }
            }
        }
        else
        {
            Console.WriteLine($"Error fetching from Ed Stem API: {response.StatusCode}");
        }
        
        return assignments;
    }

    // This is our helper function, now private because only this class needs it.
    private List<Assignment> ParseEdStemText(string postContent)
    {
        var assignments = new List<Assignment>();
        var pattern = new Regex(@"(?<name>Lab \d+|Homework \d+|Project \d+|Discussion \d+).*?due (?<date>.*?PST)");

        foreach (Match match in pattern.Matches(postContent))
        {
            string name = match.Groups["name"].Value.Trim();
            string dateRaw = match.Groups["date"].Value.Trim();
            // We need to add the year. Let's assume the current year for Ed posts.
            string dateStr = $"{dateRaw} {DateTime.Now.Year}";

            // Note: We remove "th", "st", "nd", "rd" to make parsing easier.
            dateStr = dateStr.Replace("th", "").Replace("st", "").Replace("nd", "").Replace("rd", "");
            if (DateTime.TryParse(dateStr, System.Globalization.CultureInfo.InvariantCulture, out DateTime dueDate))
            {
                assignments.Add(new Assignment
                {
                    Name = $"Ed: {name}",
                    Due_At = dueDate.ToLocalTime(),
                    // Update this with the correct course URL for EdStem
                    Html_Url = "https://edstem.org/us/courses/60131/discussion/" 
                });
            }
        }
        Console.WriteLine($"Successfully parsed {assignments.Count} assignments from Ed Stem post.");
        return assignments;
    }
}
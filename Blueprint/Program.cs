using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

// --- 1. Structure for an Assignment ---
public class Assignment
{
    public string Name { get; set; }
    public DateTime? Due_At { get; set; } // Make it nullable for assignments with no due date
    public string Html_Url { get; set; }
}

// --- 2. Main Program Logic ---
class Program
{
    static async Task Main(string[] args)
    {
        // --- CONFIGURATION ---
        var canvasUrl = "https://bcourses.berkeley.edu/"; // IMPORTANT: Replace with your school's Canvas URL
        var apiToken = "1072~eYheLMfPYU6uJTfMaxGGDkFhXJyWrLrQvMT3WzY74UxDfucJZk87hXuCcJDTRM9w";           // IMPORTANT: Paste your secret token here
        var courseId = "1545401";           // IMPORTANT: Find this in your course URL. testrun course: "Critical Studies in Education"

        // --- CANVAS API CONNECTION ---
        HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

        Console.WriteLine("Fetching assignments from Canvas...");
        var response = await client.GetAsync($"{canvasUrl}/api/v1/courses/{courseId}/assignments");

        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            List<Assignment> assignments = JsonConvert.DeserializeObject<List<Assignment>>(json);

            Console.WriteLine("\n--- Your Assignments ---");
            foreach (var assignment in assignments)
            {
                // Only print assignments that have a due date
                if (assignment.Due_At.HasValue)
                {
                    Console.WriteLine($"Name: {assignment.Name}");
                    Console.WriteLine($"Due: {assignment.Due_At.Value.ToLocalTime()}");
                    Console.WriteLine($"Link: {assignment.Html_Url}\n");
                }
            }
        }
        else
        {
            Console.WriteLine("Error fetching data: " + response.StatusCode);
        }
    }
}
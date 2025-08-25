using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

public class CanvasService
{
    private static readonly HttpClient client = new HttpClient();
    public async Task<List<Assignment>> FetchAssignmentsAsync(List<string> courseIDs)
    {
        // --- CONFIGURATION ---
        var canvasUrl = "https://bcourses.berkeley.edu/"; // IMPORTANT: Replace with your school's Canvas URL
        var apiToken = Environment.GetEnvironmentVariable("CANVAS_TOKEN");

        // List of assignments from all Courses
        List<Assignment> allAssignments = new List<Assignment>();

        // --- CANVAS API CONNECTION ---
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

        Console.WriteLine("Fetching assignments from Canvas...");
        foreach (var courseId in courseIDs)
        {
            var response = await client.GetAsync($"{canvasUrl}/api/v1/courses/{courseId}/assignments");

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                List<Assignment>? courseAssignments = JsonConvert.DeserializeObject<List<Assignment>>(json);

                // Add the fetched assignments to our main list
                if (courseAssignments != null)
                {
                    allAssignments.AddRange(courseAssignments);
                    Console.WriteLine($"Successfully fetched {courseAssignments.Count} assignments from course {courseId}.");
                }
            }
            else
            {
                Console.WriteLine($"Error fetching data for course {courseId}: " + response.StatusCode);
            }
        }
        return allAssignments;
    }
}
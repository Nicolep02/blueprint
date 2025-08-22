using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static async Task Main(string[] args)
    {
        // --- 1. SETUP ---
        // Create instances of our services
        var canvasService = new CanvasService();
        var edStemService = new EdStemService();

        // Define the configuration for our services
        List<string> canvasCourseIDs = new List<string> { "1545401", "1545472" };
        var edStemCourseId = "60131"; // Your Ed Stem Course ID


        // --- 2. FETCH DATA ---
        // Call each service to get the assignments
        var canvasAssignments = await canvasService.FetchAssignmentsAsync(canvasCourseIDs);
        var edStemAssignments = await edStemService.FetchAssignmentsAsync(edStemCourseId);


        // --- 3. COMBINE AND PROCESS ---
        // Combine the results from all services into a single list
        List<Assignment> allAssignments = new List<Assignment>();
        allAssignments.AddRange(canvasAssignments);
        allAssignments.AddRange(edStemAssignments);

        Console.WriteLine("\n--- Your Upcoming Assignments (All Courses) ---");
        
        // Filter out any assignments that don't have a due date and sort the rest
        var sortedAssignments = allAssignments
            .Where(a => a.Due_At.HasValue)
            .OrderBy(a => a.Due_At);

        // Print the final, sorted list to the console
        foreach (var assignment in sortedAssignments)
        {
            Console.WriteLine($"Name: {assignment.Name}");
            Console.WriteLine($"Due: {assignment.Due_At.Value.ToLocalTime()}");
            Console.WriteLine($"Link: {assignment.Html_Url}\n");
        }


        // --- 4. SAVE RESULTS ---
        // Save the final, sorted list to a JSON file
        await SaveAssignmentsToFile(sortedAssignments);
    }

    static async Task SaveAssignmentsToFile(IEnumerable<Assignment> assignments)
    {
        string outputDirectory = AppContext.BaseDirectory;
        string filePath = Path.Combine(outputDirectory, "assignments.json");
        string jsonString = JsonConvert.SerializeObject(assignments, Formatting.Indented);
        await File.WriteAllTextAsync(filePath, jsonString);
        Console.WriteLine($"\nSuccessfully saved assignments to: {filePath}");
    }
}
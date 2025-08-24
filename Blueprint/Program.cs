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
        List<string> canvasCourseIDs = new List<string> { "1545401", "1545472" }; //  canvas course ID
        var edStemCourseId = "80131"; // Your Ed Stem Course ID


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

        // --- 4. Connect to Google Calendar and Create Events ---
        var calendarService = new GoogleCalendarService();
        Console.WriteLine("\nAuthorizing with Google Calendar...");
        await calendarService.AuthorizeAsync();

        // TEST event add to calendar
        Console.WriteLine("Creating a test assignment object");
        var testAssignment = new Assignment
        {
            Name = "Test Event from My Comp",
            Html_Url = "http://example.com",
            // due tmr 5pm
            Due_At = DateTime.Now.Date.AddDays(1).AddHours(17)
        };

        // ADDING test to calendar
        Console.WriteLine("Adding the test event to calendar");
        var eventId = await calendarService.CreateEventAsync(testAssignment);
        if (eventId != null)
        {
            Console.WriteLine("Success! test event created");
            Console.WriteLine("check calendar for tmr 5pm");
        }
        else
        {
            Console.WriteLine("failure. test event couldn't be created");
        }

        // get existing events on calendar
            // var existingEvents = await calendarService.GetExistingEventsAsync();

            // bool isDryRun = false; //testing what to be added

            // Console.WriteLine("Syncing assignments with your calendar...");
            // if (isDryRun)
            // {
            //     Console.WriteLine("--- THIS IS A DRY RUN. NO CHANGES WILL BE MADE. ---");
            // }
            // foreach (var assignment in sortedAssignments)
            // {
            //     // Check if we've already created an event for this assignment
            //     if (existingEvents.ContainsKey(assignment.UniqueId))
            //     {
            //         // Logic for an existing event
            //         var existingEvent = existingEvents[assignment.UniqueId];
            //         bool needsUpdate = existingEvent.End.DateTimeDateTimeOffset != assignment.Due_At!.Value;

            //         if (needsUpdate)
            //         {
            //             if (isDryRun)
            //             {
            //                 Console.WriteLine($"[DRY RUN] Would UPDATE event for: {assignment.Name}");
            //             }
            //             else
            //             {
            //                 await calendarService.UpdateEventAsync(existingEvent, assignment);
            //             }
            //         }
            //     }
            //     else
            //     {
            //         // Logic for a new event
            //         if (isDryRun)
            //         {
            //             Console.WriteLine($"[DRY RUN] Would CREATE event for: {assignment.Name}");
            //         }
            //         else
            //         {
            //             await calendarService.CreateEventAsync(assignment);
            //             Console.WriteLine($"Created new calendar event for: {assignment.Name}");
            //         }
            //     }
            // }

            // Console.WriteLine("\nSync complete.");

            // // Print the final, sorted list to the console
            // foreach (var assignment in sortedAssignments)
            // {
            //     Console.WriteLine($"Name: {assignment.Name}");
            //     Console.WriteLine($"Due: {assignment.Due_At.Value.ToLocalTime()}");
            //     Console.WriteLine($"Link: {assignment.Html_Url}\n");
            // }

            // --- LAST. SAVE RESULTS ---
            // Save the final, sorted list to a JSON file
            // await SaveAssignmentsToFile(sortedAssignments);
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
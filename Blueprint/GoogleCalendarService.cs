using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

public class GoogleCalendarService
{
    private static readonly string[] Scopes = { CalendarService.Scope.Calendar };
    private CalendarService? _service;

    public async Task TestConnectionAsync()
    {
        if (_service == null)
        {
            Console.WriteLine("Service is not authorized.");
            return;
        }

        try
        {
            // Make a simple request to get the details of the primary calendar
            var calendar = await _service.Calendars.Get("primary").ExecuteAsync();
            Console.WriteLine($"Successfully connected to calendar: {calendar.Summary}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while testing the connection: {ex.Message}");
        }
    }
    public async Task AuthorizeAsync()
    {
        UserCredential credential;
        var credentialsPath = Path.Combine(AppContext.BaseDirectory, "credentials.json");

        await using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
        {
            var clientSecrets = await GoogleClientSecrets.FromStreamAsync(stream);
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets.Secrets,
                Scopes,
                "user",
                CancellationToken.None);
        }

        _service = new CalendarService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "Blueprint Assignment Scraper",
        });
    }

    // Fetch future events created
    public async Task<Dictionary<string, Event>> GetExistingEventsAsync()
    {
        var existingEvents = new Dictionary<string, Event>();
        if (_service == null) return existingEvents;

        var request = _service.Events.List("primary");
        request.TimeMinDateTimeOffset = DateTime.Now.AddDays(-14); // Get events from the past 14 days forward
        request.ShowDeleted = false;
        request.SingleEvents = true;

        // To find events created by this only
        request.PrivateExtendedProperty = "source=blueprint_scraper";
        var events = await request.ExecuteAsync();
        foreach (var calendarEvent in events.Items)
        {
            //unique ID stored when creating event
            if (calendarEvent.ExtendedProperties?.Private__ != null && calendarEvent.ExtendedProperties.Private__.ContainsKey("uniqueId"))
            {
                var uniqueId = calendarEvent.ExtendedProperties.Private__["uniqueId"];
                existingEvents[uniqueId] = calendarEvent;
            }
        }
        Console.WriteLine($"Found {existingEvents.Count} existing assignments(s) on your calendar,");
        return existingEvents;
    }

    //adding unique ID to event
    public async Task<string?> CreateEventAsync(Assignment assignment)
    {
        if (_service == null || assignment.Due_At == null)
        {
            return null;
        }

        var newEvent = new Event()
        {
            Summary = assignment.Name,
            Description = $"Link: {assignment.Html_Url}",
            // Set the event to be one hour long, ending at the due date.
            Start = new EventDateTime() { DateTimeDateTimeOffset = assignment.Due_At.Value.AddHours(-1) },
            End = new EventDateTime() { DateTimeDateTimeOffset = assignment.Due_At.Value },
            ExtendedProperties = new Event.ExtendedPropertiesData
            {
                Private__ = new Dictionary<string, string>
                {
                    {"uniqueId", assignment.UniqueId},
                    {"source", "blueprint_scraper"}
                }
            }
        };

        var request = _service.Events.Insert(newEvent, "primary");
        var createdEvent = await request.ExecuteAsync();
        return createdEvent.Id;
    }

    public async Task UpdateEventAsync(Event existingEvent, Assignment assignment)
    {
        if (_service == null || assignment.Due_At == null) return;
        bool needsUpdate = false;
        if (existingEvent.End.DateTimeDateTimeOffset != assignment.Due_At.Value)
        {
            existingEvent.Start = new EventDateTime() { DateTimeDateTimeOffset = assignment.Due_At.Value.AddHours(-1) };
            existingEvent.End = new EventDateTime() { DateTimeDateTimeOffset = assignment.Due_At.Value };
            needsUpdate = true;
        }
        if (needsUpdate)
        {
            await _service.Events.Update(existingEvent, "primary", existingEvent.Id).ExecuteAsync();
            Console.WriteLine($"Updated due date for: {assignment.Name}");
        }
    }
}
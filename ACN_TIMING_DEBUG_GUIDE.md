# ACN Timing URL Debugging Guide

## The Problem
Your URL: `https://www.acn-timing.com/?lng=FR#/events/2156215316811398/ctx/20260328_spa/generic/199034_1/home/LIVE1`

This is a **Single Page Application (SPA)** - the part after `#` is handled by JavaScript in the browser, not by the server.

## How to Find the Correct API Endpoint

### Method 1: Use Browser Developer Tools (RECOMMENDED)

1. **Open the URL in your browser** (Chrome, Edge, or Firefox)
   - URL: https://www.acn-timing.com/?lng=FR#/events/2156215316811398/ctx/20260328_spa/generic/199034_1/home/LIVE1

2. **Open Developer Tools** (Press F12)

3. **Go to the Network Tab**

4. **Filter by XHR or Fetch** (these show API calls)

5. **Refresh the page** or wait for the race data to load

6. **Look for API calls** - You should see requests like:
   - `api/participants?...`
   - `api/results?...`
   - `api/events/...`

7. **Click on the successful request** and note:
   - The full URL
   - Request headers (especially important ones like Authorization, X-API-Key, etc.)
   - Query parameters
   - Response format (JSON structure)

8. **Copy the actual API URL** - This is what we need to use in the code

### Method 2: Check Browser Network Requests

Example of what you might find:
```
Request URL: https://www.acn-timing.com/api/participants?eventId=2156215316811398&raceId=199034_1
Request Method: GET
Status Code: 200 OK
```

## Common ACN Timing API Patterns

Based on the URL structure, likely endpoints:
- `https://www.acn-timing.com/api/participants?eventId=2156215316811398&raceId=199034_1`
- `https://www.acn-timing.com/api/results?eventId=2156215316811398&raceId=199034_1`
- `https://www.acn-timing.com/api/live/2156215316811398/199034_1`

## What to Do Next

### Option A: If you find the API endpoint
1. Note the exact URL format
2. Note any required headers
3. Update the `TryGetResultsFromApi` method in `AcnTimingRaceResultRepository.cs`

### Option B: If no API is found
The race data might be:
1. **Embedded in the HTML** - Look at page source for JSON data
2. **Loaded via WebSocket** - Live timing often uses WebSockets
3. **Protected/Requires Authentication** - May need API keys or login

### Option C: Alternative Approach
If the API is not accessible, you could:
1. **Download the HTML manually** from the browser after it loads
2. **Save it as a JSON or HTML file**
3. **Use the file** instead of the URL in the application
4. The code already supports reading from files

## Testing Your Fix

After finding the correct endpoint:

1. Update this line in `AcnTimingRaceResultRepository.cs`:
   ```csharp
   var apiUrls = new[]
   {
       "YOUR_ACTUAL_API_ENDPOINT_HERE",
       // ... other fallbacks
   };
   ```

2. Run the application again with debug output enabled

3. Check the Output window in Visual Studio for diagnostic messages

## Debug Output

The updated code now outputs diagnostic information. To see it:
1. Run in Debug mode (F5)
2. Open Output window (Ctrl+Alt+O)
3. Select "Debug" from the dropdown
4. You'll see messages like:
   ```
   Processing ACN Timing URL: ...
   Trying API URL: ...
   SUCCESS: Got data from ...
   ```

## Need More Help?

If you're stuck, you can:
1. Share a screenshot of the Network tab showing the API calls
2. Export the JSON response from the working API endpoint
3. Provide the HAR (HTTP Archive) file from browser dev tools

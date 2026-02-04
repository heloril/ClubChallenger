# Facebook Sharing - Quick Start Guide

## What You Can Share

✅ **Race Results** - Individual race results with top finishers  
✅ **Challenge Standings** - Overall challenge rankings (coming soon)  
✅ **Automated Posting** - Post automatically when processing races  

---

## Quick Setup (5 Minutes)

### 1. Configure Facebook Settings

Edit `appsettings.json` or set user secrets:

```bash
cd NameParser.Web
dotnet user-secrets set "Facebook:AppId" "YOUR_APP_ID"
dotnet user-secrets set "Facebook:AppSecret" "YOUR_APP_SECRET"
dotnet user-secrets set "Facebook:PageId" "YOUR_PAGE_ID"
dotnet user-secrets set "Facebook:PageAccessToken" "YOUR_TOKEN"
```

See `FACEBOOK_SETUP_GUIDE.md` for detailed instructions on getting these values.

### 2. Test the Integration

1. Run your application
2. Go to the Races page (`/Races`)
3. Find a race you want to share
4. Click the **blue "Share" button** (Facebook icon)
5. Check your Facebook page - the post should appear!

---

## How It Works

### Race Result Posts

When you click "Share" on a race:

1. **Generates Summary** with:
   - Race name and distance
   - Top 3 finishers with times
   - Total participant count

2. **Creates Post** with:
   - Formatted text with emojis
   - Link back to results page
   - Automatic timing

3. **Returns Status**:
   - Success: Shows Post ID and confirmation
   - Error: Shows detailed error message

### Example Post

```
🏃 Summer 10K Championship - 2024

Results for Summer 10K Championship (10 km)

🏆 Top 3 Finishers:
🥇 1. John Doe - 00:35:24 (Running Club)
🥈 2. Jane Smith - 00:36:12 (Athletics Team)
🥉 3. Mike Johnson - 00:37:45

👥 Total Participants: 156

🔗 View full results: https://yoursite.com/Races?raceId=42
```

---

## Button Locations

### Races Page
- **Location**: In the action buttons for each race
- **Icon**: Facebook logo (blue)
- **Action**: Posts race results immediately

---

## Customizing Posts

### Change Post Format

Edit `RacesModel.BuildRaceSummary()` in `Races.cshtml.cs`:

```csharp
private string BuildRaceSummary(RaceEntity race, List<ClassificationEntity> topResults)
{
    var summary = $"🎉 New results posted!\n\n";
    summary += $"Race: {race.Name}\n";
    summary += $"Distance: {race.DistanceKm} km\n\n";
    
    // Add your custom formatting here
    
    return summary;
}
```

### Add Images

To post with images, modify the call in `OnPostShareToFacebookAsync`:

```csharp
// Generate or load image
byte[] imageData = GenerateRaceResultsImage(race, classifications);

// Post with image
var result = await _facebookService.PostRaceResultsAsync(
    raceName: $"{race.Name} - {race.Year}",
    raceUrl: resultsUrl,
    summary: summary,
    imageData: imageData  // Add this parameter
);
```

---

## Troubleshooting

### "Invalid OAuth access token"
- **Cause**: Token expired or invalid
- **Fix**: Generate new Page Access Token (see setup guide)

### "Permission denied"
- **Cause**: Missing Facebook permissions
- **Fix**: Ensure app has `pages_manage_posts` permission

### Post not appearing
- **Cause**: Wrong Page ID or token for different page
- **Fix**: Verify Page ID matches the page token

### Button does nothing
- **Cause**: JavaScript/form error
- **Fix**: Check browser console for errors

---

## Best Practices

### ✅ DO:
- Post results shortly after processing races
- Keep messages concise and engaging
- Include links back to full results
- Use emojis for visual appeal
- Test with your admin account first

### ❌ DON'T:
- Post too frequently (rate limits)
- Include sensitive personal information
- Post without participant consent
- Spam with duplicate posts
- Commit tokens to source control

---

## Rate Limits

Facebook has rate limits on API calls:
- **Page posts**: ~200 per hour
- **Photos**: ~100 per hour

For normal race sharing, you won't hit these limits.

---

## Privacy Considerations

When sharing race results publicly:

1. **Participant Names**: Ensure participants have consented to public sharing
2. **Personal Data**: Don't include email addresses, phone numbers, etc.
3. **Photos**: Only use photos you have permission to share
4. **Results**: Consider only posting top finishers or aggregate statistics

---

## Advanced Features

### Auto-post When Processing Races

Add this to `Index.cshtml.cs` after processing a race:

```csharp
// After race is processed successfully
if (/* race processing succeeded */)
{
    // Auto-share to Facebook
    await _facebookService.PostRaceResultsAsync(
        raceName: raceName,
        raceUrl: $"{Request.Scheme}://{Request.Host}/Races?raceId={raceId}",
        summary: BuildAutoPostSummary(race)
    );
}
```

### Post Challenge Standings

Create a new handler in a Challenge standings page:

```csharp
public async Task<IActionResult> OnPostShareChallengeAsync(int year)
{
    var standings = GetChallengeStandings(year);
    var summary = BuildChallengesSummary(standings);
    
    var result = await _facebookService.PostChallengeResultsAsync(
        challengeTitle: $"Challenge {year} Standings",
        challengeUrl: $"{Request.Scheme}://{Request.Host}/Challenge?year={year}",
        summary: summary
    );
    
    // Handle result...
}
```

### Schedule Posts

Use a background service (Hangfire, Quartz.NET) to schedule posts:

```csharp
// Post results every Sunday at 6 PM
BackgroundJob.Schedule(
    () => PostWeeklyResults(),
    TimeSpan.FromDays(7)
);
```

---

## Security Checklist

Before going to production:

- [ ] Tokens stored in user secrets or environment variables
- [ ] Never committed to Git
- [ ] HTTPS enabled on your site
- [ ] Facebook app in production mode
- [ ] Permissions approved by Facebook
- [ ] Error handling and logging enabled
- [ ] Rate limiting implemented
- [ ] Participant consent obtained

---

## Support

For issues or questions:

1. Check `FACEBOOK_SETUP_GUIDE.md` for detailed setup
2. Review application logs for errors
3. Use Facebook's Graph API Explorer to test tokens
4. Check Facebook Developer Docs: https://developers.facebook.com/docs/

---

## What's Next?

Consider adding:
- **Image generation** for race results graphics
- **Multiple platform support** (Twitter, Instagram)
- **Custom templates** for different race types
- **Scheduled posting** for regular updates
- **Analytics** to track post engagement
- **Video summaries** for major events

Happy sharing! 🎉

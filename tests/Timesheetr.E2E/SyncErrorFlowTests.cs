using Microsoft.Playwright;

namespace Timesheetr.E2E;

[TestFixture]
public class SyncErrorFlowTests : TimesheetrTest
{
    [Test]
    public async Task JiraFailureMarksEntryAsError()
    {
        await FakeApi.SeedTogglEntryAsync(new TogglEntrySeed(
            Id: 43,
            Description: "[INSP-101] Broken issue lookup",
            Start: DateTimeOffset.UtcNow.AddHours(-2),
            Stop: DateTimeOffset.UtcNow.AddHours(-1),
            Duration: 1800,
            WorkspaceId: 1,
            Tags: []));
        await FakeApi.ForceErrorAsync("jira", 500);

        await Page.GotoAsync("/");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Load from Toggl" }).ClickAsync();
        await Expect(Page.Locator("input[value='INSP-101']")).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Sync 1 entry to Tempo" }).ClickAsync();

        await Expect(Page.GetByText("Error", new() { Exact = true })).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Test]
    public async Task TogglRateLimitShowsRetryState()
    {
        await FakeApi.SeedTogglEntryAsync(new TogglEntrySeed(
            Id: 44,
            Description: "[INSP-102] Rate limited tagging",
            Start: DateTimeOffset.UtcNow.AddHours(-2),
            Stop: DateTimeOffset.UtcNow.AddHours(-1),
            Duration: 900,
            WorkspaceId: 1,
            Tags: []));
        await FakeApi.SeedJiraIssueAsync("INSP-102", 556);
        await FakeApi.ForceErrorAsync("toggl", 429);

        await Page.GotoAsync("/");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Load from Toggl" }).ClickAsync();
        await Expect(Page.Locator("input[value='INSP-102']")).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Sync 1 entry to Tempo" }).ClickAsync();

        // Jira/Tempo succeed, then the Toggl tag call hits the forced 429 and schedules a retry.
        await Expect(Page.GetByText("retrying at")).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }
}

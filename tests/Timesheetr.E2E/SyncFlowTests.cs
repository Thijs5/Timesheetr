using Microsoft.Playwright;

namespace Timesheetr.E2E;

[TestFixture]
public class SyncFlowTests : TimesheetrTest
{
    [Test]
    public async Task LoadSyncViewInTempoThenReset()
    {
        await FakeApi.SeedTogglEntryAsync(new TogglEntrySeed(
            Id: 42,
            Description: "[INSP-100] Write e2e tests",
            Start: DateTimeOffset.UtcNow.AddHours(-2),
            Stop: DateTimeOffset.UtcNow.AddHours(-1),
            Duration: 3600,
            WorkspaceId: 1,
            Tags: []));
        await FakeApi.SeedJiraIssueAsync("INSP-100", 555);

        await Page.GotoAsync("/");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Load from Toggl" }).ClickAsync();
        // Unsynced rows render issue key/description as editable <input>s, so their
        // content lives in the value attribute rather than as visible text.
        await Expect(Page.Locator("input[value='INSP-100']")).ToBeVisibleAsync();

        // Pending entries are selected by default, so the sync button already covers this one.
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sync 1 entry to Tempo" }).ClickAsync();

        // Once every entry for the day is Synced, the row collapses behind a day-summary badge.
        // "1 synced" also appears in the page's overview line, so scope to the table's badge.
        var daySummaryBadge = Page.GetByRole(AriaRole.Table).GetByText("1 synced");
        await Expect(daySummaryBadge).ToBeVisibleAsync(new() { Timeout = 15_000 });

        await Page.GotoAsync("/tempo");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Load from Tempo" }).ClickAsync();
        await Expect(Page.GetByText("Write e2e tests")).ToBeVisibleAsync();

        await Page.GotoAsync("/");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Load from Toggl" }).ClickAsync();
        await Page.GetByRole(AriaRole.Table).GetByText("1 synced").ClickAsync(); // expand the collapsed day
        await Page.GetByTitle("Reset entry").ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Reset" }).ClickAsync();
        await Expect(Page.GetByText("Pending")).ToBeVisibleAsync();
    }
}

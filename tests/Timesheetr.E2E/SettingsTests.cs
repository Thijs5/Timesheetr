using Microsoft.Playwright;

namespace Timesheetr.E2E;

[TestFixture]
public class SettingsTests : TimesheetrTest
{
    [Test]
    public async Task SavingIntegrationsPersistsAndClearsSetupBanner()
    {
        // Start from an unconfigured state so the "Setup required" banner is meaningful.
        await SaveSettingsAsync(new IntegrationSettings("", "", "", "", "", ""));

        await Page.GotoAsync("/settings");

        await Page.GetByPlaceholder("Your Toggl Track API token").FillAsync("toggl-token-123");
        await Page.GetByPlaceholder("Your Tempo API token").FillAsync("tempo-token-123");
        await Page.GetByPlaceholder("https://your-org.atlassian.net").FillAsync(TestServerFixture.FakeHostUrl + "/jira");
        await Page.GetByPlaceholder("you@example.com").FillAsync("qa@example.com");
        await Page.GetByPlaceholder("Your Jira API token").FillAsync("jira-token-123");
        await Page.GetByPlaceholder("63aea1ec...").FillAsync("account-123");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Save integrations" }).ClickAsync();
        await Expect(Page.GetByText("Saved")).ToBeVisibleAsync();

        await Page.ReloadAsync();
        await Expect(Page.GetByPlaceholder("Your Toggl Track API token")).ToHaveValueAsync("toggl-token-123");

        await Page.GotoAsync("/");
        await Expect(Page.GetByText("Setup required")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task InfrastructureTabRendersRabbitMqDashboardLink()
    {
        // vite.config.ts always defines VITE_RABBITMQ_MANAGEMENT_URL with a default,
        // so the RabbitMQ card renders in dev/test rather than the "not configured" fallback.
        await Page.GotoAsync("/settings/infrastructure");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "RabbitMQ" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Open management dashboard" })).ToBeVisibleAsync();
    }
}

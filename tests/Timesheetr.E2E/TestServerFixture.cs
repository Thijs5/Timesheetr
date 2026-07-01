using System.Diagnostics;
using System.Text;

namespace Timesheetr.E2E;

[SetUpFixture]
public class TestServerFixture
{
    public const string FakeHostUrl = "http://localhost:5299";
    public const string ApiUrl = "http://localhost:5100";
    public const string FrontendUrl = "http://localhost:5173";

    static readonly List<(Process Process, StringBuilder Log)> _processes = [];
    static string? _dataDir;
    static string? _buildTempDir;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var repoRoot = FindRepoRoot();
        _dataDir = Path.Combine(Path.GetTempPath(), $"timesheetr-e2e-data-{Guid.NewGuid():N}");
        _buildTempDir = Path.Combine(Path.GetTempPath(), $"timesheetr-e2e-build-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dataDir);

        // Build to isolated output directories rather than `dotnet run`, so this doesn't
        // collide with bin/obj of an already-running local dev instance of the same project.
        var fakeHostDll = BuildProject(
            Path.Combine(repoRoot, "tests", "Timesheetr.FakeExternalApis"),
            Path.Combine(_buildTempDir, "fakehost"));
        var apiDll = BuildProject(
            Path.Combine(repoRoot, "src", "Timesheetr.Api"),
            Path.Combine(_buildTempDir, "api"));

        StartProcess("dotnet", $"\"{fakeHostDll}\" --urls {FakeHostUrl}", Path.GetDirectoryName(fakeHostDll)!);

        StartProcess(
            "dotnet",
            $"\"{apiDll}\" --urls {ApiUrl}",
            Path.GetDirectoryName(apiDll)!,
            new Dictionary<string, string>
            {
                ["DataPath"] = _dataDir,
                ["ExternalServices__TogglBaseUrl"] = FakeHostUrl + "/toggl/",
                ["ExternalServices__TempoBaseUrl"] = FakeHostUrl + "/tempo/",
            });

        StartProcess(
            "cmd.exe",
            "/c npm run dev",
            Path.Combine(repoRoot, "src", "Timesheetr.WebApp"),
            new Dictionary<string, string>
            {
                ["services__backend__http__0"] = ApiUrl,
            });

        using var http = new HttpClient();
        await WaitUntilReady(http, FakeHostUrl + "/__admin/health", "fake external services host");
        await WaitUntilReady(http, ApiUrl + "/api/settings", "Timesheetr.Api");
        await WaitUntilReady(http, FrontendUrl, "Vite dev server");

        Microsoft.Playwright.Program.Main(["install"]);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        foreach (var (process, _) in _processes)
        {
            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
            }
            catch
            {
                // best-effort cleanup
            }
        }

        foreach (var dir in new[] { _dataDir, _buildTempDir })
        {
            if (dir is not null && Directory.Exists(dir))
            {
                try { Directory.Delete(dir, recursive: true); } catch { /* best-effort cleanup */ }
            }
        }
    }

    static string BuildProject(string projectDir, string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        var startInfo = new ProcessStartInfo("dotnet", $"build -c Debug -o \"{outputDir}\"")
        {
            WorkingDirectory = projectDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Build failed for {projectDir}:\n{output}\n{error}");
        }

        var projectName = new DirectoryInfo(projectDir).Name;
        return Path.Combine(outputDir, projectName + ".dll");
    }

    static void StartProcess(string fileName, string arguments, string workingDirectory, Dictionary<string, string>? env = null)
    {
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        if (env is not null)
        {
            foreach (var (key, value) in env) startInfo.Environment[key] = value;
        }

        var process = new Process { StartInfo = startInfo };
        var log = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (log) log.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (log) log.AppendLine(e.Data); };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        _processes.Add((process, log));
    }

    static async Task WaitUntilReady(HttpClient http, string url, string name, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(90));
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await http.GetAsync(url);
                if ((int)response.StatusCode < 500) return;
            }
            catch
            {
                // not up yet
            }

            await Task.Delay(500);
        }

        var logs = string.Join("\n---\n", _processes.Select(p => ReadLog(p.Log)));
        throw new TimeoutException($"Timed out waiting for {name} ({url}) to become ready.\n{logs}");
    }

    static string ReadLog(StringBuilder log)
    {
        lock (log) return log.ToString();
    }

    static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Timesheetr.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not locate repo root (Timesheetr.slnx not found).");
    }
}

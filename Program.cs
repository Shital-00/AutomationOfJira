using System;
using System.IO;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace JiraScreenshotDownloader
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Enter your Jira Base URL (e.g., https://your-domain.atlassian.net):");
            string baseUrl = Console.ReadLine();

            Console.WriteLine("Enter your Jira Username (e.g., email@example.com):");
            string username = Console.ReadLine();

            Console.WriteLine("Enter your Jira API Token:");
            string apiToken = Console.ReadLine();

            Console.WriteLine("Enter the Target Board Name:");
            string targetBoardName = Console.ReadLine();

            Console.WriteLine("Enter the Targeted Sprint Name:");
            string targetSprintName = Console.ReadLine();

            Console.WriteLine("Enter the Issue Type to Filter (e.g., 'Task'):");
            string issueTypeToFilter = Console.ReadLine();

            Console.WriteLine("Enter the Keyword to Match in Attachments (e.g., 'screenshot'):");
            string keywordForSS = Console.ReadLine();

            JiraConfig config = new JiraConfig
            {
                BaseUrl = baseUrl,
                Username = username,
                ApiToken = apiToken,
                TargetBoardName = targetBoardName,
                TargetSprintName = targetSprintName,
                IssueTypeToFilter = issueTypeToFilter,
                KeywordForSS = keywordForSS
            };

            IconsScreenshotDownloader downloader = new IconsScreenshotDownloader(config);

            await downloader.Start();
        }
    }

    public class JiraConfig
    {
        public string BaseUrl { get; set; }
        public string Username { get; set; }
        public string ApiToken { get; set; }
        public string TargetBoardName { get; set; }

        public string TargetSprintName { get; set; }
        public string IssueTypeToFilter { get; set; }
        public string KeywordForSS { get; set; }
    }

    public class IconsScreenshotDownloader
    {
        private readonly JiraConfig _config;
        private readonly HttpClient _httpClient;
        private int downloadingCounter = 0;
        private int jiraissuesCount = 0;
        public IconsScreenshotDownloader(JiraConfig config)
        {
            _config = config;
            _httpClient = new HttpClient();
            string auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_config.Username}:{_config.ApiToken}"));
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {auth}");
        }

        public async Task Start()
        {
            int boardId = await GetBoardIdByName(_config.TargetBoardName);
            if (boardId == -1)
            {
                Console.WriteLine("Board not found.");
                return;
            }

            int sprintId = await GetActiveSprintWithWordInName(boardId, _config.TargetSprintName);
            if (sprintId == -1)
            {
                Console.WriteLine("No active sprint found with the specified keyword.");
                return;
            }

            await ProcessSprintIssues(sprintId);
        }

        private async Task<int> GetBoardIdByName(string boardName)
        {
            string url = $"{_config.BaseUrl}/rest/agile/1.0/board";
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            try
            {
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    //Console.WriteLine(responseBody);
                    BoardResponse boards = JsonConvert.DeserializeObject<BoardResponse>(responseBody);

                    foreach (var board in boards.Values)
                    {
                        if (board.Name.Equals(boardName, StringComparison.OrdinalIgnoreCase))
                        {
                            return board.Id;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return -1;
        }

        private async Task<int> GetActiveSprintWithWordInName(int boardId, string sprintWord)
        {
            string url = $"{_config.BaseUrl}/rest/agile/1.0/board/{boardId}/sprint?state=active";
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            try
            {
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    SprintResponse sprints = JsonConvert.DeserializeObject<SprintResponse>(responseBody);

                    foreach (var sprint in sprints.Values)
                    {
                        if (sprint.Name.Contains(sprintWord, StringComparison.OrdinalIgnoreCase))
                        {
                            return sprint.Id;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return -1;
        }

        private async Task ProcessSprintIssues(int sprintId)
        {
            int startAt = 0;
            int maxResults = 50;
            bool hasMoreIssues = true;
            while (hasMoreIssues)
            {
                string url = $"{_config.BaseUrl}/rest/agile/1.0/sprint/{sprintId}/issue?startAt={startAt}&maxResults={maxResults}";
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                try
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        IssueList issues = JsonConvert.DeserializeObject<IssueList>(responseBody);
                        if (issues != null && issues.Issues != null)
                        {
                            foreach (var issue in issues.Issues)
                            {
                                if (issue.Fields.IssueType.Name == _config.IssueTypeToFilter)
                                {
                                    jiraissuesCount++;
                                    await CheckAttachmentsForIssue(issue.Key);
                                }
                            }
                            if (issues.Issues.Length < maxResults)
                            {
                                hasMoreIssues = false; //No more issues we have to fetch
                            }
                            else
                            {
                                startAt += maxResults; // Update startAt for the next set of issues
                            }
                        }
                        else
                        {
                            hasMoreIssues = false;
                        }

                    }
                    else
                    {
                        Console.WriteLine($"Failed to fetch sprint issues: {response.StatusCode}");
                        hasMoreIssues = false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }

        }
        private async Task CheckAttachmentsForIssue(string issueId)
        {
            string url = $"{_config.BaseUrl}/rest/api/2/issue/{issueId}";
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            try
            {
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    IssueDetails issueDetails = JsonConvert.DeserializeObject<IssueDetails>(responseBody);

                    foreach (var attachment in issueDetails.Fields.Attachment)
                    {
                        if (attachment.Filename.Contains(_config.KeywordForSS))
                        {
                            await DownloadAttachment(attachment.Content, issueDetails.Fields.Summary, attachment.Filename);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        private async Task DownloadAttachment(string attachmentUrl, string headline, string filename)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(attachmentUrl);

            if (response.IsSuccessStatusCode)
            {
                byte[] data = await response.Content.ReadAsByteArrayAsync();
                string sanitizedFileName = SanitizeFileName(headline + Path.GetExtension(filename));
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), sanitizedFileName);

                File.WriteAllBytesAsync(filePath, data);
                var task1 = Task.Delay(2000);
                Console.WriteLine($"Downloaded: {filePath}");
                downloadingCounter++;
                string reportfile = "report.txt";
                string reportPath = Path.Combine(Directory.GetCurrentDirectory(), reportfile);
                string reportdata = $"Total JiraTickets Count : {jiraissuesCount}\n" + $"Total Count Of Downloading Images : {downloadingCounter}\n" + $"Remaining Count Of Images : {jiraissuesCount - downloadingCounter}";
                File.WriteAllTextAsync(reportPath, reportdata);
                var task2 = Task.Delay(3000);
                await Task.WhenAll(task1, task2);               
            }
        }

        private string SanitizeFileName(string fileName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }
    }

    // Models
    public class BoardResponse
    {
        public Board[] Values { get; set; }
    }

    public class Board
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class SprintResponse
    {
        public Sprint[] Values { get; set; }
    }

    public class Sprint
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class IssueList
    {
        public Issue[] Issues { get; set; }
    }

    public class Issue
    {
        public string Key { get; set; }
        public IssueFields Fields { get; set; }
    }

    public class IssueFields
    {
        public Attachment[] Attachment { get; set; }
        public IssueType IssueType { get; set; }
        public string Summary { get; set; }
    }

    public class IssueType
    {
        public string Name { get; set; }
    }

    public class Attachment
    {
        public string Filename { get; set; }
        public string Content { get; set; }
    }

    public class IssueDetails
    {
        public IssueFields Fields { get; set; }
    }
}

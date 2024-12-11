using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewJiraAutomation
{
    public class JiraDownloader
    {
        private readonly JiraConfig _config;
        private readonly HttpClient _httpClient;
        private int downloadingCounter = 0;
        private int jiraissuesCount = 0;
        public JiraDownloader(JiraConfig config)
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
                    NewJiraAutomation.Models.BoardResponse boards = JsonConvert.DeserializeObject<NewJiraAutomation.Models.BoardResponse>(responseBody);

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
                    NewJiraAutomation.Models.SprintResponse sprints = JsonConvert.DeserializeObject<NewJiraAutomation.Models.SprintResponse>(responseBody);
                    //Console.WriteLine(sprints);
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
                        NewJiraAutomation.Models.IssueList issues = JsonConvert.DeserializeObject<NewJiraAutomation.Models.IssueList>(responseBody);
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
                    NewJiraAutomation.Models.IssueDetails issueDetails = JsonConvert.DeserializeObject<NewJiraAutomation.Models.IssueDetails>(responseBody);

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

}


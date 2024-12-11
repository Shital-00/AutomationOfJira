using System;
using System.IO;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace NewJiraAutomation
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

            JiraDownloader downloader = new JiraDownloader(config);

            await downloader.Start();
        }
    }
}

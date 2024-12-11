using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewJiraAutomation
{
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
}

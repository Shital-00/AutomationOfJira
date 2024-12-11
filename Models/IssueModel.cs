using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewJiraAutomation.Models
{
        internal class IssueList
        {
            public Issue[] Issues { get; set; }
        }

        internal class Issue
        {
            public string Key { get; set; }
            public IssueFields Fields { get; set; }
        }

        internal class IssueFields
        {
            public Attachment[] Attachment { get; set; }
            public IssueType IssueType { get; set; }
            public string Summary { get; set; }
        }

        internal class IssueType
        {
            public string Name { get; set; }
        }

        internal class Attachment
        {
            public string Filename { get; set; }
            public string Content { get; set; }
        }

        internal class IssueDetails
        {
            public IssueFields Fields { get; set; }
        }
    

}

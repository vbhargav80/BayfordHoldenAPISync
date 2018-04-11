using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BayCityHoldenSync
{
    public class AdminToolOverride : TableEntity
    {
        public string Category { get; set; }
    }

    public class DocumentVersion : TableEntity
    {
        public string VersionNumber { get; set; }
    }
}

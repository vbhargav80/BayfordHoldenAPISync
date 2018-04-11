using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BayCityHoldenSync
{
    public class CDSItem : TableEntity
    {
        public string Location { get; set; }
    }
}

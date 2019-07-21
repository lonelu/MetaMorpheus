using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaDrawGUI
{
    public class GlycoStructureForDataGrid
    {
        public GlycoStructureForDataGrid(int scanNum)
        {
            ScanNum = scanNum;
        }

        public int ScanNum { get; set; }
        public string FullSeq { get; set; }
        public string Structure { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaDrawGUI
{
    public class GlycoStructureForDataGrid
    {
        public GlycoStructureForDataGrid(int scanNum, string fullSeq, string structure)
        {
            ScanNum = scanNum;
            FullSeq = fullSeq;
            Structure = structure;
        }

        public int ScanNum { get; set; }
        public string FullSeq { get; set; }
        public string Structure { get; set; }
    }
}

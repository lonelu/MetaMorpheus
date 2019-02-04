using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaDrawGUI
{
    public class SimplePsm
    {
        public SimplePsm(int scanNum, string fullSeq, string glycoStructure)
        {
            ScanNum = scanNum;
            FullSeq = fullSeq;
            GlycoStructure = glycoStructure;
        }

        public int ScanNum { get; set; }
        public string FullSeq { get; set; }
        public string GlycoStructure { get; set; }

    }
}

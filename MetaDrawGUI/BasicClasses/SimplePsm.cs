using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Proteomics.Fragmentation;

namespace MetaDrawGUI
{
    public class SimplePsm
    {
        public SimplePsm(string line, char[] split, Dictionary<string, int> parsedHeader)
        {
            var spl = line.Split(split);
            ScanNum = int.Parse(spl[parsedHeader[PsmTsvHeader_pGlyco.FileName]].Split('.')[1]); //this is special for pGlyco
            BaseSeq = spl[parsedHeader[PsmTsvHeader_pGlyco.BaseSequence]].Trim();
            Mod = spl[parsedHeader[PsmTsvHeader_pGlyco.Mods]].Trim();
            GlycoStructure = spl[parsedHeader[PsmTsvHeader_pGlyco.GlyStruct]].Trim();
        }

        public int ScanNum { get;}
        public string BaseSeq { get; }
        public string Mod { get;  }
        public string GlycoStructure { get; }
        public string BetaPeptideBaseSequence { get; set; }
        public List<MatchedFragmentIon> MatchedIons { get; set; }
        public List<MatchedFragmentIon> BetaPeptideMatchedIons { get; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public int ScanNum { get; set; }
        public string BaseSeq { get; set; }
        public string Mod { get; set; }
        public string GlycoStructure { get; set; }

    }
}

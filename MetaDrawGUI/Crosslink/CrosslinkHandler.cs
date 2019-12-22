using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EngineLayer;

namespace MetaDrawGUI.Crosslink
{
    public static class CrosslinkHandler
    {
        public static void validateCrosslinks(List<PsmFromTsv> psms)
        {
            var psms_filtered = psms.Where(p => p.DecoyContamTarget == "T" && p.QValue <= 0.05).ToArray();

            int correct = 0;

            for (int i = 0; i < psms_filtered.Length; i++)
            {
                
                if (SyntheticLibrary.TheoryCrosslinks.Keys.Contains(psms_filtered[i].BaseSeq + "_" + psms_filtered[i].BetaPeptideBaseSequence) ||
                    SyntheticLibrary.TheoryCrosslinks.Keys.Contains(psms_filtered[i].BetaPeptideBaseSequence + "_" + psms_filtered[i].BaseSeq))
                {
                    correct++;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using EngineLayer;

namespace MetaDrawGUI
{
    public class TsvReader_pGlyco
    {

        private static readonly char[] Split = { '\t' };

        public static List<SimplePsm> ReadTsv(string filepath)
        {
            List<SimplePsm> simplePsms = new List<SimplePsm>();

            StreamReader reader = null;
            try
            {
                reader = new StreamReader(filepath);
            }
            catch(Exception e)
            {
                throw new MetaMorpheusException("Could not read file: " + e.Message);
            }

            int lineCount = 0;
            string line;
            Dictionary<string, int> parsedHeader = null;

            while (reader.Peek() > 0)
            {
                lineCount++;
                line = reader.ReadLine();

                if (lineCount == 1)
                {
                    parsedHeader = ParseHeader(line);
                    continue;
                }

                try
                {
                    simplePsms.Add(new SimplePsm(line, Split, parsedHeader));
                }
                catch (Exception e)
                {
                    throw new MetaMorpheusException("Could not read file: " + e.Message);
                }
            }
            reader.Close();
            if ((lineCount - 1) != simplePsms.Count)
            {
                throw new MetaMorpheusException("Warning: " + ((lineCount - 1) - simplePsms.Count) + " PSMs were not read.");
            }
            return simplePsms;
        }

        private static Dictionary<string, int> ParseHeader(string header)
        {
            var parsedHeader = new Dictionary<string, int>();
            var spl = header.Split(Split);
            parsedHeader.Add(PsmTsvHeader_pGlyco.FileName, Array.IndexOf(spl, PsmTsvHeader_pGlyco.FileName));
            parsedHeader.Add(PsmTsvHeader_pGlyco.Ms2ScanRetentionTime, Array.IndexOf(spl, PsmTsvHeader_pGlyco.Ms2ScanRetentionTime));
            parsedHeader.Add(PsmTsvHeader_pGlyco.PrecursorMH, Array.IndexOf(spl, PsmTsvHeader_pGlyco.PrecursorMH));
            parsedHeader.Add(PsmTsvHeader_pGlyco.BaseSequence, Array.IndexOf(spl, PsmTsvHeader_pGlyco.BaseSequence));
            parsedHeader.Add(PsmTsvHeader_pGlyco.Mods, Array.IndexOf(spl, PsmTsvHeader_pGlyco.Mods));
            parsedHeader.Add(PsmTsvHeader_pGlyco.PeptideMH, Array.IndexOf(spl, PsmTsvHeader_pGlyco.PeptideMH));
            parsedHeader.Add(PsmTsvHeader_pGlyco.GlycanMass, Array.IndexOf(spl, PsmTsvHeader_pGlyco.GlycanMass));
            parsedHeader.Add(PsmTsvHeader_pGlyco.Glycan, Array.IndexOf(spl, PsmTsvHeader_pGlyco.Glycan));
            parsedHeader.Add(PsmTsvHeader_pGlyco.GlyStruct, Array.IndexOf(spl, PsmTsvHeader_pGlyco.GlyStruct));
            parsedHeader.Add(PsmTsvHeader_pGlyco.ProteinAccession, Array.IndexOf(spl, PsmTsvHeader_pGlyco.ProteinAccession));
            parsedHeader.Add(PsmTsvHeader_pGlyco.GlyQValue, Array.IndexOf(spl, PsmTsvHeader_pGlyco.GlyQValue));
            parsedHeader.Add(PsmTsvHeader_pGlyco.PepQValue, Array.IndexOf(spl, PsmTsvHeader_pGlyco.PepQValue));
            parsedHeader.Add(PsmTsvHeader_pGlyco.QValue, Array.IndexOf(spl, PsmTsvHeader_pGlyco.QValue));
            parsedHeader.Add(PsmTsvHeader_pGlyco.ProSite, Array.IndexOf(spl, PsmTsvHeader_pGlyco.ProSite));
            parsedHeader.Add(PsmTsvHeader_pGlyco.GlySite, Array.IndexOf(spl, PsmTsvHeader_pGlyco.GlySite));
            return parsedHeader;
        }

        public static void WriteTsv(string filePath, List<SimplePsm> simplePsms)
        {
            if (simplePsms.Count == 0)
            {
                return;
            }

            using (StreamWriter output = new StreamWriter(filePath))
            {
                string header = SimplePsm.GetTabSepHeaderGlyco();

                output.WriteLine(header);
                foreach (var heh in simplePsms.OrderBy(p=>p.QValue).Where(p=>p.QValue < 0.01))
                {
                    output.WriteLine(heh.ToString());
                }
            }
        }
    }

    public static class PsmTsvHeader_pGlyco
    {
        public const string FileName = "PepSpec";
        public const string Ms2ScanRetentionTime = "RT";
        public const string PrecursorMH = "PrecursorMH";
        public const string BaseSequence = "Peptide";
        public const string Mods = "Mod";
        public const string PeptideMH = "PeptideMH";
        public const string GlycanMass = "GlyMass";
        public const string Glycan = "Glycan(H,N,A,G,F)";
        public const string GlyStruct = "PlausibleStruct";
        public const string ProteinAccession = "Proteins";    
        public const string GlyQValue = "GlycanFDR";
        public const string PepQValue = "PeptideFDR";
        public const string QValue = "TotalFDR";
        public const string ProSite = "ProSite";
        public const string GlySite = "GlySite";
        public const string GlyComp = "Glycan(H,N,A,G,F)";
    }
}

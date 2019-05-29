using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using EngineLayer;

namespace MetaDrawGUI
{
    public enum TsvType
    {
        pGlyco,
        GlycReSoft,
        Byonic
    }

    public class TsvReader_Glyco
    {
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
            TsvType tsvType = TsvType.pGlyco;
            char[] Split = new char[] { '\t' };

            while (reader.Peek() > 0)
            {
                lineCount++;
                line = reader.ReadLine();             

                if (lineCount == 1)
                {                     
                    if (line.StartsWith("glycopeptide"))
                    {
                        tsvType = TsvType.GlycReSoft;
                        Split = new char[] { ',' };
                    }
                    else if (line.StartsWith(""))
                    {
                        tsvType = TsvType.Byonic;
                    }

                    switch (tsvType)
                    {
                        case TsvType.pGlyco:
                            parsedHeader = ParseHeader_pGlyco(line, Split);
                            break;
                        case TsvType.GlycReSoft:
                            parsedHeader = ParseHeader_GlycReSoft(line, Split);
                            break;
                        case TsvType.Byonic:
                            break;
                        default:
                            break;
                    }
                    
                    continue;
                }

                try
                {
                    simplePsms.Add(new SimplePsm(line, Split, parsedHeader, tsvType));
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

        private static Dictionary<string, int> ParseHeader_pGlyco(string header, char[] Split)
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

        private static Dictionary<string, int> ParseHeader_GlycReSoft(string header, char[] Split)
        {
            var parsedHeader = new Dictionary<string, int>();
            var spl = header.Split(Split);
            parsedHeader.Add(PsmTsvHeader_GlycReSoft.glycopeptide, Array.IndexOf(spl, PsmTsvHeader_GlycReSoft.glycopeptide));
            parsedHeader.Add(PsmTsvHeader_GlycReSoft.charge, Array.IndexOf(spl, PsmTsvHeader_GlycReSoft.charge));
            parsedHeader.Add(PsmTsvHeader_GlycReSoft.mass_accuracy, Array.IndexOf(spl, PsmTsvHeader_GlycReSoft.mass_accuracy));
            parsedHeader.Add(PsmTsvHeader_GlycReSoft.mass_shift_name, Array.IndexOf(spl, PsmTsvHeader_GlycReSoft.mass_shift_name));
            parsedHeader.Add(PsmTsvHeader_GlycReSoft.neutral_mass, Array.IndexOf(spl, PsmTsvHeader_GlycReSoft.neutral_mass));
            parsedHeader.Add(PsmTsvHeader_GlycReSoft.peptide_end, Array.IndexOf(spl, PsmTsvHeader_GlycReSoft.peptide_end));
            parsedHeader.Add(PsmTsvHeader_GlycReSoft.peptide_start, Array.IndexOf(spl, PsmTsvHeader_GlycReSoft.peptide_start));
            parsedHeader.Add(PsmTsvHeader_GlycReSoft.precursor_abundance, Array.IndexOf(spl, PsmTsvHeader_GlycReSoft.precursor_abundance));
            parsedHeader.Add(PsmTsvHeader_GlycReSoft.protein_name, Array.IndexOf(spl, PsmTsvHeader_GlycReSoft.protein_name));
            parsedHeader.Add(PsmTsvHeader_GlycReSoft.q_value, Array.IndexOf(spl, PsmTsvHeader_GlycReSoft.q_value));
            parsedHeader.Add(PsmTsvHeader_GlycReSoft.scan_id, Array.IndexOf(spl, PsmTsvHeader_GlycReSoft.scan_id));
            parsedHeader.Add(PsmTsvHeader_GlycReSoft.scan_time, Array.IndexOf(spl, PsmTsvHeader_GlycReSoft.scan_time));
            return parsedHeader;
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

    public static class PsmTsvHeader_GlycReSoft
    {
        public const string glycopeptide = "glycopeptide";
        public const string neutral_mass = "neutral_mass";
        public const string mass_accuracy = "mass_accuracy";
        public const string mass_shift_name = "mass_shift_name";
        public const string scan_id = "scan_id";
        public const string scan_time = "scan_time";
        public const string charge = "charge";
        public const string q_value = "q_value";
        public const string precursor_abundance = "precursor_abudance";
        public const string peptide_start = "peptide_start";
        public const string peptide_end = "peptide_end";
        public const string protein_name = "protein_name";
    }

}

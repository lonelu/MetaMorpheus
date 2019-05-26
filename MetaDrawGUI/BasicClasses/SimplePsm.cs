using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Proteomics.Fragmentation;
using EngineLayer;
using EngineLayer.CrosslinkSearch;
using Proteomics;
using Proteomics.ProteolyticDigestion;
using MassSpectrometry;

namespace MetaDrawGUI
{
    public class SimplePsm
    {
        public SimplePsm(string line, char[] split, Dictionary<string, int> parsedHeader)
        {
            //this is special for pGlyco
            var spl = line.Split(split);
            FileName = spl[parsedHeader[PsmTsvHeader_pGlyco.FileName]].Split('.')[0]; 
            ScanNum = int.Parse(spl[parsedHeader[PsmTsvHeader_pGlyco.FileName]].Split('.')[1]); 
            RT = double.Parse(spl[parsedHeader[PsmTsvHeader_pGlyco.Ms2ScanRetentionTime]])/60;
            PrecursorMass = double.Parse(spl[parsedHeader[PsmTsvHeader_pGlyco.PrecursorMH]]) - 1.0073;
            ChargeState = int.Parse(spl[parsedHeader[PsmTsvHeader_pGlyco.FileName]].Split('.')[3]);
            GlycanStructure = spl[parsedHeader[PsmTsvHeader_pGlyco.GlyStruct]].Trim();
            glycan = Glycan.Struct2Glycan(GlycanStructure, 0);
            MonoisotopicMass = double.Parse(spl[parsedHeader[PsmTsvHeader_pGlyco.PeptideMH]]) + glycan.Mass - 1.0073;
            var pBaseSeq = spl[parsedHeader[PsmTsvHeader_pGlyco.BaseSequence]].Trim();
            StringBuilder sb = new StringBuilder(pBaseSeq);
            sb[pBaseSeq.IndexOf('J')] = 'N';
            BaseSeq = sb.ToString();
            Mod = spl[parsedHeader[PsmTsvHeader_pGlyco.Mods]].Trim();

            Modification modification = GlycoPeptides.GlycanToModification(glycan);
            Dictionary<int, Modification> testMods = GetMods(Mod, AllPossibleMods);
            testMods.Add(pBaseSeq.IndexOf('J') + 1, modification);
            FullSeq = GetFullSeq(BaseSeq, testMods);

            glycoPwsm = new PeptideWithSetModifications(FullSeq, GlobalVariables.AllModsKnownDictionary);          

            QValue = double.Parse(spl[parsedHeader[PsmTsvHeader_pGlyco.QValue]]);
            Decoy = false;
            ProteinAccess = spl[parsedHeader[PsmTsvHeader_pGlyco.ProteinAccession]].Split('|')[1];
            ProteinName = spl[parsedHeader[PsmTsvHeader_pGlyco.ProteinAccession]].Split('|', '/')[2];
            int ProSite = int.Parse(spl[parsedHeader[PsmTsvHeader_pGlyco.ProSite]].Split('/')[0]);
            int GlySite = int.Parse(spl[parsedHeader[PsmTsvHeader_pGlyco.GlySite]]);
            int peptideLength = BaseSeq.Count();
            ProteinStartEnd = "[" +(ProSite - GlySite).ToString() + " to " +(ProSite + peptideLength - GlySite).ToString() + "]";
        }

        public string FileName { get; set; }
        public int ScanNum { get;}
        public double RT { get; }
        public double PrecursorMass { get; }
        public int ChargeState { get; }
        public double MonoisotopicMass { get; }     
        public string BaseSeq { get; }
        public string FullSeq { get; }
        public string Mod { get;  }
        public double QValue { get; }
        public bool Decoy { get; }
        public string ProteinName { get; }       
        public string ProteinAccess { get; }
        public string ProteinStartEnd { get; }

        public string BetaPeptideBaseSequence { get; }
        private string GlycanStructure { get; }
        public Glycan glycan { get; set; }
        public PeptideWithSetModifications glycoPwsm { get;  }
        public List<MatchedFragmentIon> MatchedIons { get; set; }
        public List<MatchedFragmentIon> BetaPeptideMatchedIons { get; set; }

        private static Dictionary<string, Modification> AllPossibleMods = GetAllPossibleMods();


        //TO DO: Bug may exist for the PrecursorMH, which is different from PrecursorMass.
        public static List<MatchedFragmentIon> GetMatchedIons(PeptideWithSetModifications glycoPwsm, double precursorMH, int chargeState, CommonParameters commonParameters, MsDataScan msDataScan)
        {
            List<Product> peptideTheorProducts = glycoPwsm.Fragment(commonParameters.DissociationType, commonParameters.DigestionParams.FragmentationTerminus).ToList();
            Ms2ScanWithSpecificMass scanWithMass = new Ms2ScanWithSpecificMass(msDataScan, precursorMH, chargeState, null, commonParameters);
            List<MatchedFragmentIon> matchedIons = MetaMorpheusEngine.MatchFragmentIons(scanWithMass, peptideTheorProducts, commonParameters);
            return matchedIons;
        }

        private static Dictionary<string, Modification> GetAllPossibleMods()
        {
            Dictionary<string, Modification> allPossibleMods = new Dictionary<string, Modification>();

            var m1 = GlobalVariables.AllModsKnown.Where(p => p.IdWithMotif == "Carbamidomethyl on C").FirstOrDefault();
            allPossibleMods.Add("Carbamidomethyl[C]", m1);

            var m2 = GlobalVariables.AllModsKnown.Where(p => p.IdWithMotif == "Oxidation on M").FirstOrDefault();
            allPossibleMods.Add("Oxidation[M]", m2);

            var m3 = GlobalVariables.AllModsKnown.Where(p => p.IdWithMotif == "Acetylation" && p.ModificationType == "N-terminal.").FirstOrDefault();
            allPossibleMods.Add("Acetyl[ProteinN-term]", m3);

            //var x = GlobalVariables.AllModsKnownDictionary;

            return allPossibleMods;
        }

        private static Dictionary<int, Modification> GetMods(string mod, Dictionary<string, Modification> AllPossibleMods)
        {
            //This is for pGlyco Mod: {1,Acetyl[ProteinN-term];1,Oxidation[M];}
            Dictionary<int, Modification> mods = new Dictionary<int, Modification>();

            char[] s1 = { ';' };
            char[] s2 = { ',' };
            var spl = mod.Split(s1);
            foreach (var m in spl)
            {
                if (m!="null" && m!="")
                {
                    var aspl = m.Split(s2);
                    if (aspl.Last().Contains("[ProteinN-term]"))
                    {
                        mods.Add(0, AllPossibleMods[aspl.Last()]);
                    }
                    else
                    {
                        mods.Add(int.Parse(aspl.First()), AllPossibleMods[aspl.Last()]);
                    }
                }               
            }
            return mods;
        }

        private static string GetFullSeq(string BaseSeq, Dictionary<int, Modification> modDict)
        {
            string fullSeq = "";
            for (int i = 0; i < BaseSeq.Length; i++)
            {
                if (modDict.ContainsKey(i))
                {
                    fullSeq += "[" + modDict[i].ModificationType + ":" + modDict[i].IdWithMotif + "]";
                }
                fullSeq += BaseSeq[i];
            }
            return fullSeq;
        }

        public static string GetTabSepHeaderGlyco()
        {
            var sb = new StringBuilder();
            sb.Append("File Name" + '\t');
            sb.Append("Scan Number" + '\t');
            sb.Append("Scan Retention Time" + '\t');

            sb.Append("Precursor Charge" + '\t');
            sb.Append("Precursor Mass" + '\t');

            sb.Append("Protein Accession" + '\t');
            sb.Append("Protein Name" + '\t');
            sb.Append("Start and End Residues In Protein" + '\t');

            sb.Append("Base Sequence" + '\t');
            sb.Append("Full Sequence" + '\t');
            sb.Append("Peptide Monoisotopic Mass" + '\t');

            sb.Append("Decoy" + '\t');
            sb.Append("QValue" + '\t');

            sb.Append("GlycanStructure" + '\t');
            sb.Append("GlycanMass" + '\t');
            sb.Append("GlycanComposition(H,N,A,G,F)" + '\t');
            return sb.ToString();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(FileName + '\t');
            sb.Append(ScanNum.ToString() + '\t');
            sb.Append(RT.ToString() + '\t');
            sb.Append(ChargeState.ToString() + '\t');
            sb.Append(PrecursorMass.ToString() + '\t');
            sb.Append(ProteinAccess + '\t');
            sb.Append(ProteinName + '\t');
            sb.Append(ProteinStartEnd + '\t');
            sb.Append(BaseSeq + '\t');
            sb.Append(glycoPwsm.FullSequence + '\t');
            sb.Append(MonoisotopicMass.ToString() + '\t');
            sb.Append(Decoy ? "Y":"N" + '\t');
            sb.Append(QValue.ToString() + '\t');
            sb.Append(glycan.Struc + '\t');
            sb.Append(glycan.Mass.ToString() + '\t');
            sb.Append(Glycan.GetKindString(glycan.Kind) + '\t');
            return sb.ToString();
        }
    }
}

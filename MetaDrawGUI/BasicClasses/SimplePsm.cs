using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Proteomics.Fragmentation;
using EngineLayer;
using EngineLayer.CrosslinkSearch;
using EngineLayer.GlycoSearch;
using Proteomics;
using Proteomics.ProteolyticDigestion;
using MassSpectrometry;

namespace MetaDrawGUI
{
    public class SimplePsm
    {
        public SimplePsm(string line, char[] split, Dictionary<string, int> parsedHeader, TsvType tsvType)
        {
            switch (tsvType)
            {
                case TsvType.pGlyco:
                    generateSimplePsm_pGlyco(line, split, parsedHeader);
                    break;
                case TsvType.GlycReSoft:
                    generateSimplePsm_GlycReSoft(line, split, parsedHeader);
                    break;
                case TsvType.Byonic:
                    generateSimplePsm_Byonic(line, split, parsedHeader);
                    break;
                case TsvType.pTOP:
                    generateSimplePsm_pTop(line, split, parsedHeader);
                    break;
                case TsvType.Promex:
                    generateSimplePsm_Promex(line, split, parsedHeader);
                    break;
                default:
                    break;
            }
            
        }

        public string FileName { get; set; }
        public int ScanNum { get; set; }
        public double RT { get; set; }
        public double PrecursorMass { get; set; }
        public double PrecursorMz { get; set; }
        public int ChargeState { get; set; }
        public double MonoisotopicMass { get; set; }  
        public string BaseSeq { get; set; }
        public string FullSeq { get; set; }
        public string Mod { get; set; }
        public double QValue { get; set; }
        public bool Decoy { get; set; }
        public string ProteinName { get; set; }       
        public string ProteinAccess { get; set; }
        public string ProteinStartEnd { get; set; }
        public List<MatchedFragmentIon> MatchedIons { get; set; }

        //Crosslink
        public string BetaPeptideBaseSequence { get; set; }
        public List<MatchedFragmentIon> BetaPeptideMatchedIons { get; set; }

        //Glycopeptide
        public string iD { get; set; }
        public double PeptideMassNoGlycan { get; set; }
        public double glycanMass { get; set; }
        private string GlycanStructure { get; set; }
        public byte[] glycanKind { get; set; }
        public int glycanAGNumber { get; set; }
        public string glycanString { get; set; }
        public Glycan glycan { get; set; }
        public PeptideWithSetModifications glycoPwsm { get; set; }

        //pTOP
        public int MatchedPeakNum { get; set; }
        public int NterMatchedPeakNum { get; set; }
        public int CTerMatchedPeakNum { get; set; }
        public double NTerMatchedPeakIntensityRatio { get; set; }
        public double CTerMatchedPeakIntensityRatio { get; set; }


        private void generateSimplePsm_pGlyco(string line, char[] split, Dictionary<string, int> parsedHeader)
        {
            var spl = line.Split(split);

            //this is special for pGlyco
            FileName = spl[parsedHeader[PsmTsvHeader_pGlyco.FileName]].Split('.')[0];
            ScanNum = int.Parse(spl[parsedHeader[PsmTsvHeader_pGlyco.FileName]].Split('.')[1]);
            RT = double.Parse(spl[parsedHeader[PsmTsvHeader_pGlyco.Ms2ScanRetentionTime]]) / 60;
            PrecursorMass = double.Parse(spl[parsedHeader[PsmTsvHeader_pGlyco.PrecursorMH]]) - 1.0073;
            ChargeState = int.Parse(spl[parsedHeader[PsmTsvHeader_pGlyco.FileName]].Split('.')[3]);

            if (parsedHeader[PsmTsvHeader_pGlyco.GlyStruct] > 0)
            {
                GlycanStructure = spl[parsedHeader[PsmTsvHeader_pGlyco.GlyStruct]].Trim();
                glycan = Glycan.Struct2Glycan(GlycanStructure, 0);
            }

            glycanKind = Glycan.GetKindFromKindString(spl[parsedHeader[PsmTsvHeader_pGlyco.GlycanKind]]);
            glycanAGNumber = glycanKind[2] + glycanKind[3];
            glycanString = Glycan.GetKindString(glycanKind);
            glycanMass = double.Parse(spl[parsedHeader[PsmTsvHeader_pGlyco.GlycanMass]]);
           
            MonoisotopicMass = double.Parse(spl[parsedHeader[PsmTsvHeader_pGlyco.PeptideMH]]) + glycanMass - 1.0073;
            PeptideMassNoGlycan = MonoisotopicMass - glycanMass;
            var pBaseSeq = spl[parsedHeader[PsmTsvHeader_pGlyco.BaseSequence]].Trim();
            StringBuilder sb = new StringBuilder(pBaseSeq);
            sb[pBaseSeq.IndexOf('J')] = 'N';
            BaseSeq = sb.ToString();
            Mod = spl[parsedHeader[PsmTsvHeader_pGlyco.Mods]].Trim();
            
            if (glycan !=null)
            {
                Modification modification = GlycoPeptides.GlycanToModification(glycan);


                Dictionary<int, Modification> testMods = GetMods_pGlyco(Mod, AllPossibleMods_pGlyco);
                testMods.Add(pBaseSeq.IndexOf('J') + 1, modification);
                FullSeq = GetFullSeq(BaseSeq, testMods);

                var AllModsKnownDictionary = new Dictionary<string, Modification>();
                foreach (Modification mod in testMods.Values)
                {
                    if (!AllModsKnownDictionary.ContainsKey(mod.IdWithMotif))
                    {
                        AllModsKnownDictionary.Add(mod.IdWithMotif, mod);
                    }
                    // no error thrown if multiple mods with this ID are present - just pick one
                }
                glycoPwsm = new PeptideWithSetModifications(FullSeq, AllModsKnownDictionary);
            }

            QValue = double.Parse(spl[parsedHeader[PsmTsvHeader_pGlyco.QValue]]);
            Decoy = false;
            ProteinAccess = spl[parsedHeader[PsmTsvHeader_pGlyco.ProteinAccession]].Split('|')[1];
            ProteinName = spl[parsedHeader[PsmTsvHeader_pGlyco.ProteinAccession]].Split('|', '/')[2];
            int ProSite = int.Parse(spl[parsedHeader[PsmTsvHeader_pGlyco.ProSite]].Split('/')[0]);
            int GlySite = int.Parse(spl[parsedHeader[PsmTsvHeader_pGlyco.GlySite]]);
            int peptideLength = BaseSeq.Count();
            ProteinStartEnd = "[" + (ProSite - GlySite).ToString() + " to " + (ProSite + peptideLength - GlySite).ToString() + "]";

        }

        private void generateSimplePsm_GlycReSoft(string line, char[] split, Dictionary<string, int> parsedHeader)
        {
            var spl = line.Split(split);

            FileName = "";
            ScanNum = int.Parse(spl[parsedHeader[PsmTsvHeader_GlycReSoft.scan_id]].Split('=')[3]);

            RT = double.Parse(spl[parsedHeader[PsmTsvHeader_GlycReSoft.scan_time]]);
            MonoisotopicMass = double.Parse(spl[parsedHeader[PsmTsvHeader_GlycReSoft.neutral_mass]]); //TO CHECK: monoisotopic mass?
            ChargeState = int.Parse(spl[parsedHeader[PsmTsvHeader_GlycReSoft.charge]]);
            double massAccuracy = double.Parse(spl[parsedHeader[PsmTsvHeader_GlycReSoft.mass_accuracy]]);
            PrecursorMass = MonoisotopicMass - massAccuracy;
            BaseSeq = GetBaseSeq_GlycReSoft(spl[parsedHeader[PsmTsvHeader_GlycReSoft.glycopeptide]]);
            glycanKind = GetGlycan_GlycReSoft(spl[parsedHeader[PsmTsvHeader_GlycReSoft.glycopeptide]]);
            glycan = Glycan.Kind2Glycan(glycanKind);
            Mod = spl[parsedHeader[PsmTsvHeader_GlycReSoft.mass_shift_name]];  //TO DO: Mod is not converted to MetaMorpheus mod.
            Modification modification = GlycanToModificationWithNoMass(glycan);
            Dictionary<int, Modification> testMods = new Dictionary<int, Modification>();
            testMods.Add(spl[parsedHeader[PsmTsvHeader_GlycReSoft.glycopeptide]].IndexOf('('), modification);
            FullSeq = GetFullSeq(BaseSeq, testMods);

            var AllModsKnownDictionary = new Dictionary<string, Modification>();
            foreach (Modification mod in testMods.Values)
            {
                if (!AllModsKnownDictionary.ContainsKey(mod.IdWithMotif))
                {
                    AllModsKnownDictionary.Add(mod.IdWithMotif, mod);
                }
                // no error thrown if multiple mods with this ID are present - just pick one
            }
            glycoPwsm = new PeptideWithSetModifications(FullSeq, AllModsKnownDictionary);

            QValue = double.Parse(spl[parsedHeader[PsmTsvHeader_GlycReSoft.q_value]]);
            Decoy = false;
            ProteinAccess = spl[parsedHeader[PsmTsvHeader_GlycReSoft.protein_name]].Split('|')[1]; //Only works for uniprot fasta
            ProteinName = spl[parsedHeader[PsmTsvHeader_GlycReSoft.protein_name]].Split('|')[2].Split('_')[0]; //Only works for uniprot fasta
            ProteinStartEnd = "[" + spl[parsedHeader[PsmTsvHeader_GlycReSoft.peptide_start]] + " to " + spl[parsedHeader[PsmTsvHeader_GlycReSoft.peptide_end]] + "]";

        }

        private void generateSimplePsm_Byonic(string line, char[] split, Dictionary<string, int> parsedHeader)
        {
            var spl = line.Split(split);

            //this is special for pGlyco
            FileName = spl[parsedHeader[PsmTsvHeader_Byonic.FileName]];
            RT = double.Parse(spl[parsedHeader[PsmTsvHeader_Byonic.Ms2ScanRetentionTime]]);
            MonoisotopicMass = double.Parse(spl[parsedHeader[PsmTsvHeader_Byonic.PrecursorMH]]) - 1.0073;

            glycanKind = Glycan.GetKindFromByonic(spl[parsedHeader[PsmTsvHeader_Byonic.GlycanKind]]);
            glycanAGNumber = glycanKind[2] + glycanKind[3];
            glycanString = Glycan.GetKindString(glycanKind);
            glycanMass = Glycan.GetMass(glycanKind);
            PeptideMassNoGlycan = MonoisotopicMass - glycanMass/1E5;

            BaseSeq = spl[parsedHeader[PsmTsvHeader_Byonic.BaseSequence]];
            Mod = spl[parsedHeader[PsmTsvHeader_Byonic.Mods]];
            FullSeq = spl[parsedHeader[PsmTsvHeader_Byonic.FullSeq]];
            int peptideLength = BaseSeq.Count();

        }

        private void generateSimplePsm_pTop(string line, char[] split, Dictionary<string, int> parsedHeader)
        {
            var spl = line.Split(split);
            FileName = spl[parsedHeader[PsmTsvHeader_pTop.FileName]].Split('.').First();
            ScanNum = int.Parse(spl[parsedHeader[PsmTsvHeader_pTop.ScanNum]]);
            ChargeState = int.Parse(spl[parsedHeader[PsmTsvHeader_pTop.ChargeState]]);
            PrecursorMz = double.Parse(spl[parsedHeader[PsmTsvHeader_pTop.PrecursorMz]]);
            PrecursorMass = double.Parse(spl[parsedHeader[PsmTsvHeader_pTop.PrecursorMass]]);
            BaseSeq = spl[parsedHeader[PsmTsvHeader_pTop.BaseSequence]];
            Mod = spl[parsedHeader[PsmTsvHeader_pTop.PTMs]];
            FullSeq = BaseSeq + "_" + Mod;
            MatchedPeakNum = int.Parse(spl[parsedHeader[PsmTsvHeader_pTop.MatchedPeaks]]);
            NterMatchedPeakNum = int.Parse(spl[parsedHeader[PsmTsvHeader_pTop.NtermMatchedIons]]);
            CTerMatchedPeakNum  = int.Parse(spl[parsedHeader[PsmTsvHeader_pTop.CtermMatchedIons]]);
            NTerMatchedPeakIntensityRatio = double.Parse(spl[parsedHeader[PsmTsvHeader_pTop.NtermMatchedIntensityRatio]]);
            CTerMatchedPeakIntensityRatio = double.Parse(spl[parsedHeader[PsmTsvHeader_pTop.CtermMatchedIntensityRatio]]);
        }

        private void generateSimplePsm_Promex(string line, char[] split, Dictionary<string, int> parsedHeader)
        {
            var spl = line.Split(split);
            ScanNum = int.Parse(spl[parsedHeader[PsmTsvHeader_Promex.ScanNum]]);
            ChargeState = int.Parse(spl[parsedHeader[PsmTsvHeader_Promex.ChargeState]]);
            PrecursorMz = double.Parse(spl[parsedHeader[PsmTsvHeader_Promex.PrecursorMz]]);
            PrecursorMass = double.Parse(spl[parsedHeader[PsmTsvHeader_Promex.PrecursorMass]]);
            BaseSeq = spl[parsedHeader[PsmTsvHeader_Promex.BaseSequence]];
            Mod = spl[parsedHeader[PsmTsvHeader_Promex.PTMs]];
            MatchedPeakNum = int.Parse(spl[parsedHeader[PsmTsvHeader_Promex.MatchedPeaks]]);
        }

        //TO DO: Bug may exist for the PrecursorMH, which is different from PrecursorMass.
        public static List<MatchedFragmentIon> GetMatchedIons(PeptideWithSetModifications glycoPwsm, double precursorMH, int chargeState, CommonParameters commonParameters, MsDataScan msDataScan)
        {
            List<Product> peptideTheorProducts = glycoPwsm.Fragment(commonParameters.DissociationType, commonParameters.DigestionParams.FragmentationTerminus).ToList();
            Ms2ScanWithSpecificMass scanWithMass = new Ms2ScanWithSpecificMass(msDataScan, precursorMH, chargeState, null, commonParameters);
            List<MatchedFragmentIon> matchedIons = MetaMorpheusEngine.MatchFragmentIons(scanWithMass, peptideTheorProducts, commonParameters);
            return matchedIons;
        }

        private static Dictionary<string, Modification> AllPossibleMods_pGlyco = GetAllPossibleMods_pGlyco();

        private static Dictionary<string, Modification> GetAllPossibleMods_pGlyco()
        {
            Dictionary<string, Modification> allPossibleMods = new Dictionary<string, Modification>();

            var m1 = GlobalVariables.AllModsKnown.Where(p => p.IdWithMotif == "Carbamidomethyl on C").FirstOrDefault();
            allPossibleMods.Add("Carbamidomethyl[C]", m1);

            var m2 = GlobalVariables.AllModsKnown.Where(p => p.IdWithMotif == "Oxidation on M").FirstOrDefault();
            allPossibleMods.Add("Oxidation[M]", m2);

            var m3 = GlobalVariables.AllModsKnown.Where(p => p.IdWithMotif == "Acetylation" && p.ModificationType == "N-terminal.").FirstOrDefault();
            allPossibleMods.Add("Acetyl[ProteinN-term]", m3);

            return allPossibleMods;
        }

        private static Dictionary<int, Modification> GetMods_pGlyco(string mod, Dictionary<string, Modification> AllPossibleMods)
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

        public static Modification GlycanToModificationWithNoMass(Glycan glycan)
        {
            //string[] motifs = new string[] { "Nxt", "Nxs" };
            ModificationMotif.TryGetMotif("N", out ModificationMotif finalMotif); //TO DO: only one motif can be write here.
            var id = Glycan.GetKindString(glycan.Kind);

            Modification modification = new Modification(
                _originalId: id,
                _modificationType: "N-Glycosylation",
                _monoisotopicMass: (double)glycan.Mass / 1E5,
                _locationRestriction: "Anywhere.",
                _target: finalMotif
            );
            return modification;
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

        private static string GetBaseSeq_GlycReSoft(string glycopeptide)
        {
            string baseSeq = "";
            bool add = true;
            foreach (var c in glycopeptide)
            {
                if (c == '(' || c == '{')
                {
                    add = false;
                    continue;
                }
                if (c == ')')
                {
                    add = true;
                    continue;
                }
                if (add)
                {
                    baseSeq += c;
                }
            }
            return baseSeq;
        }

        public static byte[] GetGlycan_GlycReSoft(string line)
        {      
            byte[] kind = new byte[5] { 0, 0, 0, 0, 0};
            var y = line.Split('{', '}');
            var x = y[1].Split(';', ':');
            int i = 0;
            while (i < x.Length - 1)
            {
                switch (x[i].Trim())
                {
                    case "Hex":
                        kind[0] = byte.Parse(x[i + 1]);
                        break;
                    case "HexNAc":
                        kind[1] = byte.Parse(x[i + 1]);
                        break;
                    case "Neu5Ac":
                        kind[2] = byte.Parse(x[i + 1]);
                        break;
                    case "Neu5Gc":
                        kind[3] = byte.Parse(x[i + 1]);
                        break;
                    case "Fuc":
                        kind[4] = byte.Parse(x[i + 1]);
                        break;
                    case "Xyl":
                        kind[5] = byte.Parse(x[i + 1]);
                        break;
                    case "KND":
                        kind[6] = byte.Parse(x[i + 1]);
                        break;
                    case "Phosphate":
                        kind[7] = byte.Parse(x[i + 1]);
                        break;
                    case "Sulfate":
                        kind[8] = byte.Parse(x[i + 1]);
                        break;
                    case "HexA":
                        kind[9] = byte.Parse(x[i + 1]);
                        break;
                    default:
                        break;
                }
                i = i + 2;
            }
            return kind;
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

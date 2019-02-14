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
            var spl = line.Split(split);
            ScanNum = int.Parse(spl[parsedHeader[PsmTsvHeader_pGlyco.FileName]].Split('.')[1]); //this is special for pGlyco
            PrecursorMH = double.Parse(spl[parsedHeader[PsmTsvHeader_pGlyco.PrecursorMH]]);
            ChargeState = int.Parse(spl[parsedHeader[PsmTsvHeader_pGlyco.FileName]].Split('.')[3]);
            BaseSeq = spl[parsedHeader[PsmTsvHeader_pGlyco.BaseSequence]].Trim();
            Mod = spl[parsedHeader[PsmTsvHeader_pGlyco.Mods]].Trim();
            GlycoStructure = spl[parsedHeader[PsmTsvHeader_pGlyco.GlyStruct]].Trim();          
        }

        public int ScanNum { get;}
        public double PrecursorMH { get; }
        public int ChargeState { get; }
        public string BaseSeq { get; }
        public string Mod { get;  }
        public string GlycoStructure { get; }
        public string BetaPeptideBaseSequence { get; set; }
        public List<MatchedFragmentIon> MatchedIons { get; set; }
        public List<MatchedFragmentIon> BetaPeptideMatchedIons { get; set; }

        private static Dictionary<string, Modification> AllPossibleMods = GetAllPossibleMods();

        public static List<MatchedFragmentIon> GetMatchedIons(string baseSeq, string mods, double precursorMH, int chargeState, CommonParameters commonParameters, MsDataScan msDataScan)
        {
            string fullSeq;
            if (mods == "null")
            {
                fullSeq = baseSeq;
            }
            else
            {
                fullSeq = GetFullSeq(baseSeq, GetMods(mods, AllPossibleMods));
            }
            PeptideWithSetModifications peptide = new PeptideWithSetModifications(fullSeq, GlobalVariables.AllModsKnownDictionary);
            List<Product> peptideTheorProducts = peptide.Fragment(commonParameters.DissociationType, commonParameters.DigestionParams.FragmentationTerminus).ToList();
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
                if (m!="")
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
    }
}

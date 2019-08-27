﻿using Proteomics.Fragmentation;
using Proteomics.ProteolyticDigestion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Proteomics;

namespace EngineLayer.GlycoSearch
{
    public class GlycoSpectralMatch : PeptideSpectralMatch
    {
        public GlycoSpectralMatch(PeptideWithSetModifications theBestPeptide, int notch, double score, int scanIndex, Ms2ScanWithSpecificMass scan, DigestionParams digestionParams, List<MatchedFragmentIon> matchedFragmentIons)
            : base(theBestPeptide, notch, score, scanIndex, scan, digestionParams, matchedFragmentIons)
        {
            this.XLTotalScore = score;
        }

        public List<int> LinkPositions { get; set; }
        public double DeltaScore { get; set; }
        public double XLTotalScore { get; set; } //alpha + beta psmCross
        public int XlProteinPos { get; set; }
        public List<int> XlRank { get; set; } //only contain 2 intger, consider change to Tuple

        public PsmCrossType CrossType { get; set; }
        public Dictionary<int, List<MatchedFragmentIon>> ChildMatchedFragmentIons { get; set; }
        //Glyco properties
        public List<Glycan> Glycan { get; set; }
        public List<GlycanBox> glycanBoxes { get; set; }
        public double PeptideScore { get; set; }
        public double GlycanScore { get; set; }
        public double DiagnosticIonScore { get; set; }


        public static List<int> GetPossibleCrosslinkerModSites(char[] crosslinkerModSites, PeptideWithSetModifications peptide)
        {
            List<int> possibleXlPositions = new List<int>();
            bool wildcard = crosslinkerModSites.Any(p => p == 'X');

            for (int r = 0; r < peptide.BaseSequence.Length; r++)
            {
                if (crosslinkerModSites.Contains(peptide.BaseSequence[r]) || wildcard)
                {
                    possibleXlPositions.Add(r + 1);
                }
            }

            return possibleXlPositions;
        }

        //Motif should be writen with required form
        public static List<int> GetPossibleModSites(PeptideWithSetModifications peptide, string[] motifs)
        {
            List<int> possibleModSites = new List<int>();

            List<Modification> modifications = new List<Modification>();

            foreach (var mtf in motifs)
            {
                if (ModificationMotif.TryGetMotif(mtf, out ModificationMotif aMotif))
                {
                    Modification modWithMotif = new Modification(_target: aMotif, _locationRestriction: "Anywhere.");
                    modifications.Add(modWithMotif);
                }
            }

            foreach (var modWithMotif in modifications)
            {
                for (int r = 0; r < peptide.Length; r++)
                {
                    if (peptide.AllModsOneIsNterminus.Keys.Contains(r+2))
                    {
                        continue;
                    }
                    
                    //FullSequence is used here to avoid duplicated modification on same sites?
                    if (ModificationLocalization.ModFits(modWithMotif, peptide.BaseSequence, r + 1, peptide.Length, r + 1))
                    {
                        possibleModSites.Add(r + 2);
                    }
                }
            }

            return possibleModSites;
        }

        /// <summary>
        /// Rank experimental mass spectral peaks by intensity
        /// </summary>
        public static int[] GenerateIntensityRanks(double[] experimental_intensities)
        {
            var y = experimental_intensities.ToArray();
            var x = Enumerable.Range(1, y.Length).OrderBy(p => p).ToArray();
            Array.Sort(y, x);
            var experimental_intensities_rank = Enumerable.Range(1, y.Length).OrderByDescending(p => p).ToArray();
            Array.Sort(x, experimental_intensities_rank);
            return experimental_intensities_rank;
        }

        public static string GetTabSepHeaderSingle()
        {
            var sb = new StringBuilder();
            sb.Append("File Name" + '\t');
            sb.Append("Scan Number" + '\t');
            sb.Append("Precursor Scan Number" + '\t');
            sb.Append("Precursor MZ" + '\t');
            sb.Append("Precursor Charge" + '\t');
            sb.Append("Precursor Mass" + '\t');
            sb.Append("Cross Type" + '\t');
            sb.Append("Link Residues" + "\t");

            sb.Append("Peptide" + '\t');
            sb.Append("Protein Accession" + '\t');
            sb.Append("Protein Link Site" + '\t');
            sb.Append("Base Sequence" + '\t');
            sb.Append("Full Sequence" + '\t');
            sb.Append("Peptide Monoisotopic Mass" + '\t');
            sb.Append("Score" + '\t');
            sb.Append("Rank" + '\t');

            sb.Append("Matched Ion Series" + '\t');
            sb.Append("Matched Ion Mass-To-Charge Ratios" + '\t');
            sb.Append("Matched Ion Mass Diff (Da)" + '\t');
            sb.Append("Matched Ion Mass Diff (Ppm)" + '\t');
            sb.Append("Matched Ion Intensities" + '\t');
            sb.Append("Matched Ion Counts" + '\t');
            sb.Append("Child Scans Matched Ion Series" + '\t');
            sb.Append("Decoy/Contaminant/Target" + '\t');
            sb.Append("QValue" + '\t');

            return sb.ToString();
        }

        public static string GetTabSepHeaderGlyco()
        {
            var sb = new StringBuilder();
            sb.Append("File Name" + '\t');
            sb.Append("Scan Number" + '\t');
            sb.Append("Scan Retention Time" + '\t');
            sb.Append("Precursor Scan Number" + '\t');
            sb.Append("Precursor MZ" + '\t');
            sb.Append("Precursor Charge" + '\t');
            sb.Append("Precursor Mass" + '\t');
            sb.Append("Cross Type" + '\t');
            sb.Append("Link Residues" + "\t");

            sb.Append("Peptide" + '\t');
            sb.Append("Protein Accession" + '\t');
            sb.Append("Protein Name" + '\t');
            sb.Append("Start and End Residues In Protein" + '\t');
            sb.Append("Protein Link Site" + '\t');
            sb.Append("Base Sequence" + '\t');
            sb.Append("Full Sequence" + '\t');
            sb.Append("Peptide Monoisotopic Mass" + '\t');
            sb.Append("Score" + '\t');
            sb.Append("Rank" + '\t');

            sb.Append("Matched Ion Series" + '\t');
            sb.Append("Matched Ion Mass-To-Charge Ratios" + '\t');
            sb.Append("Matched Ion Mass Diff (Da)" + '\t');
            sb.Append("Matched Ion Mass Diff (Ppm)" + '\t');
            sb.Append("Matched Ion Intensities" + '\t');
            sb.Append("Matched Ion Counts" + '\t');
            sb.Append('\t');
            sb.Append("Decoy/Contaminant/Target" + '\t');
            sb.Append("Decoy" + '\t');
            sb.Append("QValue" + '\t');

            sb.Append("Total Score" + '\t');
            sb.Append("Peptide Score" + '\t');
            sb.Append("Glycan Score" + '\t');
            sb.Append("DiagonosticIon Score" + '\t');
            sb.Append("GlycanIDs" + '\t');
            sb.Append("GlycanDecoy" + '\t');
            sb.Append("GlycanStructure" + '\t');
            sb.Append("GlycanMass" + '\t');
            sb.Append("GlycanComposition(H,N,A,G,F)" + '\t');
            return sb.ToString();
        }

        public override string ToString()
        {
            string position = "";
            switch (CrossType)
            {
                case PsmCrossType.Single:
                    break;

                default:
                    position = "(" + LinkPositions[0].ToString() + ")";
                    break;
            }

            var sb = new StringBuilder();
            sb.Append(FullFilePath + "\t");
            sb.Append(ScanNumber + "\t");
            sb.Append(ScanRetentionTime + "\t");
            sb.Append(PrecursorScanNumber + "\t");
            sb.Append(ScanPrecursorMonoisotopicPeakMz + "\t");
            sb.Append(ScanPrecursorCharge + "\t");
            sb.Append(ScanPrecursorMass + "\t");
            sb.Append(CrossType.ToString() + "\t");          

            {
                sb.Append("\t");
            }

            sb.Append("\t");
            sb.Append(ProteinAccession + "\t");
            sb.Append(BestMatchingPeptides.First().Peptide.Protein.FullName + "\t");
            sb.Append("[" + OneBasedStartResidueInProtein.Value.ToString() + " to " + OneBasedEndResidueInProtein.Value.ToString() + "]" + '\t');
            sb.Append(XlProteinPos + "\t");
            sb.Append(BaseSequence + "\t");
            sb.Append(FullSequence + position + "\t");

            sb.Append((PeptideMonisotopicMass.HasValue ? PeptideMonisotopicMass.Value.ToString() : "---")); sb.Append("\t");
            sb.Append(Score + "\t");
            sb.Append(XlRank[0] + "\t");

            foreach (var mid in MatchedIonDataDictionary(this.MatchedFragmentIons))
            {
                sb.Append(mid.Value);
                sb.Append("\t");
            }

            StringBuilder childScanFragmentStringbuilder = new StringBuilder();
            if (ChildMatchedFragmentIons != null)
            {
                foreach (var childScan in ChildMatchedFragmentIons)
                {
                    int oneBasedScan = childScan.Key;
                    var matchedIonsDict = MatchedIonDataDictionary(childScan.Value);
                    childScanFragmentStringbuilder.Append("{" + oneBasedScan + "|" + matchedIonsDict[PsmTsvHeader.MatchedIonMzRatios] + "}");
                }
            }
            sb.Append(childScanFragmentStringbuilder.ToString() + "\t");


            sb.Append((IsDecoy) ? "D" : (IsContaminant) ? "C" : "T");
            sb.Append("\t");


            sb.Append(FdrInfo.QValue.ToString() + "\t");

            if (Glycan != null)
            {
                sb.Append(XLTotalScore + "\t");             
                sb.Append(PeptideScore + "\t");
                sb.Append(GlycanScore + "\t");
                sb.Append(DiagnosticIonScore + "\t");
                sb.Append(string.Join("|", Glycan.Select(p => p.GlyId.ToString()).ToArray())); sb.Append("\t");
                sb.Append(Glycan.First().Decoy? "D": "T"); sb.Append("\t");
                sb.Append(Glycan.First().Struc); sb.Append("\t");
                sb.Append((double)Glycan.First().Mass/1E5); sb.Append("\t");
                sb.Append(string.Join(" ", Glycan.First().Kind.Select(p => p.ToString()).ToArray())); sb.Append("\t");
            }

            if (glycanBoxes != null)
            {
                sb.Append(XLTotalScore + "\t");
                sb.Append(string.Join("|", glycanBoxes.First().glycans.Select(p => p.GlyId.ToString()).ToArray())); sb.Append("\t");
                sb.Append( "T"); sb.Append("\t");
                sb.Append(string.Join("|", glycanBoxes.First().glycans.Select(p => p.Struc.ToString()).ToArray())); sb.Append("\t");
                sb.Append((double)glycanBoxes.First().Mass / 1E5); sb.Append("\t");
                sb.Append(glycanBoxes.First().Kind.Select(p => p.ToString()).ToArray()); sb.Append("\t");
            }

            return sb.ToString();
        }

        public static Dictionary<string, string> MatchedIonDataDictionary(List<MatchedFragmentIon> matchedFragmentIons)
        {
            Dictionary<string, string> s = new Dictionary<string, string>();
            PsmTsvWriter.AddMatchedIonsData(s, matchedFragmentIons);
            return s;
        }
    }
}
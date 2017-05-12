﻿using MzLibUtil;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace EngineLayer
{
    public class ProteinLevelInfo
    {

        #region Public Constructors

        public ProteinLevelInfo(HashSet<PeptideWithSetModifications> hashSet, Tolerance fragmentTolerance, Ms2ScanWithSpecificMass theScan, List<ProductType> lp)
        {
            PeptidesWithSetModifications = hashSet;
            IsDecoy = PeptidesWithSetModifications.Any(bb => bb.Protein.IsDecoy);
            IsContaminant = PeptidesWithSetModifications.Any(bb => bb.Protein.IsContaminant);
            var representative = PeptidesWithSetModifications.First();
            var MatchedIonDictPositiveIsMatch = new Dictionary<ProductType, double[]>();
            foreach (var huh in lp)
            {
                var df = representative.ProductMassesMightHaveDuplicatesAndNaNs(new List<ProductType> { huh });
                Array.Sort(df);
                double[] matchedIonMassesListPositiveIsMatch = new double[df.Length];
                PsmParent.MatchIons(theScan.TheScan, fragmentTolerance, df, matchedIonMassesListPositiveIsMatch);
                MatchedIonDictPositiveIsMatch.Add(huh, matchedIonMassesListPositiveIsMatch);
            }

            var localizedScores = new List<double>();
            for (int indexToLocalize = 0; indexToLocalize < representative.Length; indexToLocalize++)
            {
                PeptideWithSetModifications localizedPeptide = representative.Localize(indexToLocalize, theScan.PrecursorMass - representative.MonoisotopicMass);

                var gg = localizedPeptide.ProductMassesMightHaveDuplicatesAndNaNs(lp);
                Array.Sort(gg);
                double[] matchedIonMassesListPositiveIsMatch = new double[gg.Length];
                var score = PsmParent.MatchIons(theScan.TheScan, fragmentTolerance, gg, matchedIonMassesListPositiveIsMatch);
                localizedScores.Add(score);
            }

            MatchedIonMassesListPositiveIsMatch = MatchedIonDictPositiveIsMatch;
            LocalizedScores = localizedScores;
            PeptideMonoisotopicMass = representative.MonoisotopicMass;
            FullSequence = representative.Sequence;
            BaseSequence = representative.BaseSequence;
            MissedCleavages = representative.MissedCleavages;
            NumVariableMods = representative.NumMods - representative.numFixedMods;
            SequenceWithChemicalFormulas = representative.SequenceWithChemicalFormulas;
        }

        #endregion Public Constructors

        #region Public Properties

        public HashSet<PeptideWithSetModifications> PeptidesWithSetModifications { get; }
        public Dictionary<ProductType, double[]> MatchedIonMassesListPositiveIsMatch { get; }
        public List<double> LocalizedScores { get; }
        public string FullSequence { get; }
        public string BaseSequence { get; }
        public int MissedCleavages { get; }
        public double PeptideMonoisotopicMass { get; }
        public int NumVariableMods { get; }
        public string SequenceWithChemicalFormulas { get; }
        public bool IsContaminant { get; }
        public bool IsDecoy { get; }

        #endregion Public Properties

        #region Public Methods

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            var s = string.Join(" or ", PeptidesWithSetModifications.Select(b => b.Protein.Accession));
            if (s.Length > 32000)
                s = "too many";
            sb.Append(s + "\t");

            s = string.Join(" or ", PeptidesWithSetModifications.Select(b => b.Protein.FullName));
            if (s.Length > 32000)
                s = "too many";
            sb.Append(s + "\t");

            s = string.Join(" or ", PeptidesWithSetModifications.Select(b => b.PeptideDescription));
            if (s.Length > 32000)
                s = "too many";
            sb.Append(s + "\t");

            s = string.Join(" or ", PeptidesWithSetModifications.Select(b => "[" + b.OneBasedStartResidueInProtein + " to " + b.OneBasedEndResidueInProtein + "]"));
            if (s.Length > 32000)
                s = "too many";
            sb.Append(s + "\t");

            s = string.Join(" or ", PeptidesWithSetModifications.Select(b => b.PreviousAminoAcid));
            if (s.Length > 32000)
                s = "too many";
            sb.Append(s + "\t");

            s = string.Join(" or ", PeptidesWithSetModifications.Select(b => b.NextAminoAcid));
            if (s.Length > 32000)
                s = "too many";
            sb.Append(s + "\t");

            var representative = PeptidesWithSetModifications.First();

            sb.Append(representative.BaseSequence + "\t");
            sb.Append(representative.Sequence + "\t");
            sb.Append(NumVariableMods.ToString(CultureInfo.InvariantCulture) + '\t');
            sb.Append(string.Join(";", MatchedIonMassesListPositiveIsMatch.Select(b => b.Value.Count(c => c > 0))) + '\t');

            sb.Append("[");
            foreach (var kvp in MatchedIonMassesListPositiveIsMatch)
                sb.Append("[" + string.Join(",", kvp.Value.Where(b => b > 0).Select(b => b.ToString("F5", CultureInfo.InvariantCulture))) + "];");
            sb.Append("]" + '\t');

            sb.Append("[" + string.Join(",", LocalizedScores.Select(b => b.ToString("F3", CultureInfo.InvariantCulture))) + "]" + '\t');

            sb.Append(MissedCleavages.ToString(CultureInfo.InvariantCulture) + '\t');
            sb.Append(PeptideMonoisotopicMass.ToString("F5", CultureInfo.InvariantCulture) + '\t');

            if (IsDecoy)
                sb.Append("D");
            else if (IsContaminant)
                sb.Append("C");
            else
                sb.Append("T");

            return sb.ToString();
        }

        #endregion Public Methods

        #region Internal Methods

        internal static string GetTabSeparatedHeader()
        {
            var sb = new StringBuilder();

            // Could have MANY options
            sb.Append("Protein Accession" + '\t');
            sb.Append("Protein Name" + '\t');
            sb.Append("Peptide Description" + '\t');
            sb.Append("Start and End Residues In Protein" + '\t');
            sb.Append("Previous Amino Acid" + '\t');
            sb.Append("Next Amino Acid" + '\t');

            // Single info, common for all peptides/proteins
            sb.Append("Base Sequence" + '\t');
            sb.Append("Full Sequence" + '\t');
            sb.Append("Variable Mods" + '\t');
            sb.Append("Matched Ion Counts" + '\t');
            sb.Append("Matched Ion Masses" + '\t');
            sb.Append("Localized Scores" + '\t');
            sb.Append("Missed Cleavages" + '\t');
            sb.Append("Peptide Monoisotopic Mass" + '\t');
            sb.Append("Decoy/Contaminant/Target");
            return sb.ToString();
        }

        #endregion Internal Methods

    }
}
﻿using EngineLayer.ModernSearch;
using MzLibUtil;
using Proteomics;
using Proteomics.Fragmentation;
using Proteomics.ProteolyticDigestion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EngineLayer;
using MassSpectrometry;

namespace EngineLayer.GlycoSearch
{
    public class GlycoSearchEngine : ModernSearchEngine
    {
        protected readonly List<GlycoSpectralMatch>[] GlobalCsms;

        private bool IsOGlycoSearch;
        // crosslinker molecule
        private readonly bool GlycoSearchTopN;
        private readonly int TopN;
        private readonly int _maxOGlycanNum;

        private readonly bool SearchGlycan182;
        private readonly Tolerance PrecusorSearchMode;
        private readonly MassDiffAcceptor ProductSearchMode;

        private readonly List<int>[] SecondFragmentIndex;

        public GlycoSearchEngine(List<GlycoSpectralMatch>[] globalCsms, Ms2ScanWithSpecificMass[] listOfSortedms2Scans, List<PeptideWithSetModifications> peptideIndex,
            List<int>[] fragmentIndex, List<int>[] secondFragmentIndex, int currentPartition, CommonParameters commonParameters, 
             bool isOGlycoSearch, bool glycoSearchTop, int glycoSearchTopNum, bool searchGlycan182, int maxOGlycanNum, List<string> nestedIds)
            : base(null, listOfSortedms2Scans, peptideIndex, fragmentIndex, currentPartition, commonParameters, new OpenSearchMode(), 0, nestedIds)
        {
            this.GlobalCsms = globalCsms;
            this.IsOGlycoSearch = isOGlycoSearch;
            this.GlycoSearchTopN = glycoSearchTop;
            this.TopN = glycoSearchTopNum;
            this._maxOGlycanNum = maxOGlycanNum;

            SecondFragmentIndex = secondFragmentIndex;
            PrecusorSearchMode = commonParameters.PrecursorMassTolerance;
            ProductSearchMode = new SingleAbsoluteAroundZeroSearchMode(20); //For Oxinium ion only

            SearchGlycan182 = searchGlycan182;
            if (!isOGlycoSearch)
            {
                var NGlycans = Glycan.LoadGlycan(GlobalVariables.NGlycanLocation);

                if (SearchGlycan182)
                {
                    var NGlycans182 = Glycan.LoadKindGlycan(GlobalVariables.NGlycanLocation_182, NGlycans);
                    Glycans = NGlycans182.OrderBy(p => p.Mass).ToArray();
                }
                else
                {
                    Glycans = NGlycans.OrderBy(p => p.Mass).ToArray();
                }       
                
                DecoyGlycans = Glycan.BuildTargetDecoyGlycans(NGlycans);
            }
            else
            {
                GlycanBox.GlobalOGlycans = Glycan.LoadGlycan(GlobalVariables.OGlycanLocation).ToArray();
                GlycanBox.GlobalOGlycanModifications = GlycanBox.BuildGlobalOGlycanModifications(GlycanBox.GlobalOGlycans);
                OGlycanBoxes = GlycanBox.BuildOGlycanBoxes(_maxOGlycanNum).OrderBy(p=>p.Mass).ToArray();
            }
        }

        private Glycan[] Glycans {get;}
        private Glycan[] DecoyGlycans { get; }
        private GlycanBox[] OGlycanBoxes { get; }

        protected override MetaMorpheusEngineResults RunSpecific()
        {
            double progress = 0;
            int oldPercentProgress = 0;
            ReportProgress(new ProgressEventArgs(oldPercentProgress, "Performing crosslink search... " + CurrentPartition + "/" + CommonParameters.TotalPartitions, NestedIds));

            byte byteScoreCutoff = (byte)CommonParameters.ScoreCutoff;

            int maxThreadsPerFile = CommonParameters.MaxThreadsToUsePerFile;
            int[] threads = Enumerable.Range(0, maxThreadsPerFile).ToArray();
            Parallel.ForEach(threads, (scanIndex) =>
            {
                byte[] scoringTable = new byte[PeptideIndex.Count];
                List<int> idsOfPeptidesPossiblyObserved = new List<int>();

                byte[] secondScoringTable = new byte[PeptideIndex.Count];
                List<int> childIdsOfPeptidesPossiblyObserved = new List<int>();

                for (; scanIndex < ListOfSortedMs2Scans.Length; scanIndex += maxThreadsPerFile)
                {
                    // Stop loop if canceled
                    if (GlobalVariables.StopLoops) { return; }

                    // empty the scoring table to score the new scan (conserves memory compared to allocating a new array)
                    Array.Clear(scoringTable, 0, scoringTable.Length);
                    idsOfPeptidesPossiblyObserved.Clear();
                    var scan = ListOfSortedMs2Scans[scanIndex];

                    // get fragment bins for this scan
                    List<int> allBinsToSearch = GetBinsToSearch(scan, FragmentIndex, CommonParameters.DissociationType);
                    List<BestPeptideScoreNotch> bestPeptideScoreNotchList = new List<BestPeptideScoreNotch>();

                    //TO DO: limit the high bound limitation

                    // first-pass scoring
                    IndexedScoring(FragmentIndex, allBinsToSearch, scoringTable, byteScoreCutoff, idsOfPeptidesPossiblyObserved, scan.PrecursorMass, Double.NegativeInfinity, Double.PositiveInfinity, PeptideIndex, MassDiffAcceptor, 0, CommonParameters.DissociationType);

                    //child scan first-pass scoring
                    if (scan.ChildScans != null && CommonParameters.ChildScanDissociationType != DissociationType.LowCID)
                    {
                        Array.Clear(secondScoringTable, 0, secondScoringTable.Length);
                        childIdsOfPeptidesPossiblyObserved.Clear();

                        List<int> childBinsToSearch = new List<int>();

                        foreach (var aChildScan in scan.ChildScans)
                        {
                            var x = GetBinsToSearch(aChildScan, SecondFragmentIndex, CommonParameters.ChildScanDissociationType);
                            childBinsToSearch.AddRange(x);
                        }

                        IndexedScoring(SecondFragmentIndex, childBinsToSearch, secondScoringTable, byteScoreCutoff, childIdsOfPeptidesPossiblyObserved, scan.PrecursorMass, Double.NegativeInfinity, Double.PositiveInfinity, PeptideIndex, MassDiffAcceptor, 0, CommonParameters.ChildScanDissociationType);

                        foreach (var childId in childIdsOfPeptidesPossiblyObserved)
                        {
                            if (!idsOfPeptidesPossiblyObserved.Contains(childId))
                            {
                                idsOfPeptidesPossiblyObserved.Add(childId);
                            }
                            scoringTable[childId] = (byte)(scoringTable[childId] + secondScoringTable[childId]);
                        }
                    }

                    // done with indexed scoring; refine scores and create PSMs
                    if (idsOfPeptidesPossiblyObserved.Any())
                    {
                        if (GlycoSearchTopN)
                        {
                            // take top N hits for this scan
                            idsOfPeptidesPossiblyObserved = idsOfPeptidesPossiblyObserved.OrderByDescending(p => scoringTable[p]).Take(TopN).ToList();
                        }

                        foreach (var id in idsOfPeptidesPossiblyObserved)
                        {
                            PeptideWithSetModifications peptide = PeptideIndex[id];

                            int notch = MassDiffAcceptor.Accepts(scan.PrecursorMass, peptide.MonoisotopicMass);
                            bestPeptideScoreNotchList.Add(new BestPeptideScoreNotch(peptide, scoringTable[id], notch));
                        }

                        List<GlycoSpectralMatch> gsms;
                        if (IsOGlycoSearch == false)
                        {
                            gsms = FindNGlycopeptide(scan, bestPeptideScoreNotchList, scanIndex);
                        }
                        else
                        {
                            gsms = FindOGlycopeptide(scan, bestPeptideScoreNotchList, scanIndex, byteScoreCutoff);
                        }


                        if (gsms.Count == 0)
                        {
                            progress++;
                            continue;
                        }

                        if (GlobalCsms[scanIndex] == null)
                        {
                            GlobalCsms[scanIndex] = new List<GlycoSpectralMatch>();
                        }

                        GlobalCsms[scanIndex].AddRange(gsms.Where(p => p != null).OrderByDescending(p => p.TotalScore));
                    }

                    // report search progress
                    progress++;
                    var percentProgress = (int)((progress / ListOfSortedMs2Scans.Length) * 100);

                    if (percentProgress > oldPercentProgress)
                    {
                        oldPercentProgress = percentProgress;
                        ReportProgress(new ProgressEventArgs(percentProgress, "Performing crosslink search... " + CurrentPartition + "/" + CommonParameters.TotalPartitions, NestedIds));
                    }
                }
            });

            return new MetaMorpheusEngineResults(this);
        }

        private List<GlycoSpectralMatch> FindNGlycopeptide(Ms2ScanWithSpecificMass theScan, List<BestPeptideScoreNotch> theScanBestPeptide, int scanIndex)
        {
            List<GlycoSpectralMatch> possibleMatches = new List<GlycoSpectralMatch>();

            if (theScan.OxiniumIonNum < 2)
            {
                return possibleMatches;
            }

            for (int ind = 0; ind < theScanBestPeptide.Count; ind++)
            {
                //Considering coisolation, it doesn't mean it must from a glycopeptide even the scan contains oxinium ions.
                //if (XLPrecusorSearchMode.Accepts(theScan.PrecursorMass, theScanBestPeptide[ind].BestPeptide.MonoisotopicMass) >= 0)
                //{
                //    List<Product> products = theScanBestPeptide[ind].BestPeptide.Fragment(commonParameters.DissociationType, FragmentationTerminus.Both).ToList();
                //    var matchedFragmentIons = MatchFragmentIons(theScan, products, commonParameters);
                //    double score = CalculatePeptideScore(theScan.TheScan, matchedFragmentIons);

                //    var psmCrossSingle = new CrosslinkSpectralMatch(theScanBestPeptide[ind].BestPeptide, theScanBestPeptide[ind].BestNotch, score, scanIndex, theScan, commonParameters.DigestionParams, matchedFragmentIons);
                //    psmCrossSingle.CrossType = PsmCrossType.Single;
                //    psmCrossSingle.XlRank = new List<int> { ind };

                //    possibleMatches.Add(psmCrossSingle);
                //}

                if (theScan.OxiniumIonNum < 2)
                {
                    continue;
                }

                List<int> modPos = GlycoSpectralMatch.GetPossibleModSites(theScanBestPeptide[ind].BestPeptide, new string[] { "Nxt", "Nxs" });
                if (modPos.Count < 1)
                {
                    continue;
                }

                var possibleGlycanMassLow = theScan.PrecursorMass * (1 - 1E-5) - theScanBestPeptide[ind].BestPeptide.MonoisotopicMass;
                if (possibleGlycanMassLow < 200 || possibleGlycanMassLow > Glycans.Last().Mass)
                {
                    continue;
                }


                int iDLow = GlycoPeptides.BinarySearchGetIndex(Glycans.Select(p => (double)p.Mass / 1E5).ToArray(), possibleGlycanMassLow);

                while (iDLow < Glycans.Count() && PrecusorSearchMode.Within(theScan.PrecursorMass, theScanBestPeptide[ind].BestPeptide.MonoisotopicMass + (double)Glycans[iDLow].Mass / 1E5))
                {
                    double bestLocalizedScore = 0;
                    int bestSite = 0;
                    List<MatchedFragmentIon> bestMatchedIons = new List<MatchedFragmentIon>();
                    PeptideWithSetModifications peptideWithSetModifications = theScanBestPeptide[ind].BestPeptide;
                    foreach (int possibleSite in modPos)
                    {
                        var testPeptide = GlycoPeptides.GenerateGlycopeptide(possibleSite, theScanBestPeptide[ind].BestPeptide, Glycans[iDLow]);

                        List<Product> theoreticalProducts = testPeptide.Fragment(CommonParameters.DissociationType, FragmentationTerminus.Both).Where(p => p.ProductType != ProductType.M).ToList();
                        theoreticalProducts.AddRange(GlycoPeptides.GetGlycanYIons(theScan.PrecursorMass, Glycans[iDLow]));

                        var matchedIons = MatchOriginFragmentIons(theScan, theoreticalProducts, CommonParameters);

                        if (!GlycoPeptides.ScanTrimannosylCoreFilter(matchedIons, Glycans[iDLow]))
                        {
                            continue;
                        }

                        double score = CalculatePeptideScore(theScan.TheScan, matchedIons);

                        if (score > bestLocalizedScore)
                        {
                            peptideWithSetModifications = testPeptide;
                            bestLocalizedScore = score;
                            bestSite = possibleSite;
                            bestMatchedIons = matchedIons;
                        }

                    }

                    var psmCross = new GlycoSpectralMatch(peptideWithSetModifications, theScanBestPeptide[ind].BestNotch, bestLocalizedScore, scanIndex, theScan, CommonParameters.DigestionParams, bestMatchedIons);
                    psmCross.Glycan = new List<Glycan> { Glycans[iDLow] };
                    psmCross.GlycanScore = CalculatePeptideScore(theScan.TheScan, bestMatchedIons.Where(p => p.Annotation.Contains('M')).ToList());
                    psmCross.DiagnosticIonScore = CalculatePeptideScore(theScan.TheScan, bestMatchedIons.Where(p => p.Annotation.Contains('D')).ToList());
                    psmCross.PeptideScore = psmCross.TotalScore - psmCross.GlycanScore - psmCross.DiagnosticIonScore;
                    psmCross.Rank = ind;
                    psmCross.localizations = new List<int[]> { new int[] { bestSite - 1 } }; //TO DO: ambiguity modification site
                    possibleMatches.Add(psmCross);

                    iDLow++;
                }
            }

            //if (possibleMatches.Count != 0)
            //{
            //    possibleMatches = possibleMatches.OrderByDescending(p => p.Score).ToList();
            //    bestPsmCross = possibleMatches.First();
            //    bestPsmCross.ResolveAllAmbiguities();
            //    bestPsmCross.DeltaScore = bestPsmCross.XLTotalScore;
            //    if (possibleMatches.Count > 1)
            //    {
            //        //This DeltaScore will be 0 if there are more than one glycan matched.
            //        bestPsmCross.DeltaScore = Math.Abs(possibleMatches.First().Score - possibleMatches[1].Score);

            //        ////TO DO: Try to find other plausible glycans
            //        //for (int iPsm = 1; iPsm < possibleMatches.Count; iPsm++)
            //        //{
            //        //    possibleMatches[iPsm].ResolveAllAmbiguities();
            //        //    if (possibleMatches[iPsm].Score == bestPsmCross.Score && possibleMatches[iPsm].Glycan != null)
            //        //    {
            //        //        bestPsmCross.Glycan.Add(possibleMatches[iPsm].Glycan.First());
            //        //    }
            //        //}
            //    }
            //}

            if (possibleMatches.Count != 0)
            {
                possibleMatches = possibleMatches.OrderByDescending(p => p.Score).ToList();
            }
            return possibleMatches;
        }

        private List<GlycoSpectralMatch> FindOGlycopeptide(Ms2ScanWithSpecificMass theScan, List<BestPeptideScoreNotch> theScanBestPeptide, int scanIndex)
        {
            List<GlycoSpectralMatch> possibleMatches = new List<GlycoSpectralMatch>();

            for (int ind = 0; ind < theScanBestPeptide.Count; ind++)
            {
                if (PrecusorSearchMode.Within(theScan.PrecursorMass, theScanBestPeptide[ind].BestPeptide.MonoisotopicMass))
                {
                    List<Product> products = theScanBestPeptide[ind].BestPeptide.Fragment(CommonParameters.DissociationType, FragmentationTerminus.Both).ToList();
                    var matchedFragmentIons = MatchFragmentIons(theScan, products, CommonParameters);
                    double score = CalculatePeptideScore(theScan.TheScan, matchedFragmentIons);

                    var psmCrossSingle = new GlycoSpectralMatch(theScanBestPeptide[ind].BestPeptide, theScanBestPeptide[ind].BestNotch, score, scanIndex, theScan, CommonParameters.DigestionParams, matchedFragmentIons);
                    psmCrossSingle.Rank = ind;
                    psmCrossSingle.ResolveAllAmbiguities();

                    possibleMatches.Add(psmCrossSingle);
                }
                //TO DO: add if the scan contains diagnostic ions, the rule to check diagnostic ions
                else if ((theScan.PrecursorMass - theScanBestPeptide[ind].BestPeptide.MonoisotopicMass >= 100) && GlycoPeptides.ScanOxoniumIonFilter(theScan, ProductSearchMode, CommonParameters.DissociationType)>=1)
                {
                    //Using glycanBoxes
                    var possibleGlycanMassLow = theScan.PrecursorMass * (1 - 1E-5) - theScanBestPeptide[ind].BestPeptide.MonoisotopicMass;
                    if (possibleGlycanMassLow < 100 || possibleGlycanMassLow > OGlycanBoxes.Last().Mass)
                    {
                        continue;
                    }

                    int iDLow = GlycoPeptides.BinarySearchGetIndex(OGlycanBoxes.Select(p => (double)p.Mass / 1E5).ToArray(), possibleGlycanMassLow);

                    while(iDLow < OGlycanBoxes.Count() && (PrecusorSearchMode.Within(theScan.PrecursorMass, theScanBestPeptide[ind].BestPeptide.MonoisotopicMass + (double)OGlycanBoxes[iDLow].Mass/1E5)))
                    {
                        List<int> modPos = GlycoSpectralMatch.GetPossibleModSites(theScanBestPeptide[ind].BestPeptide, new string[] { "S", "T" });
                        if (modPos.Count >= OGlycanBoxes[iDLow].NumberOfGlycans)
                        {
                            var permutateModPositions = GlycoPeptides.GetPermutations(modPos, OGlycanBoxes[iDLow].GlycanIds);

                            double bestLocalizedScore = 0;
                            PeptideWithSetModifications[] bestPeptideWithMod = new PeptideWithSetModifications[1];
                            var bestMatchedIons = new List<MatchedFragmentIon>();
                            var bestChildMatchedIons = new Dictionary<int, List<MatchedFragmentIon>>();
                            List<int[]> localization = new List<int[]>();

                            foreach (var theModPositions in permutateModPositions)
                            {                              
                                var peptideWithMod = GlycoPeptides.OGlyGetTheoreticalPeptide(theModPositions.ToArray(), theScanBestPeptide[ind].BestPeptide, OGlycanBoxes[iDLow]);

                                var fragmentsForEachGlycoPeptide = GlycoPeptides.OGlyGetTheoreticalFragments(CommonParameters.DissociationType, theScanBestPeptide[ind].BestPeptide, peptideWithMod);

                                var matchedIons = MatchFragmentIons(theScan, fragmentsForEachGlycoPeptide, CommonParameters);

                                //TO DO: May need a new score function
                                double score = CalculatePeptideScore(theScan.TheScan, matchedIons);

                                var allMatchedChildIons = new Dictionary<int, List<MatchedFragmentIon>>();

                                foreach (var childScan in theScan.ChildScans)
                                {
                                    var childFragments = GlycoPeptides.OGlyGetTheoreticalFragments(CommonParameters.ChildScanDissociationType, theScanBestPeptide[ind].BestPeptide, peptideWithMod);

                                    var matchedChildIons = MatchFragmentIons(childScan, childFragments, CommonParameters);

                                    if (matchedChildIons == null)
                                    {
                                        continue;
                                    }

                                    allMatchedChildIons.Add(childScan.OneBasedScanNumber, matchedChildIons);
                                    double childScore = CalculatePeptideScore(childScan.TheScan, matchedChildIons);

                                    //TO DO:may think a different way to use childScore
                                    score += childScore;
                                }

                                if (bestLocalizedScore < score)
                                {
                                    bestPeptideWithMod[0] = peptideWithMod;
                                    bestLocalizedScore = score;
                                    bestMatchedIons = matchedIons;
                                    bestChildMatchedIons = allMatchedChildIons;
                                    localization.Clear();
                                    localization.Add(theModPositions.ToArray());
                                }
                                else if(bestLocalizedScore == score)
                                {
                                    localization.Add(theModPositions.ToArray());
                                }
                                
                            }

                            //TO CHECK: In theory, the bestPeptideWithMod[0] shouldn't be null, because the score always >= index-score >0. 
                            if (bestPeptideWithMod[0] != null)
                            {
                                var psmGlyco = new GlycoSpectralMatch(bestPeptideWithMod[0], theScanBestPeptide[ind].BestNotch, bestLocalizedScore, scanIndex, theScan, CommonParameters.DigestionParams, bestMatchedIons);
                                psmGlyco.glycanBoxes = new List<GlycanBox> { OGlycanBoxes[iDLow] };
                                psmGlyco.Rank = ind;
                                psmGlyco.ChildMatchedFragmentIons = bestChildMatchedIons;
                                psmGlyco.localizations = localization;
                                possibleMatches.Add(psmGlyco);
                            }
                        }

                        iDLow++;
                    }
                }
            }

            if (possibleMatches.Count != 0)
            {
                possibleMatches = possibleMatches.OrderByDescending(p => p.Score).ToList();
            }

            return possibleMatches;
        }

        private List<GlycoSpectralMatch> FindOGlycopeptide(Ms2ScanWithSpecificMass theScan, List<BestPeptideScoreNotch> theScanBestPeptide, int scanIndex, byte byteScoreCutoff)
        {
            List<GlycoSpectralMatch> possibleMatches = new List<GlycoSpectralMatch>();

            //Tuple<IsGlyco, GlycanBoxId, GlycanBox Mod sites, ind, UnModPeptide, GlycoPeptides>
            List<Tuple<bool, int, int[], int, PeptideWithSetModifications, PeptideWithSetModifications>> allPossiblePeptidesIsGlyco = new List<Tuple<bool, int, int[], int, PeptideWithSetModifications, PeptideWithSetModifications>>();

            //TO THINK: This here can be paralleled.
            for (int ind = 0; ind < theScanBestPeptide.Count; ind++)
            {
                if (PrecusorSearchMode.Within(theScan.PrecursorMass, theScanBestPeptide[ind].BestPeptide.MonoisotopicMass))
                {
                    allPossiblePeptidesIsGlyco.Add(new Tuple<bool, int, int[], int, PeptideWithSetModifications, PeptideWithSetModifications>(false, 0, null, ind, null, theScanBestPeptide[ind].BestPeptide));
                }
                //TO DO: the rule to check diagnostic ions
                else if ((theScan.PrecursorMass - theScanBestPeptide[ind].BestPeptide.MonoisotopicMass >= 100) && GlycoPeptides.ScanOxoniumIonFilter(theScan, ProductSearchMode, CommonParameters.DissociationType) >= 1)
                {
                    //Using glycanBoxes
                    var possibleGlycanMassLow = theScan.PrecursorMass * (1 - 1E-5) - theScanBestPeptide[ind].BestPeptide.MonoisotopicMass;
                    if (possibleGlycanMassLow < 100 || possibleGlycanMassLow > OGlycanBoxes.Last().Mass)
                    {
                        continue;
                    }

                    int iDLow = GlycoPeptides.BinarySearchGetIndex(OGlycanBoxes.Select(p => (double)p.Mass / 1E5).ToArray(), possibleGlycanMassLow);
                    List<int> modPos = GlycoSpectralMatch.GetPossibleModSites(theScanBestPeptide[ind].BestPeptide, new string[] { "S", "T" });
                    while (iDLow < OGlycanBoxes.Count() && (PrecusorSearchMode.Within(theScan.PrecursorMass, theScanBestPeptide[ind].BestPeptide.MonoisotopicMass + (double)OGlycanBoxes[iDLow].Mass / 1E5)))
                    {                
                        if (modPos.Count >= OGlycanBoxes[iDLow].NumberOfGlycans)
                        {
                            var permutateModPositions = GlycoPeptides.GetPermutations(modPos, OGlycanBoxes[iDLow].GlycanIds);


                            foreach (var theModPositions in permutateModPositions)
                            {
                                var peptideWithMod = GlycoPeptides.OGlyGetTheoreticalPeptide(theModPositions.ToArray(), theScanBestPeptide[ind].BestPeptide, OGlycanBoxes[iDLow]);

                                allPossiblePeptidesIsGlyco.Add(new Tuple<bool, int, int[], int, PeptideWithSetModifications, PeptideWithSetModifications>(true, iDLow, theModPositions, ind, theScanBestPeptide[ind].BestPeptide, peptideWithMod));
                            }

                        }

                        iDLow++;
                    }
                }
            }

            //TO THINK: There is a point at which there is a requirement for ion index search. Find the point, here assume n peaks match m peptides, each peptides match h peaks. 
            //If m*h > n, we should use index.
            if (allPossiblePeptidesIsGlyco.Count > 50)
            {
                //TO DO: Order or not.
                //allPossiblePeptidesIsGlyco = allPossiblePeptidesIsGlyco.OrderBy(p=>p.Item6.MonoisotopicMass).ToList();

                List<int> idsOfPeptidesPossiblyObserved = new List<int>();
                List<int> childIdsOfPeptidesPossiblyObserved = new List<int>();

                byte[] glycoScoringTable = new byte[allPossiblePeptidesIsGlyco.Count];
                
                //TO DO: Finish the generation of fragmentIndex. May need 2 fragmentIndex. 
                List<int>[] glycoFragmentIndex;
                List<int>[] glycoSecondFragmentIndex;
                double MaxFragmentSize = 10000.0;
                try
                {
                    //Possible largest fragment ions in QE-HF for bottom-up.
                    glycoFragmentIndex = new List<int>[(int)Math.Ceiling(MaxFragmentSize) * FragmentBinsPerDalton + 1];
                    glycoSecondFragmentIndex = new List<int>[(int)Math.Ceiling(MaxFragmentSize) * FragmentBinsPerDalton + 1];
                }
                catch (OutOfMemoryException)
                {
                    throw new MetaMorpheusException("FragmentIndex for localization!");
                }
                GenerateGlycoIonIndex(allPossiblePeptidesIsGlyco, glycoFragmentIndex, glycoSecondFragmentIndex, MaxFragmentSize);

                List<int> allBinsToSearch = GetBinsToSearch(theScan, glycoFragmentIndex, CommonParameters.DissociationType);
                IndexedScoring(glycoFragmentIndex, allBinsToSearch, glycoScoringTable, byteScoreCutoff, idsOfPeptidesPossiblyObserved, theScan.PrecursorMass, Double.NegativeInfinity, Double.PositiveInfinity, allPossiblePeptidesIsGlyco.Select(p=>p.Item6).ToList(), MassDiffAcceptor, 0, CommonParameters.DissociationType);

                if (theScan.ChildScans != null && CommonParameters.ChildScanDissociationType != DissociationType.LowCID)
                {
                    List<int> childBinsToSearch = new List<int>();

                    foreach (var aChildScan in theScan.ChildScans)
                    {
                        var x = GetBinsToSearch(aChildScan, glycoSecondFragmentIndex, CommonParameters.ChildScanDissociationType);
                        childBinsToSearch.AddRange(x);
                    }

                    IndexedScoring(glycoSecondFragmentIndex, childBinsToSearch, glycoScoringTable, byteScoreCutoff, childIdsOfPeptidesPossiblyObserved, theScan.PrecursorMass, Double.NegativeInfinity, Double.PositiveInfinity, allPossiblePeptidesIsGlyco.Select(p => p.Item6).ToList(), MassDiffAcceptor, 0, CommonParameters.ChildScanDissociationType);
                }


                foreach (var childId in childIdsOfPeptidesPossiblyObserved)
                {
                    if (!idsOfPeptidesPossiblyObserved.Contains(childId))
                    {
                        idsOfPeptidesPossiblyObserved.Add(childId);
                    }
                }

                List<int> idsOfPeptidesTopN = new List<int>();

                if (idsOfPeptidesPossiblyObserved.Any())
                {
                    int scoreAtTopN = 0;
                    int peptideCount = 0;

                    foreach (int id in idsOfPeptidesPossiblyObserved.OrderByDescending(p => glycoScoringTable[p]))
                    {
                        peptideCount++;
                        // Whenever the count exceeds the TopN that we want to keep, we removed everything with a score lower than the score of the TopN-th peptide in the ids list
                        if (peptideCount == 1)
                        {
                            scoreAtTopN = glycoScoringTable[1];
                        }

                        if (glycoScoringTable[id] < scoreAtTopN)
                        {
                            break;
                        }

                        idsOfPeptidesTopN.Add(id);
                    }
                }

                foreach (var id in idsOfPeptidesTopN)
                {
                    //TO DO: There is no need to score everyone of them again, considering they are almost the same.
                    if (allPossiblePeptidesIsGlyco[id].Item1)
                    {
                        var fragmentsForEachGlycoPeptide = GlycoPeptides.OGlyGetTheoreticalFragments(CommonParameters.DissociationType, allPossiblePeptidesIsGlyco[id].Item5, allPossiblePeptidesIsGlyco[id].Item6);

                        var matchedIons = MatchFragmentIons(theScan, fragmentsForEachGlycoPeptide, CommonParameters);

                        //TO DO: May need a new score function
                        double score = CalculatePeptideScore(theScan.TheScan, matchedIons);

                        var allMatchedChildIons = new Dictionary<int, List<MatchedFragmentIon>>();

                        foreach (var childScan in theScan.ChildScans)
                        {
                            var childFragments = GlycoPeptides.OGlyGetTheoreticalFragments(CommonParameters.ChildScanDissociationType, allPossiblePeptidesIsGlyco[id].Item5, allPossiblePeptidesIsGlyco[id].Item6);

                            var matchedChildIons = MatchFragmentIons(childScan, childFragments, CommonParameters);

                            if (matchedChildIons == null)
                            {
                                continue;
                            }

                            allMatchedChildIons.Add(childScan.OneBasedScanNumber, matchedChildIons);
                            double childScore = CalculatePeptideScore(childScan.TheScan, matchedChildIons);

                            //TO DO:may think a different way to use childScore
                            score += childScore;
                        }

                        var psmGlyco = new GlycoSpectralMatch(allPossiblePeptidesIsGlyco[id].Item6, 0, score, scanIndex, theScan, CommonParameters.DigestionParams, matchedIons);
                        

                        psmGlyco.glycanBoxes = new List<GlycanBox> { OGlycanBoxes[allPossiblePeptidesIsGlyco[id].Item2] };
                        psmGlyco.Rank = allPossiblePeptidesIsGlyco[id].Item4;
                        psmGlyco.ChildMatchedFragmentIons = allMatchedChildIons;

                        //TO DO:localization can have 2 level. The first level is glycan groups, the second level is positions.
                        //The logic changed, so the localization is not here. 
                        //psmGlyco.localizations = localization;

                        possibleMatches.Add(psmGlyco);

                    }
                    else
                    {
                        List<Product> products = allPossiblePeptidesIsGlyco[id].Item6.Fragment(CommonParameters.DissociationType, FragmentationTerminus.Both).ToList();
                        var matchedFragmentIons = MatchFragmentIons(theScan, products, CommonParameters);
                        double score = CalculatePeptideScore(theScan.TheScan, matchedFragmentIons);

                        var psmCrossSingle = new GlycoSpectralMatch(allPossiblePeptidesIsGlyco[id].Item6, 0, score, scanIndex, theScan, CommonParameters.DigestionParams, matchedFragmentIons);
                        psmCrossSingle.Rank = allPossiblePeptidesIsGlyco[id].Item4;
                        psmCrossSingle.ResolveAllAmbiguities();

                        possibleMatches.Add(psmCrossSingle);
                    }
                }

            }
            else
            {
                for (int id = 0; id < allPossiblePeptidesIsGlyco.Count; id++)
                {                    
                    if (allPossiblePeptidesIsGlyco[id].Item1)
                    {
                        var fragmentsForEachGlycoPeptide = GlycoPeptides.OGlyGetTheoreticalFragments(CommonParameters.DissociationType, allPossiblePeptidesIsGlyco[id].Item5, allPossiblePeptidesIsGlyco[id].Item6);

                        var matchedIons = MatchFragmentIons(theScan, fragmentsForEachGlycoPeptide, CommonParameters);

                        //TO DO: May need a new score function
                        double score = CalculatePeptideScore(theScan.TheScan, matchedIons);

                        var allMatchedChildIons = new Dictionary<int, List<MatchedFragmentIon>>();

                        foreach (var childScan in theScan.ChildScans)
                        {
                            var childFragments = GlycoPeptides.OGlyGetTheoreticalFragments(CommonParameters.ChildScanDissociationType, allPossiblePeptidesIsGlyco[id].Item5, allPossiblePeptidesIsGlyco[id].Item6);

                            var matchedChildIons = MatchFragmentIons(childScan, childFragments, CommonParameters);

                            if (matchedChildIons == null)
                            {
                                continue;
                            }

                            allMatchedChildIons.Add(childScan.OneBasedScanNumber, matchedChildIons);
                            double childScore = CalculatePeptideScore(childScan.TheScan, matchedChildIons);

                            //TO DO:may think a different way to use childScore
                            score += childScore;
                        }

                        var psmGlyco = new GlycoSpectralMatch(allPossiblePeptidesIsGlyco[id].Item6, 0, score, scanIndex, theScan, CommonParameters.DigestionParams, matchedIons);


                        psmGlyco.glycanBoxes = new List<GlycanBox> { OGlycanBoxes[allPossiblePeptidesIsGlyco[id].Item2] };
                        psmGlyco.Rank = allPossiblePeptidesIsGlyco[id].Item4;
                        psmGlyco.ChildMatchedFragmentIons = allMatchedChildIons;

                        //TO DO:localization can have 2 level. The first level is glycan groups, the second level is positions.
                        //The logic changed, so the localization is not here. 
                        //psmGlyco.localizations = localization;

                        possibleMatches.Add(psmGlyco);

                    }
                    else
                    {
                        List<Product> products = allPossiblePeptidesIsGlyco[id].Item6.Fragment(CommonParameters.DissociationType, FragmentationTerminus.Both).ToList();
                        var matchedFragmentIons = MatchFragmentIons(theScan, products, CommonParameters);
                        double score = CalculatePeptideScore(theScan.TheScan, matchedFragmentIons);

                        var psmCrossSingle = new GlycoSpectralMatch(allPossiblePeptidesIsGlyco[id].Item6, 0, score, scanIndex, theScan, CommonParameters.DigestionParams, matchedFragmentIons);
                        psmCrossSingle.Rank = allPossiblePeptidesIsGlyco[id].Item4;
                        psmCrossSingle.ResolveAllAmbiguities();

                        possibleMatches.Add(psmCrossSingle);
                    }
                }
            }

            if (possibleMatches.Count != 0)
            {
                possibleMatches = possibleMatches.OrderByDescending(p => p.Score).ToList();
            }

            return possibleMatches;
        }

        private void GenerateGlycoIonIndex(List<Tuple<bool, int, int[], int, PeptideWithSetModifications, PeptideWithSetModifications>> allPossiblePeptidesIsGlyco, List<int>[] fragmentIndex, List<int>[] secondFragmentIndex, double MaxFragmentSize)
        {
            for (int peptideId = 0; peptideId < allPossiblePeptidesIsGlyco.Count; peptideId++)
            {
                List<Product> fragments = new List<Product>();
                if (allPossiblePeptidesIsGlyco[peptideId].Item1)
                {
                    fragments = GlycoPeptides.OGlyGetTheoreticalFragments(CommonParameters.DissociationType, allPossiblePeptidesIsGlyco[peptideId].Item5, allPossiblePeptidesIsGlyco[peptideId].Item6);
                }
                else
                {
                    fragments = allPossiblePeptidesIsGlyco[peptideId].Item6.Fragment(CommonParameters.DissociationType, FragmentationTerminus.Both).ToList();
                }

                List<double> fragmentMasses = fragments.Select(m => m.NeutralMass).ToList();

                foreach (double theoreticalFragmentMass in fragmentMasses)
                {
                    double tfm = theoreticalFragmentMass;

                    if (tfm < MaxFragmentSize && tfm > 0)
                    {
                        int fragmentBin = (int)Math.Round(tfm * FragmentBinsPerDalton);

                        if (fragmentIndex[fragmentBin] == null)
                        {
                            fragmentIndex[fragmentBin] = new List<int> { peptideId };
                        }
                        else
                        {
                            fragmentIndex[fragmentBin].Add(peptideId);
                        }
                    }
                }

                //For childFragmentIndex
                List<Product> childfragments = new List<Product>();
                if (allPossiblePeptidesIsGlyco[peptideId].Item1)
                {
                    childfragments = GlycoPeptides.OGlyGetTheoreticalFragments(CommonParameters.ChildScanDissociationType, allPossiblePeptidesIsGlyco[peptideId].Item5, allPossiblePeptidesIsGlyco[peptideId].Item6);
                }
                else
                {
                    childfragments = allPossiblePeptidesIsGlyco[peptideId].Item6.Fragment(CommonParameters.ChildScanDissociationType, FragmentationTerminus.Both).ToList();
                }

                List<double> childfragmentMasses = childfragments.Select(m => m.NeutralMass).ToList();

                foreach (double theoreticalFragmentMass in childfragmentMasses)
                {
                    double tfm = theoreticalFragmentMass;

                    if (tfm < MaxFragmentSize && tfm > 0)
                    {
                        int fragmentBin = (int)Math.Round(tfm * FragmentBinsPerDalton);

                        if (secondFragmentIndex[fragmentBin] == null)
                        {
                            secondFragmentIndex[fragmentBin] = new List<int> { peptideId };
                        }
                        else
                        {
                            secondFragmentIndex[fragmentBin].Add(peptideId);
                        }
                    }
                }
            }
        }
    }
}
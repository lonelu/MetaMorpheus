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
        private readonly int TopN;
        private readonly int _maxOGlycanNum;
        private bool OxiniumIonFilt;
        private readonly string _glycanDatabase;

        private readonly Tolerance PrecusorSearchMode;
        private readonly MassDiffAcceptor ProductSearchMode;

        private readonly List<int>[] SecondFragmentIndex;

        public GlycoSearchEngine(List<GlycoSpectralMatch>[] globalCsms, Ms2ScanWithSpecificMass[] listOfSortedms2Scans, List<PeptideWithSetModifications> peptideIndex,
            List<int>[] fragmentIndex, List<int>[] secondFragmentIndex, int currentPartition, CommonParameters commonParameters, List<(string fileName, CommonParameters fileSpecificParameters)> fileSpecificParameters,
             string glycanDatabase, bool isOGlycoSearch, int glycoSearchTopNum, int maxOGlycanNum, bool oxiniumIonFilt, List<string> nestedIds)
            : base(null, listOfSortedms2Scans, peptideIndex, fragmentIndex, currentPartition, commonParameters, fileSpecificParameters, new OpenSearchMode(), 0, nestedIds)
        {
            this.GlobalCsms = globalCsms;
            this.IsOGlycoSearch = isOGlycoSearch;
            this.TopN = glycoSearchTopNum;
            this._maxOGlycanNum = maxOGlycanNum;
            this.OxiniumIonFilt = oxiniumIonFilt;
            this._glycanDatabase = glycanDatabase;

            SecondFragmentIndex = secondFragmentIndex;
            PrecusorSearchMode = commonParameters.PrecursorMassTolerance;
            ProductSearchMode = new SinglePpmAroundZeroSearchMode(20); //For Oxinium ion only


            if (!isOGlycoSearch)
            {
                Glycans = GlycanDatabase.LoadGlycan(GlobalVariables.GlycanLocations.Where(p=> System.IO.Path.GetFileName(p)==_glycanDatabase).First(), !isOGlycoSearch).OrderBy(p => p.Mass).ToArray();
                //TO THINK: Glycan Decoy database.
                //DecoyGlycans = Glycan.BuildTargetDecoyGlycans(NGlycans);

            }
            else
            {
                GlycanBox.GlobalOGlycans = GlycanDatabase.LoadGlycan(GlobalVariables.GlycanLocations.Where(p => System.IO.Path.GetFileName(p) == _glycanDatabase).First(), !isOGlycoSearch).ToArray();
                GlycanBox.GlobalOGlycanModifications = GlycanBox.BuildGlobalOGlycanModifications(GlycanBox.GlobalOGlycans);
                GlycanBox.OGlycanBoxes = GlycanBox.BuildOGlycanBoxes(_maxOGlycanNum).OrderBy(p => p.Mass).ToArray();

                //This is for Hydroxyproline 
                SelectedModBox.SelectedModifications = new Modification[5];
                SelectedModBox.SelectedModifications[0] = GlobalVariables.AllModsKnownDictionary["Hydroxylation on P"];
                SelectedModBox.SelectedModifications[1] = GlobalVariables.AllModsKnownDictionary["Hydroxylation on K"];
                SelectedModBox.SelectedModifications[2] = GlobalVariables.AllModsKnownDictionary["Glucosylgalactosyl on K"];
                SelectedModBox.SelectedModifications[3] = GlobalVariables.AllModsKnownDictionary["Galactosyl on K"];
                SelectedModBox.SelectedModifications[4] = GlobalVariables.AllModsKnownDictionary["Oxidation on M"];
                SelectedModBox.ModBoxes = SelectedModBox.BuildModBoxes(_maxOGlycanNum).Where(p => !p.MotifNeeded.ContainsKey("K") || (p.MotifNeeded.ContainsKey("K") && p.MotifNeeded["K"].Count <= 3)).OrderBy(p => p.Mass).ToArray();

            }
        }

        private Glycan[] Glycans { get; }
        //private Glycan[] DecoyGlycans { get; }

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

                List<int> idsOfPeptidesTopN = new List<int>();
                byte scoreAtTopN = 0;
                int peptideCount = 0;

                for (; scanIndex < ListOfSortedMs2Scans.Length; scanIndex += maxThreadsPerFile)
                {
                    // Stop loop if canceled
                    if (GlobalVariables.StopLoops) { return; }

                    // empty the scoring table to score the new scan (conserves memory compared to allocating a new array)
                    Array.Clear(scoringTable, 0, scoringTable.Length);
                    idsOfPeptidesPossiblyObserved.Clear();
                    idsOfPeptidesTopN.Clear();

                    var scan = ListOfSortedMs2Scans[scanIndex];

                    // get fragment bins for this scan
                    List<int> allBinsToSearch = GetBinsToSearch(scan, FragmentIndex, CommonParameters.DissociationType);
                    List<int> childBinsToSearch = null;

                    //TO DO: limit the high bound limitation

                    // first-pass scoring
                    IndexedScoring(FragmentIndex, allBinsToSearch, scoringTable, byteScoreCutoff, idsOfPeptidesPossiblyObserved, scan.PrecursorMass, Double.NegativeInfinity, Double.PositiveInfinity, PeptideIndex, MassDiffAcceptor, 0, CommonParameters.DissociationType);

                    //child scan first-pass scoring
                    if (scan.ChildScans != null && CommonParameters.ChildScanDissociationType != DissociationType.LowCID)
                    {
                        Array.Clear(secondScoringTable, 0, secondScoringTable.Length);
                        childIdsOfPeptidesPossiblyObserved.Clear();

                        childBinsToSearch = new List<int>();

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
                        scoreAtTopN = 0;
                        peptideCount = 0;
                        foreach (int id in idsOfPeptidesPossiblyObserved.OrderByDescending(p => scoringTable[p]))
                        {
                            if (scoringTable[id] < (int)byteScoreCutoff)
                            {
                                continue;
                            }
                            peptideCount++;
                            if (peptideCount == TopN)
                            {
                                scoreAtTopN = scoringTable[id];
                            }
                            if (scoringTable[id] < scoreAtTopN)
                            {
                                break;
                            }
                            idsOfPeptidesTopN.Add(id);
                        }

                        List<GlycoSpectralMatch> gsms;
                        if (IsOGlycoSearch == false)
                        {
                            gsms = FindNGlycopeptide(scan, idsOfPeptidesTopN, scanIndex);
                        }
                        else
                        {
                            //gsms = FindOGlycopeptideHash(scan, idsOfPeptidesTopN, scanIndex, allBinsToSearch, childBinsToSearch, (int)byteScoreCutoff);
                            gsms = FindOGlycopeptideHashLocal(scan, idsOfPeptidesTopN, scanIndex, allBinsToSearch, childBinsToSearch, (int)byteScoreCutoff);
                            //gsms = FindModPepHash(scan, idsOfPeptidesTopN, scanIndex, allBinsToSearch, (int)byteScoreCutoff);
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

                        GlobalCsms[scanIndex].AddRange(gsms.Where(p => p != null).OrderByDescending(p => p.Score));
                    }

                    // report search progress
                    progress++;
                    var percentProgress = (int)((progress / ListOfSortedMs2Scans.Length) * 100);

                    if (percentProgress > oldPercentProgress)
                    {
                        oldPercentProgress = percentProgress;
                        ReportProgress(new ProgressEventArgs(percentProgress, "Performing glyco search... " + CurrentPartition + "/" + CommonParameters.TotalPartitions, NestedIds));
                    }
                }
            });

            return new MetaMorpheusEngineResults(this);
        }

        private List<GlycoSpectralMatch> FindNGlycopeptide(Ms2ScanWithSpecificMass theScan, List<int> idsOfPeptidesPossiblyObserved, int scanIndex)
        {
            List<GlycoSpectralMatch> possibleMatches = new List<GlycoSpectralMatch>();

            //if (theScan.OxiniumIonNum < 2)
            //{
            //    return possibleMatches;
            //}

            for (int ind = 0; ind < idsOfPeptidesPossiblyObserved.Count; ind++)
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

                var theScanBestPeptide = PeptideIndex[idsOfPeptidesPossiblyObserved[ind]];

                //if (theScan.OxiniumIonNum < 2)
                //{
                //    continue;
                //}

                List<int> modPos = GlycoSpectralMatch.GetPossibleModSites(theScanBestPeptide, new string[] { "Nxt", "Nxs" });
                if (modPos.Count < 1)
                {
                    continue;
                }

                var possibleGlycanMassLow = theScan.PrecursorMass * (1 - 1E-5) - theScanBestPeptide.MonoisotopicMass;
                if (possibleGlycanMassLow < 200 || possibleGlycanMassLow > Glycans.Last().Mass)
                {
                    continue;
                }


                int iDLow = GlycoPeptides.BinarySearchGetIndex(Glycans.Select(p => (double)p.Mass / 1E5).ToArray(), possibleGlycanMassLow);

                while (iDLow < Glycans.Count() && PrecusorSearchMode.Within(theScan.PrecursorMass, theScanBestPeptide.MonoisotopicMass + (double)Glycans[iDLow].Mass / 1E5))
                {
                    double bestLocalizedScore = 0;
                    int bestSite = 0;
                    List<MatchedFragmentIon> bestMatchedIons = new List<MatchedFragmentIon>();
                    PeptideWithSetModifications peptideWithSetModifications = theScanBestPeptide;
                    foreach (int possibleSite in modPos)
                    {
                        var testPeptide = GlycoPeptides.GenerateGlycopeptide(possibleSite, theScanBestPeptide, Glycans[iDLow]);

                        List<Product> theoreticalProducts = new List<Product>();
                        testPeptide.Fragment(CommonParameters.DissociationType, FragmentationTerminus.Both, theoreticalProducts);
                        theoreticalProducts = theoreticalProducts.Where(p => p.ProductType != ProductType.M).ToList();
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

                    var psmCross = new GlycoSpectralMatch(peptideWithSetModifications, 0, bestLocalizedScore, scanIndex, theScan, CommonParameters, bestMatchedIons);
                    psmCross.NGlycan = new List<Glycan> { Glycans[iDLow] };
                    psmCross.GlycanScore = CalculatePeptideScore(theScan.TheScan, bestMatchedIons.Where(p => p.Annotation.Contains('M')).ToList());
                    psmCross.DiagnosticIonScore = CalculatePeptideScore(theScan.TheScan, bestMatchedIons.Where(p => p.Annotation.First() == 'D').ToList());
                    psmCross.PeptideScore = psmCross.Score - psmCross.GlycanScore - psmCross.DiagnosticIonScore;
                    psmCross.Rank = ind;
                    psmCross.NGlycanLocalizations = new List<int> { bestSite - 1 }; //TO DO: ambiguity modification site
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

        private List<GlycoSpectralMatch> FindOGlycopeptideHash(Ms2ScanWithSpecificMass theScan, List<int> idsOfPeptidesPossiblyObserved, int scanIndex, List<int> allBinsToSearch, List<int> childBinsToSearch, int scoreCutOff)
        {
            List<GlycoSpectralMatch> possibleMatches = new List<GlycoSpectralMatch>();

            for (int ind = 0; ind < idsOfPeptidesPossiblyObserved.Count; ind++)
            {
                var theScanBestPeptide = PeptideIndex[idsOfPeptidesPossiblyObserved[ind]];

                if (PrecusorSearchMode.Within(theScan.PrecursorMass, theScanBestPeptide.MonoisotopicMass))
                {
                    List<Product> products = new List<Product>();
                    theScanBestPeptide.Fragment(CommonParameters.DissociationType, FragmentationTerminus.Both, products);
                    var matchedFragmentIons = MatchFragmentIons(theScan, products, CommonParameters);
                    double score = CalculatePeptideScore(theScan.TheScan, matchedFragmentIons);

                    var psmCrossSingle = new GlycoSpectralMatch(theScanBestPeptide, 0, score, scanIndex, theScan, CommonParameters, matchedFragmentIons);
                    psmCrossSingle.Rank = ind;
                    psmCrossSingle.ResolveAllAmbiguities();

                    possibleMatches.Add(psmCrossSingle);
                }
                else if (theScan.PrecursorMass - theScanBestPeptide.MonoisotopicMass >= 100)
                {
                    //Filter by glycanBoxes mass difference.
                    var possibleGlycanMassLow = theScan.PrecursorMass * (1 - 1E-5) - theScanBestPeptide.MonoisotopicMass;

                    if (possibleGlycanMassLow < GlycanBox.OGlycanBoxes.First().Mass || possibleGlycanMassLow > GlycanBox.OGlycanBoxes.Last().Mass)
                    {
                        continue;
                    }

                    //Filter by OxoniumIon
                    var oxoniumIonIntensities = GlycoPeptides.ScanOxoniumIonFilter(theScan, ProductSearchMode, CommonParameters.DissociationType);

                    //The oxoniumIonIntensities is related with Glycan.AllOxoniumIons (the [9] is 204). A spectrum needs to have 204.0867 to be considered as a glycopeptide for now.
                    if (OxiniumIonFilt && oxoniumIonIntensities[9] == 0)
                    {
                        continue;
                    }

                    int[] modPos = GlycoSpectralMatch.GetPossibleModSites(theScanBestPeptide, new string[] { "S", "T" }).ToArray();

                    List<Tuple<int, int[]>> glycanBoxId_localization = new List<Tuple<int, int[]>>();

                    int iDLow = GlycoPeptides.BinarySearchGetIndex(GlycanBox.OGlycanBoxes.Select(p => p.Mass).ToArray(), possibleGlycanMassLow);

                    while (iDLow < GlycanBox.OGlycanBoxes.Count() && (PrecusorSearchMode.Within(theScan.PrecursorMass, theScanBestPeptide.MonoisotopicMass + GlycanBox.OGlycanBoxes[iDLow].Mass)))
                    {

                        if (modPos.Length >= GlycanBox.OGlycanBoxes[iDLow].NumberOfMods && GlycoPeptides.OxoniumIonsAnalysis(oxoniumIonIntensities, GlycanBox.OGlycanBoxes[iDLow]))
                        {
                            var permutateModPositions = GlycoPeptides.GetPermutations(modPos.ToList(), GlycanBox.OGlycanBoxes[iDLow].ModIds);

                            foreach (var theModPositions in permutateModPositions)
                            {
                                glycanBoxId_localization.Add(new Tuple<int, int[]>(iDLow, theModPositions));
                            }
                        }

                        iDLow++;
                    }

                    //TO DO: Consider the situation that there is no child Scan. Consider if the father scan EThcD Scan.
                    if (glycanBoxId_localization.Count > 0 && theScan.ChildScans.Count > 0)
                    {
                        HashSet<int> allPeaksForLocalization = new HashSet<int>(childBinsToSearch);

                        List<Product> products = new List<Product>();
                        theScanBestPeptide.Fragment(DissociationType.ETD, FragmentationTerminus.Both, products);

                        List<Tuple<int, Tuple<int, int, double>[]>> localizationCandidates = new List<Tuple<int, Tuple<int, int, double>[]>>();
                        int BestLocalizaionScore = 0;

                        for (int i = 0; i < glycanBoxId_localization.Count; i++)
                        {
                            var fragmentHash = GlycoPeptides.GetFragmentHash(products, glycanBoxId_localization[i], GlycanBox.OGlycanBoxes, FragmentBinsPerDalton);
                            int currentLocalizationScore = allPeaksForLocalization.Intersect(fragmentHash).Count();

                            Tuple<int, int, double>[] tuples = new Tuple<int, int, double>[glycanBoxId_localization[i].Item2.Length];
                            for (int j = 0; j < glycanBoxId_localization[i].Item2.Length; j++)
                            {
                                tuples[j] = new Tuple<int, int, double>(glycanBoxId_localization[i].Item2[j], GlycanBox.OGlycanBoxes[glycanBoxId_localization[i].Item1].ModIds[j], 0);
                            }

                            if (currentLocalizationScore > BestLocalizaionScore)
                            {
                                localizationCandidates.Clear();
                                localizationCandidates.Add(new Tuple<int, Tuple<int, int, double>[]>(glycanBoxId_localization[i].Item1, tuples));
                            }
                            else if (BestLocalizaionScore > 0 && currentLocalizationScore == BestLocalizaionScore)
                            {
                                localizationCandidates.Add(new Tuple<int, Tuple<int, int, double>[]>(glycanBoxId_localization[i].Item1, tuples));
                            }
                        }

                        if (localizationCandidates.Count > 0)
                        {
                            var psmGlyco = CreateGsm(theScan, scanIndex, ind, theScanBestPeptide, localizationCandidates.First().Item2, CommonParameters, oxoniumIonIntensities, null, localizationCandidates);

                            possibleMatches.Add(psmGlyco);
                        }
                    }
                }

                if (possibleMatches.Count != 0)
                {
                    possibleMatches = possibleMatches.OrderByDescending(p => p.Score).ToList();
                }
            }

            return possibleMatches;
        }

        private List<GlycoSpectralMatch> FindOGlycopeptideHashLocal(Ms2ScanWithSpecificMass theScan, List<int> idsOfPeptidesPossiblyObserved, int scanIndex, List<int> allBinsToSearch, List<int> childBinsToSearch, int scoreCutOff)
        {
            List<GlycoSpectralMatch> possibleMatches = new List<GlycoSpectralMatch>();

            for (int ind = 0; ind < idsOfPeptidesPossiblyObserved.Count; ind++)
            {
                var theScanBestPeptide = PeptideIndex[idsOfPeptidesPossiblyObserved[ind]];

                if (PrecusorSearchMode.Within(theScan.PrecursorMass, theScanBestPeptide.MonoisotopicMass))
                {
                    List<Product> products = new List<Product>(); 
                    theScanBestPeptide.Fragment(CommonParameters.DissociationType, FragmentationTerminus.Both, products);
                    var matchedFragmentIons = MatchFragmentIons(theScan, products, CommonParameters);
                    double score = CalculatePeptideScore(theScan.TheScan, matchedFragmentIons);

                    var psmCrossSingle = new GlycoSpectralMatch(theScanBestPeptide, 0, score, scanIndex, theScan, CommonParameters, matchedFragmentIons);
                    psmCrossSingle.Rank = ind;

                    possibleMatches.Add(psmCrossSingle);
                }
                else if (theScan.PrecursorMass - theScanBestPeptide.MonoisotopicMass >= 100) //Filter out unknow non-glycan modifications.
                {
                    //Filter by glycanBoxes mass difference.
                    var possibleGlycanMassLow = PrecusorSearchMode.GetMinimumValue(theScan.PrecursorMass) - theScanBestPeptide.MonoisotopicMass;

                    var possibleGlycanMassHigh = PrecusorSearchMode.GetMaximumValue(theScan.PrecursorMass) - theScanBestPeptide.MonoisotopicMass;

                    if (possibleGlycanMassHigh < GlycanBox.OGlycanBoxes.First().Mass || possibleGlycanMassLow > GlycanBox.OGlycanBoxes.Last().Mass)
                    {
                        continue;
                    }

                    //Filter by OxoniumIon
                    var oxoniumIonIntensities = GlycoPeptides.ScanOxoniumIonFilter(theScan, ProductSearchMode, CommonParameters.DissociationType);

                    //The oxoniumIonIntensities is related with Glycan.AllOxoniumIons (the [9] is 204). A spectrum needs to have 204.0867 to be considered as a glycopeptide for now.
                    if (OxiniumIonFilt && oxoniumIonIntensities[9] == 0)
                    {
                        continue;
                    }


                    int iDLow = GlycoPeptides.BinarySearchGetIndex(GlycanBox.OGlycanBoxes.Select(p => p.Mass).ToArray(), possibleGlycanMassLow);

                    int[] modPos = GlycoSpectralMatch.GetPossibleModSites(theScanBestPeptide, new string[] { "S", "T" }).ToArray();

                    //Localization for O-glycopeptides only works on ETD related dissociationtype
                    //No localization can be done with MS2-HCD spectrum
                    if ((theScan.ChildScans==null || !GlycoPeptides.DissociationTypeContainETD(CommonParameters.ChildScanDissociationType)) && !GlycoPeptides.DissociationTypeContainETD(CommonParameters.DissociationType))
                    {
                        while(iDLow < GlycanBox.OGlycanBoxes.Count() && (PrecusorSearchMode.Within(theScan.PrecursorMass, theScanBestPeptide.MonoisotopicMass + GlycanBox.OGlycanBoxes[iDLow].Mass)))
                        {
                            if (modPos.Length >= GlycanBox.OGlycanBoxes[iDLow].NumberOfMods && GlycoPeptides.OxoniumIonsAnalysis(oxoniumIonIntensities, GlycanBox.OGlycanBoxes[iDLow]))
                            {
                                List<Product> hcdProducts = new List<Product>();
                                theScanBestPeptide.Fragment(CommonParameters.DissociationType, FragmentationTerminus.Both, hcdProducts);
                                var hcdMatchedFragmentIons = MatchFragmentIons(theScan, hcdProducts, CommonParameters);
                                double hcdScore = CalculatePeptideScore(theScan.TheScan, hcdMatchedFragmentIons);

                                var psmCrossSingle = new GlycoSpectralMatch(theScanBestPeptide, 0, hcdScore, scanIndex, theScan, CommonParameters, hcdMatchedFragmentIons);
                                psmCrossSingle.Rank = ind;

                                possibleMatches.Add(psmCrossSingle);
                                break;
                            }

                            iDLow++;
                            
                        }

                        continue;
                    }

                    HashSet<int> allPeaksForLocalization = new HashSet<int>();


                    if (GlycoPeptides.DissociationTypeContainETD(CommonParameters.ChildScanDissociationType))
                    {
                        allPeaksForLocalization.UnionWith(childBinsToSearch);
                    }

                    //The workflow is designed for MS2:HCD-MS2:EThcD type of data, but could also work with MS2:EThcD type of data.
                    if (GlycoPeptides.DissociationTypeContainETD(CommonParameters.DissociationType))
                    {
                        allPeaksForLocalization.UnionWith(allBinsToSearch);
                    }

                    List<Product> products = new List<Product>();
                    theScanBestPeptide.Fragment(DissociationType.ETD, FragmentationTerminus.Both, products);                    

                    double bestLocalizedScore = 0;

                    List<LocalizationGraph> localizationGraphs = new List<LocalizationGraph>();

                    while (iDLow < GlycanBox.OGlycanBoxes.Count() && (PrecusorSearchMode.Within(theScan.PrecursorMass, theScanBestPeptide.MonoisotopicMass + GlycanBox.OGlycanBoxes[iDLow].Mass)))
                    {
                        if (modPos.Length >= GlycanBox.OGlycanBoxes[iDLow].NumberOfMods && GlycoPeptides.OxoniumIonsAnalysis(oxoniumIonIntensities, GlycanBox.OGlycanBoxes[iDLow]))
                        {
                            var boxes = GlycanBox.BuildChildOGlycanBoxes(GlycanBox.OGlycanBoxes[iDLow].NumberOfMods, GlycanBox.OGlycanBoxes[iDLow].ModIds).ToArray();
                            LocalizationGraph localizationGraph = new LocalizationGraph(modPos, GlycanBox.OGlycanBoxes[iDLow], boxes, iDLow);
                            LocalizationGraph.LocalizeOGlycan(localizationGraph, allPeaksForLocalization, products);

                            double currentLocalizationScore = localizationGraph.TotalScore;
                            if (currentLocalizationScore > bestLocalizedScore)
                            {
           
                                localizationGraphs.Clear();
                                localizationGraphs.Add(localizationGraph);
                            }
                            else if (bestLocalizedScore > 0 && currentLocalizationScore == bestLocalizedScore)
                            {
                                localizationGraphs.Add(localizationGraph);
                            }
                        }

                        iDLow++;
                    }

                    //In theory, the peptide_localization shouldn't be null, but it is possible that the real score is smaller than indexed score.
                    if (localizationGraphs.Count > 0)
                    {
                        var firstPath = LocalizationGraph.GetFirstPath(localizationGraphs[0].array, localizationGraphs[0].ChildModBoxes);
                        var localizationCandidate = LocalizationGraph.GetLocalizedPath(localizationGraphs[0].array, modPos, localizationGraphs[0].ChildModBoxes, firstPath);

                        var psmGlyco = CreateGsm(theScan, scanIndex, ind, theScanBestPeptide, localizationCandidate, CommonParameters, oxoniumIonIntensities, localizationGraphs);

                        possibleMatches.Add(psmGlyco);
                    }
                }

                if (possibleMatches.Count != 0)
                {
                    possibleMatches = possibleMatches.OrderByDescending(p => p.Score).ToList();
                }
            }

            return possibleMatches;
        }

        //List<Tuple<int, Tuple<int, int>[]>> <glycanBoxId, <mod site, glycan id>>
        private GlycoSpectralMatch CreateGsm(Ms2ScanWithSpecificMass theScan, int scanIndex, int rank, PeptideWithSetModifications peptide, Tuple<int, int, double>[] localization, CommonParameters commonParameters, double[] oxoniumIonIntensities, List<LocalizationGraph> localizationGraphs = null, List<Tuple<int, Tuple<int, int, double>[]>> glycanBox_localizations = null)
        {
            var peptideWithMod = GlycoPeptides.OGlyGetTheoreticalPeptide(localization, peptide);

            var fragmentsForEachGlycoPeptide = GlycoPeptides.OGlyGetTheoreticalFragments(CommonParameters.DissociationType, peptide, peptideWithMod);

            var matchedIons = MatchFragmentIons(theScan, fragmentsForEachGlycoPeptide, CommonParameters);

            double score = CalculatePeptideScore(theScan.TheScan, matchedIons);

            var DiagnosticIonScore = CalculatePeptideScore(theScan.TheScan, matchedIons.Where(p => p.Annotation.First() =='D').ToList());

            var PeptideScore = score - DiagnosticIonScore;

            var p = theScan.TheScan.MassSpectrum.Size * CommonParameters.ProductMassTolerance.GetRange(1000).Width / theScan.TheScan.MassSpectrum.Range.Width;

            int n = fragmentsForEachGlycoPeptide.Where(p => p.ProductType == ProductType.c || p.ProductType == ProductType.zDot).Count();

            var allMatchedChildIons = new Dictionary<int, List<MatchedFragmentIon>>();

            foreach (var childScan in theScan.ChildScans)
            {
                var childFragments = GlycoPeptides.OGlyGetTheoreticalFragments(CommonParameters.ChildScanDissociationType, peptide, peptideWithMod);

                var matchedChildIons = MatchFragmentIons(childScan, childFragments, CommonParameters);

                n += childFragments.Where(p => p.ProductType == ProductType.c || p.ProductType == ProductType.zDot).Count();

                if (matchedChildIons == null)
                {
                    continue;
                }

                allMatchedChildIons.Add(childScan.OneBasedScanNumber, matchedChildIons);
                double childScore = CalculatePeptideScore(childScan.TheScan, matchedChildIons);

                double childDiagnosticIonScore = CalculatePeptideScore(childScan.TheScan, matchedChildIons.Where(p => p.Annotation.First() == 'D').ToList());

                DiagnosticIonScore += childDiagnosticIonScore;

                PeptideScore += childScore - childDiagnosticIonScore;
                //TO THINK:may think a different way to use childScore
                score += childScore;

                p += childScan.TheScan.MassSpectrum.Size * CommonParameters.ProductMassTolerance.GetRange(1000).Width / childScan.TheScan.MassSpectrum.Range.Width;

            }

            var psmGlyco = new GlycoSpectralMatch(peptideWithMod, 0, PeptideScore, scanIndex, theScan, CommonParameters, matchedIons);
            
            //TO DO: This p is from childScan p, it works for HCD-pd-EThcD, which may not work for other type.
            psmGlyco.ScanInfo_p = p;

            psmGlyco.Thero_n = n;

            psmGlyco.Rank = rank;

            psmGlyco.DiagnosticIonScore = DiagnosticIonScore;

            psmGlyco.ChildMatchedFragmentIons = allMatchedChildIons;

            psmGlyco.LocalizationGraphs = localizationGraphs;

            psmGlyco.OGlycanBoxLocalization = glycanBox_localizations;

            if (oxoniumIonIntensities[5] == 0)
            {
                psmGlyco.R138vs144 = double.PositiveInfinity;
            }
            else
            {
                psmGlyco.R138vs144 = oxoniumIonIntensities[4] / oxoniumIonIntensities[5];
            }     

            return psmGlyco;
        }

        private List<GlycoSpectralMatch> FindModPepHash(Ms2ScanWithSpecificMass theScan, List<int> idsOfPeptidesPossiblyObserved, int scanIndex, List<int> allBinsToSearch, int scoreCutOff)
        {
            List<GlycoSpectralMatch> possibleMatches = new List<GlycoSpectralMatch>();

            for (int ind = 0; ind < idsOfPeptidesPossiblyObserved.Count; ind++)
            {
                var theScanBestPeptide = PeptideIndex[idsOfPeptidesPossiblyObserved[ind]];

                if (PrecusorSearchMode.Within(theScan.PrecursorMass, theScanBestPeptide.MonoisotopicMass))
                {
                    List<Product> products = new List<Product>();
                    theScanBestPeptide.Fragment(CommonParameters.DissociationType, FragmentationTerminus.Both, products);
                    var matchedFragmentIons = MatchFragmentIons(theScan, products, CommonParameters);
                    double score = CalculatePeptideScore(theScan.TheScan, matchedFragmentIons);

                    var psmCrossSingle = new GlycoSpectralMatch(theScanBestPeptide, 0, score, scanIndex, theScan, CommonParameters, matchedFragmentIons);
                    psmCrossSingle.Rank = ind;
                    psmCrossSingle.ResolveAllAmbiguities();

                    possibleMatches.Add(psmCrossSingle);
                }
                else if (theScan.PrecursorMass - theScanBestPeptide.MonoisotopicMass >= 10)
                {
                    //filter by glycanBoxes masses
                    var possibleModMassLow = theScan.PrecursorMass * (1 - 1E-5) - theScanBestPeptide.MonoisotopicMass;
                    if (possibleModMassLow < SelectedModBox.ModBoxes.First().Mass || possibleModMassLow > SelectedModBox.ModBoxes.Last().Mass)
                    {
                        continue;
                    }

                    List<Product> products = new List<Product>();
                    theScanBestPeptide.Fragment(CommonParameters.DissociationType, FragmentationTerminus.Both, products);
                    HashSet<int> allPeaksForLocalization = new HashSet<int>(allBinsToSearch);

                    double bestLocalizedScore = 1;

                    List<LocalizationGraph> localizationGraphs = new List<LocalizationGraph>();
                    List<int> ids = new List<int>();

                    int iDLow = GlycoPeptides.BinarySearchGetIndex(SelectedModBox.ModBoxes.Select(p => p.Mass).ToArray(), possibleModMassLow);

                    while (iDLow < SelectedModBox.ModBoxes.Count() && PrecusorSearchMode.Within(theScan.PrecursorMass, theScanBestPeptide.MonoisotopicMass + SelectedModBox.ModBoxes[iDLow].Mass))
                    {
                        int[] modPos = SelectedModBox.GetAllPossibleModSites(theScanBestPeptide, SelectedModBox.ModBoxes[iDLow]);

                        if (modPos==null)
                        {
                            iDLow++;
                            continue;
                        }

                        var boxes = SelectedModBox.BuildChildModBoxes(SelectedModBox.ModBoxes[iDLow].NumberOfMods, SelectedModBox.ModBoxes[iDLow].ModIds).ToArray();

                        LocalizationGraph localizationGraph = new LocalizationGraph(modPos, SelectedModBox.ModBoxes[iDLow], boxes);
                        localizationGraph.LocalizeMod(modPos, SelectedModBox.ModBoxes[iDLow], boxes, allPeaksForLocalization, products, theScanBestPeptide);

                        double currentLocalizationScore = localizationGraph.array.Last().Last().maxCost;
                        if (currentLocalizationScore > bestLocalizedScore)
                        {
                            ids.Clear();
                            localizationGraphs.Clear();
                            ids.Add(iDLow);
                            localizationGraphs.Add(localizationGraph);
                        }
                        else if (bestLocalizedScore > 0 && currentLocalizationScore == bestLocalizedScore)
                        {
                            ids.Add(iDLow);
                            localizationGraphs.Add(localizationGraph);        
                        }

                        iDLow++;
                    }

                    //In theory, the peptide_localization shouldn't be null, but it is possible that the real score is smaller than indexed score.
                    if (localizationGraphs.Count > 0)
                    {
                        List<Tuple<int, Tuple<int, int, double>[]>> localizationCandidates = new List<Tuple<int, Tuple<int, int, double>[]>>();
                        for (int i = 0; i < localizationGraphs.Count; i++)
                        {
                            int[] modPos = SelectedModBox.GetAllPossibleModSites(theScanBestPeptide, SelectedModBox.ModBoxes[ids[i]]);
                            var boxes = SelectedModBox.BuildChildModBoxes(SelectedModBox.ModBoxes[ids[i]].NumberOfMods, SelectedModBox.ModBoxes[ids[i]].ModIds).ToArray();
                            var allPaths = LocalizationGraph.GetAllPaths(localizationGraphs[i].array, boxes);
                            var local = LocalizationGraph.GetLocalizedPath(localizationGraphs[i].array, modPos, boxes, allPaths.First());
                            localizationCandidates.Add(new Tuple<int, Tuple<int, int, double>[]>(iDLow, local));
                        }

                        var psmGlyco = CreateGsmMod(theScan, scanIndex, ind, theScanBestPeptide, localizationCandidates, CommonParameters);

                        possibleMatches.Add(psmGlyco);
                    }
                }
            }

            if (possibleMatches.Count != 0)
            {
                possibleMatches = possibleMatches.OrderByDescending(p => p.Score).ToList();
            }

            return possibleMatches;
        }

        private GlycoSpectralMatch CreateGsmMod(Ms2ScanWithSpecificMass theScan, int scanIndex, int rank, PeptideWithSetModifications peptide, List<Tuple<int, Tuple<int, int, double>[]>> localizationCandidates, CommonParameters commonParameters)
        {
            var peptideWithMod = SelectedModBox.GetTheoreticalPeptide(localizationCandidates.First().Item2, peptide, SelectedModBox.ModBoxes[localizationCandidates.First().Item1]);

            List<Product> fragmentsForEachGlycoPeptide = new List<Product>();
            peptideWithMod.Fragment(CommonParameters.DissociationType, FragmentationTerminus.Both, fragmentsForEachGlycoPeptide);

            var matchedIons = MatchFragmentIons(theScan, fragmentsForEachGlycoPeptide, CommonParameters);

            double score = CalculatePeptideScore(theScan.TheScan, matchedIons);

            var psmGlyco = new GlycoSpectralMatch(peptideWithMod, 0, score, scanIndex, theScan, CommonParameters, matchedIons);

            psmGlyco.Rank = rank;
            //TO DO
            //psmGlyco.localizations = glycanBox_localization.First().Value;

            return psmGlyco;
        }

    }
}
﻿using MassSpectrometry;
using Proteomics;
using Proteomics.Fragmentation;
using Proteomics.ProteolyticDigestion;
using System;
using System.Collections.Generic;
using System.Linq;
using Chemistry;
using System.Threading;
using System.Runtime.InteropServices.ComTypes;

namespace EngineLayer.GlycoSearch
{
    public static class GlycoPeptides
    {
        public static double[] ScanOxoniumIonFilter(Ms2ScanWithSpecificMass theScan, MassDiffAcceptor massDiffAcceptor, DissociationType dissociationType)
        {
            double[] oxoniumIonsintensities = new double[Glycan.AllOxoniumIons.Length];

            if (dissociationType != DissociationType.HCD && dissociationType != DissociationType.CID && dissociationType != DissociationType.EThcD)
            {
                return oxoniumIonsintensities;
            }

            for (int i = 0; i < Glycan.AllOxoniumIons.Length; i++)
            {
                var oxoMass = ((double)Glycan.AllOxoniumIons[i] / 1E5).ToMass(1);
                var envelope = theScan.GetClosestExperimentalIsotopicEnvelope(oxoMass);
                if (massDiffAcceptor.Accepts(envelope.MonoisotopicMass, oxoMass) >= 0)
                {
                    oxoniumIonsintensities[i] = envelope.TotalIntensity;
                }

            }

            //Normalize by 204. What will happen if 204 is 0.
            if (oxoniumIonsintensities[9] != 0)
            {
                var x204 = oxoniumIonsintensities[9];
                for (int i = 0; i < Glycan.AllOxoniumIons.Length; i++)
                {

                    oxoniumIonsintensities[i] = oxoniumIonsintensities[i] / x204;
                }
            }

            return oxoniumIonsintensities;
        }

        public static Product GetIndicatorYIon(double peptideMonomassWithNoGlycan, string glycanString)
        {
            Product product = new Product(ProductType.M, FragmentationTerminus.Both, peptideMonomassWithNoGlycan + (double)Glycan.GetMass(glycanString) / 1E5, 0, 0, 0);
            return product;
        }

        public static bool MatchIndicatorYIon(Ms2ScanWithSpecificMass scan, Product theoreticalProduct, CommonParameters commonParameters)
        {
            List<Product> products = new List<Product>();
            products.Add(theoreticalProduct);
            var x = MetaMorpheusEngine.MatchFragmentIons(scan, products, commonParameters).Count();

            foreach (var childScan in scan.ChildScans)
            {
                x += MetaMorpheusEngine.MatchFragmentIons(childScan, products, commonParameters).Count();
            }

            return x > 0;
        }

        //NGlycopeptide usually contain Y ions with different charge states, especially in sceHCD data. 
        //The purpose of this function is to try match all Y ion with different charges. The usage of this function requires further investigation. 
        //Not sure about OGlycopeptide. 
        public static List<MatchedFragmentIon> GlyMatchOriginFragmentIons(Ms2ScanWithSpecificMass scan, List<Product> theoreticalProducts, CommonParameters commonParameters)
        {
            var matchedFragmentIons = new List<MatchedFragmentIon>();

            // if the spectrum has no peaks
            if (scan.ExperimentalFragments != null && !scan.ExperimentalFragments.Any())
            {
                return matchedFragmentIons;
            }

            // search for ions in the spectrum

            for (int id = 0; id < theoreticalProducts.Count; id++)
            {
                var product = theoreticalProducts[id];
                // unknown fragment mass; this only happens rarely for sequences with unknown amino acids
                if (double.IsNaN(product.NeutralMass))
                {
                    continue;
                }

                if (product.ProductType == ProductType.M)
                {
                    for (int i = 1; i <= scan.PrecursorCharge; i++)
                    {

                        var closestExperimentalMz = scan.GetClosestExperimentalFragmentMz(product.NeutralMass.ToMz(i), out double? intensity);

                        if (closestExperimentalMz.HasValue && commonParameters.ProductMassTolerance.Within(closestExperimentalMz.Value, product.NeutralMass.ToMz(i)))
                        {
                            matchedFragmentIons.Add(new MatchedFragmentIon(ref product, closestExperimentalMz.Value, intensity.Value, i));
                        }
                    }
                }

                else
                {
                    // get the closest peak in the spectrum to the theoretical peak
                    var closestExperimentalMass = scan.GetClosestExperimentalIsotopicEnvelope(product.NeutralMass);

                    // is the mass error acceptable?
                    if (closestExperimentalMass != null && commonParameters.ProductMassTolerance.Within(closestExperimentalMass.MonoisotopicMass, product.NeutralMass) && closestExperimentalMass.Charge <= scan.PrecursorCharge)
                    {
                        matchedFragmentIons.Add(new MatchedFragmentIon(ref product, closestExperimentalMass.MonoisotopicMass.ToMz(closestExperimentalMass.Charge),
                            closestExperimentalMass.Peaks.First().intensity, closestExperimentalMass.Charge));
                    }
                }
            }

            return matchedFragmentIons;
        }

        //Find Glycan index or glycanBox index.
        public static int BinarySearchGetIndex(double[] massArray, double targetMass)
        {
            var iD = Array.BinarySearch(massArray, targetMass);
            if (iD < 0) { iD = ~iD; }
            else
            {
                while (iD - 1 >= 0 && massArray[iD - 1] >= targetMass - 0.00000001)
                {
                    iD--;
                }
            }
            return iD;
        }

        public static bool DissociationTypeContainETD(DissociationType dissociationType)
        {
            if (dissociationType == DissociationType.ETD || dissociationType == DissociationType.EThcD)
            {
                return true;
            }

            return false;
        }

        public static bool DissociationTypeContainHCD(DissociationType dissociationType)
        {
            if (dissociationType == DissociationType.HCD || dissociationType == DissociationType.CID || dissociationType == DissociationType.EThcD)
            {
                return true;
            }

            return false;
        }

        #region N-Glyco related functions

        public static Dictionary<int, double> ScanGetTrimannosylCore(List<MatchedFragmentIon> matchedFragmentIons, Glycan glycan)
        {
            Dictionary<int, double> cores = new Dictionary<int, double>();

            foreach (var fragment in matchedFragmentIons.Where(p => p.NeutralTheoreticalProduct.ProductType == ProductType.M))
            {
                if (Glycan.TrimannosylCores.ContainsKey((int)((double)glycan.Mass / 1E5 - fragment.NeutralTheoreticalProduct.NeutralLoss)))
                {
                    var pair = Glycan.TrimannosylCores.Where(p => p.Key == (int)((double)glycan.Mass / 1E5 - fragment.NeutralTheoreticalProduct.NeutralLoss)).FirstOrDefault();
                    if (!cores.ContainsKey(pair.Key))
                    {
                        cores.Add(pair.Key, pair.Value);
                    }
                }

            }
            return cores;
        }

        public static bool ScanTrimannosylCoreFilter(List<MatchedFragmentIon> matchedFragmentIons, Glycan glycan)
        {
            Dictionary<int, double> cores = ScanGetTrimannosylCore(matchedFragmentIons, glycan);
            if (cores.Count > 2)
            {
                return true;
            }
            else if (cores.Keys.Contains(83) && cores.Keys.Contains(203))
            {
                return true;
            }
            return false;
        }

        //This method and the method below apply the some function from different direction. This one maybe deleted in the future.
        //public static List<Product> GetGlycanYIons(double precursorMass, Glycan glycan)
        //{
        //    List<Product> YIons = new List<Product>();
        //    foreach (var ion in glycan.Ions)
        //    {
        //        Product product = new Product(ProductType.M, FragmentationTerminus.Both, precursorMass - (double)ion.LossIonMass / 1E5, 0, 0, (double)ion.LossIonMass/1E5);
        //        YIons.Add(product);
        //    }
        //    return YIons;
        //}

        public static List<Product> GetGlycanYIons(PeptideWithSetModifications peptide, Glycan glycan)
        {
            double possiblePeptideMass = peptide.MonoisotopicMass;
            List<Product> YIons = new List<Product>();         
            foreach (var ion in glycan.Ions)
            {
                Product product = new Product(ProductType.M, FragmentationTerminus.Both, possiblePeptideMass + (double)ion.IonMass/1E5, 0, 0, (double)ion.LossIonMass/1E5);
                YIons.Add(product);
            }
            return YIons;
        }

        public static PeptideWithSetModifications GenerateNGlycopeptide(int position, PeptideWithSetModifications peptide, Glycan glycan)
        {
            Modification modification = Glycan.NGlycanToModification(glycan);


            Dictionary<int, Modification> testMods = new Dictionary<int, Modification> { { position, modification } };

            if (!peptide.AllModsOneIsNterminus.Keys.Contains(position))
            {
                foreach (var mod in peptide.AllModsOneIsNterminus)
                {
                    testMods.Add(mod.Key, mod.Value);
                }
            }

            var testPeptide = new PeptideWithSetModifications(peptide.Protein, peptide.DigestionParams, peptide.OneBasedStartResidueInProtein,
                peptide.OneBasedEndResidueInProtein, peptide.CleavageSpecificityForFdrCategory, peptide.PeptideDescription, peptide.MissedCleavages, testMods, peptide.NumFixedMods);
            
            return testPeptide;

        }

        public static List<Product> NGlyGetTheoreticalFragments(DissociationType dissociationType, PeptideWithSetModifications peptide, PeptideWithSetModifications modPeptide, Glycan glycan)
        {
            List<Product> theoreticalProducts = new List<Product>();

            HashSet<double> masses = new HashSet<double>();
            List<Product> products = new List<Product>();

            if (dissociationType == DissociationType.HCD || dissociationType == DissociationType.CID)
            {
                peptide.Fragment(dissociationType, FragmentationTerminus.Both, products);

                List<Product> modProducts = new List<Product>();

                modPeptide.Fragment(dissociationType, FragmentationTerminus.Both, modProducts);

                var glycanYIons = GlycoPeptides.GetGlycanYIons(peptide, glycan);

                products.AddRange(modProducts.Where(p => p.ProductType == ProductType.D || (p.ProductType != ProductType.M && p.NeutralLoss > 0)));

                products.AddRange(glycanYIons);

            }
            else if (dissociationType == DissociationType.ETD)
            {
                modPeptide.Fragment(dissociationType, FragmentationTerminus.Both, products);
            }
            else if (dissociationType == DissociationType.EThcD)
            {
                peptide.Fragment(DissociationType.HCD, FragmentationTerminus.Both, products);

                List<Product> modProducts = new List<Product>();

                modPeptide.Fragment(DissociationType.HCD, FragmentationTerminus.Both, modProducts);

                products.AddRange(modProducts.Where(p => p.ProductType == ProductType.D || (p.ProductType != ProductType.M && p.NeutralLoss > 0)));

                List<Product> etdProducts = new List<Product>();

                modPeptide.Fragment(DissociationType.ETD, FragmentationTerminus.Both, etdProducts);

                products.AddRange(etdProducts.Where(p => p.ProductType != ProductType.y));

                var glycanYIons = GlycoPeptides.GetGlycanYIons(peptide, glycan);

                products.AddRange(glycanYIons);
            }
            
            foreach (var fragment in products)
            {
                if (!masses.Contains(fragment.NeutralMass))
                {
                    masses.Add(fragment.NeutralMass);
                    theoreticalProducts.Add(fragment);
                }
            }

            return theoreticalProducts;
        }

        //The oxoniumIonIntensities is related with Glycan.AllOxoniumIons. 
        //Rules are coded in the function.    
        public static bool NGlyOxoniumIonsAnalysis(double[] oxoniumIonsintensities, Glycan glycan)
        {
            //If a glycopeptide spectrum does not have 292.1027 or 274.0921, then remove all glycans that have sialic acids from the search.
            if (oxoniumIonsintensities[10] <= 0 && oxoniumIonsintensities[12] <= 0)
            {
                if (glycan.Kind[2] != 0 || glycan.Kind[3] != 0)
                {
                    return false;
                }
            }

            //If a spectrum has 366.1395, remove glycans that do not have HexNAc(1)Hex(1) or more. Here use the total glycan of glycanBox to calculate. 
            if (oxoniumIonsintensities[14] > 0)
            {
                if (glycan.Kind[0] < 1 && glycan.Kind[1] < 1)
                {
                    return false;
                }
            }

            //Other rules:
            //A spectrum needs to have 204.0867 to be considered as a glycopeptide.              
            //Ratio of 138.055 to 144.0655 can seperate O/N glycan. 

            return true;
        }

        #endregion

        #region O-Glyco related functions

        //TO THINK: filter reasonable fragments here. The final solution is to change mzLib.Proteomics.PeptideWithSetModifications.Fragment
        public static List<Product> OGlyGetTheoreticalFragments(DissociationType dissociationType, PeptideWithSetModifications peptide, PeptideWithSetModifications modPeptide)
        {
            List<Product> theoreticalProducts = new List<Product>();        
            HashSet<double> masses = new HashSet<double>();

            List<Product> products = new List<Product>();
            if (dissociationType == DissociationType.HCD || dissociationType == DissociationType.CID)
            {
                List<Product> diag = new List<Product>();
                modPeptide.Fragment(dissociationType, FragmentationTerminus.Both, diag);
                peptide.Fragment(dissociationType, FragmentationTerminus.Both, products);
                products = products.Concat(diag.Where(p => p.ProductType != ProductType.b && p.ProductType != ProductType.y)).ToList();
            }
            else if(dissociationType == DissociationType.ETD)
            {
                modPeptide.Fragment(dissociationType, FragmentationTerminus.Both, products);
            }
            else if(dissociationType == DissociationType.EThcD)
            {
                List<Product> diag = new List<Product>();
                modPeptide.Fragment(DissociationType.HCD, FragmentationTerminus.Both, diag);
                peptide.Fragment(DissociationType.HCD, FragmentationTerminus.Both, products);
                products = products.Concat(diag.Where(p => p.ProductType != ProductType.b && p.ProductType != ProductType.y)).ToList();

                List<Product> etdProducts = new List<Product>();
                modPeptide.Fragment(DissociationType.ETD, FragmentationTerminus.Both, etdProducts);
                products = products.Concat(etdProducts.Where(p => p.ProductType != ProductType.y)).ToList();
            }

            foreach (var fragment in products)
            {
                if (!masses.Contains(fragment.NeutralMass))
                {
                    masses.Add(fragment.NeutralMass);
                    theoreticalProducts.Add(fragment);
                }           
            }

            return theoreticalProducts;
        }

        public static PeptideWithSetModifications OGlyGetTheoreticalPeptide(int[] theModPositions, PeptideWithSetModifications peptide, GlycanBox glycanBox)
        {
            Modification[] modifications = new Modification[glycanBox.NumberOfMods];
            for (int i = 0; i < glycanBox.NumberOfMods; i++)
            {
                modifications[i] = GlycanBox.GlobalOGlycanModifications[glycanBox.ModIds.ElementAt(i)];
            }

            Dictionary<int, Modification> testMods = new Dictionary<int, Modification>();
            foreach (var mod in peptide.AllModsOneIsNterminus)
            {
                testMods.Add(mod.Key, mod.Value);
            }

            for (int i = 0; i < theModPositions.Count(); i++)
            {
                testMods.Add(theModPositions.ElementAt(i), modifications[i]);
            }

            var testPeptide = new PeptideWithSetModifications(peptide.Protein, peptide.DigestionParams, peptide.OneBasedStartResidueInProtein,
                peptide.OneBasedEndResidueInProtein, peptide.CleavageSpecificityForFdrCategory, peptide.PeptideDescription, peptide.MissedCleavages, testMods, peptide.NumFixedMods);

            return testPeptide;
        }

        public static PeptideWithSetModifications OGlyGetTheoreticalPeptide(Route theModPositions, PeptideWithSetModifications peptide)
        {
            Modification[] modifications = new Modification[theModPositions.Mods.Count];
            for (int i = 0; i < theModPositions.Mods.Count; i++)
            {
                modifications[i] = GlycanBox.GlobalOGlycanModifications[theModPositions.Mods[i].Item2];
            }

            Dictionary<int, Modification> testMods = new Dictionary<int, Modification>();
            foreach (var mod in peptide.AllModsOneIsNterminus)
            {
                testMods.Add(mod.Key, mod.Value);
            }

            for (int i = 0; i < theModPositions.Mods.Count; i++)
            {
                testMods.Add(theModPositions.Mods[i].Item1, modifications[i]);
            }

            var testPeptide = new PeptideWithSetModifications(peptide.Protein, peptide.DigestionParams, peptide.OneBasedStartResidueInProtein,
                peptide.OneBasedEndResidueInProtein, peptide.CleavageSpecificityForFdrCategory, peptide.PeptideDescription, peptide.MissedCleavages, testMods, peptide.NumFixedMods);

            return testPeptide;
        }

        //The function here is to calculate permutation localization which could be used to compare with Graph-Localization.
        public static List<int[]> GetPermutations(List<int> allModPos, int[] glycanBoxId)
        {
            var length = glycanBoxId.Length;
            var indexes = Enumerable.Range(0, length).ToArray();
            int[] orderGlycan = new int[length];

            List<int[]> permutateModPositions = new List<int[]>();

            var combinations = Glycan.GetKCombs(allModPos, length);
        
            foreach (var com in combinations)
            {
                var permutation = Glycan.GetPermutations(com, length);

                HashSet<string> keys = new HashSet<string>();

                foreach (var per in permutation)
                {
                    Array.Sort(indexes);

                    var orderedPer = per.ToArray();
                    Array.Sort(orderedPer, indexes);
                                                         
                    for (int i = 0; i < length; i++)
                    {
                        orderGlycan[i] = glycanBoxId[indexes[i]];
                    }
                    var key = string.Join(",", orderGlycan.Select(p => p.ToString()));
                    if (!keys.Contains(key))
                    {
                        keys.Add(key);
                        permutateModPositions.Add(per.ToArray());
                    }
                }
            }

            return permutateModPositions;
        }

        //The purpose of the funtion is to generate hash fragment ions without generate the PeptideWithMod. keyValuePair key:GlycanBoxId, Value:mod sites
        public static int[] GetFragmentHash(List<Product> products, Tuple<int, int[]> keyValuePair, GlycanBox[] OGlycanBoxes, int FragmentBinsPerDalton)
        {
            double[] newFragments = products.OrderBy(p=>p.ProductType).ThenBy(p=>p.FragmentNumber).Select(p => p.NeutralMass).ToArray();
            var len = products.Count / 3;
            if (keyValuePair.Item2!=null)
            {
                for (int i = 0; i < keyValuePair.Item2.Length; i++)
                {
                    var j = keyValuePair.Item2[i];
                    while (j <= len + 1)
                    {
                        newFragments[j - 2] += (double)GlycanBox.GlobalOGlycans[OGlycanBoxes[keyValuePair.Item1].ModIds[i]].Mass/1E5;
                        j++;
                    }
                    j = keyValuePair.Item2[i];
                    while (j >= 3)
                    {
                        //y ions didn't change in EThcD for O-glyco
                        newFragments[len * 3 - j + 2] += (double)GlycanBox.GlobalOGlycans[OGlycanBoxes[keyValuePair.Item1].ModIds[i]].Mass/1E5;
                        j--;
                    }
                }
            }


            int[] fragmentHash = new int[products.Count];
            for (int i = 0; i < products.Count; i++)
            {
                fragmentHash[i] = (int)Math.Round(newFragments[i] * FragmentBinsPerDalton);
            }
            return fragmentHash;
        }

        //Find FragmentHash for current box at modInd. 
        //y-ion didn't change for O-Glycopeptide.
        public static List<double> GetLocalFragment(List<Product> products, int[] modPoses, int modInd, ModBox OGlycanBox, ModBox localOGlycanBox)
        {
            List<double> newFragments = new List<double>();
            var local_c_fragments = products.Where(p => p.ProductType == ProductType.c && p.AminoAcidPosition >= modPoses[modInd] - 1 && p.AminoAcidPosition < modPoses[modInd + 1] - 1).ToList();

            foreach (var c in local_c_fragments)
            {
                var newMass = c.NeutralMass + localOGlycanBox.Mass;
                newFragments.Add(newMass);
            }

            var local_z_fragments = products.Where(p => p.ProductType == ProductType.zDot && p.AminoAcidPosition >= modPoses[modInd] && p.AminoAcidPosition < modPoses[modInd + 1]).ToList();

            foreach (var z in local_z_fragments)
            {
                var newMass = z.NeutralMass + (OGlycanBox.Mass - localOGlycanBox.Mass);
                newFragments.Add(newMass);
            }

            return newFragments;
        }

        //Find FragmentMass for the fragments that doesn't contain localization Information. For example, "A|TAABBS|B", c1 and c7, z1 and z7, z8 ion don't contain localization information.
        public static List<double> GetUnlocalFragment(List<Product> products, int[] modPoses, ModBox OGlycanBox)
        {
            var mass = OGlycanBox.Mass;

            List<double> newFragments = new List<double>();
            var c_fragments = products.Where(p => p.ProductType == ProductType.c && p.AminoAcidPosition < modPoses.First() - 1).Select(p => p.NeutralMass);
            newFragments.AddRange(c_fragments);

            var c_fragments_shift = products.Where(p => p.ProductType == ProductType.c && p.AminoAcidPosition >= modPoses.Last() - 1).Select(p => p.NeutralMass);

            foreach (var c in c_fragments_shift)
            {
                var newMass = c + mass;
                newFragments.Add(newMass);
            }

            var z_fragments = products.Where(p => p.ProductType == ProductType.zDot && p.AminoAcidPosition > modPoses.Last() - 1).Select(p => p.NeutralMass);
            newFragments.AddRange(z_fragments);

            var z_fragments_shift = products.Where(p => p.ProductType == ProductType.zDot && p.AminoAcidPosition < modPoses.First() - 1).Select(p => p.NeutralMass);

            foreach (var z in z_fragments_shift)
            {
                var newMass = z + mass;
                newFragments.Add(newMass);
            }

            return newFragments;
        }

        //The oxoniumIonIntensities is related with Glycan.AllOxoniumIons. 
        //Rules are coded in the function.    
        public static bool OxoniumIonsAnalysis(double[] oxoniumIonsintensities, GlycanBox glycanBox)
        {
            //If a glycopeptide spectrum does not have 292.1027 or 274.0921, then remove all glycans that have sialic acids from the search.
            if (oxoniumIonsintensities[10] <= 0 && oxoniumIonsintensities[12] <= 0)
            {
                if (glycanBox.Kind[2] != 0 || glycanBox.Kind[3] != 0)
                {
                    return false;
                }
            }

            //If a spectrum has 366.1395, remove glycans that do not have HexNAc(1)Hex(1) or more. Here use the total glycan of glycanBox to calculate. 
            if (oxoniumIonsintensities[14] > 0)
            {
                if (glycanBox.Kind[0] < 1 && glycanBox.Kind[1] < 1)
                {
                    return false;
                }
            }

            //Other rules:
            //A spectrum needs to have 204.0867 to be considered as a glycopeptide.              
            //Ratio of 138.055 to 144.0655 can seperate O/N glycan.

            return true;
        }

        #endregion
    }
}

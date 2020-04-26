using System;
using System.Collections.Generic;
using System.Text;
using Proteomics.Fragmentation;
using Proteomics.ProteolyticDigestion;
using MzLibUtil;
using System.Linq;
using MassSpectrometry;

namespace EngineLayer.CrosslinkSearch
{
    public class CrosslinkMatrix
    {
        public XLNode[][] N_Matrix { get; set; }

        public XLNode[][] C_Matrix { get; set; }

        public int PeptideLength { get; }

        public int Depth { get; }

        public CrosslinkMatrix(int pepLen, int depth)
        {
            PeptideLength = pepLen;

            Depth = depth;

            N_Matrix = new XLNode[depth][];

            C_Matrix = new XLNode[depth][];

        }

        public static List<double> GenerateMassesToLocalize(Crosslinker crosslinker, DissociationType dissociationType, double otherPeptideMass)
        {
            List<double> massesToLocalize = new List<double>();
            if (crosslinker.Cleavable && crosslinker.CleaveDissociationTypes.Contains(dissociationType))
            {
                massesToLocalize.Add(crosslinker.CleaveMassShort);
                massesToLocalize.Add(crosslinker.CleaveMassLong);
            }
            else
            {
                massesToLocalize.Add(crosslinker.TotalMass + otherPeptideMass);
            }

            return massesToLocalize;
        }

        public static CrosslinkMatrix XLGetCrosslinkMatrix(Ms2ScanWithSpecificMass ms2Scan, DissociationType dissociationType, Crosslinker crosslinker, double otherPeptideMass, PeptideWithSetModifications peptide, Tolerance tolerance)
        {
            List<double> massesToLocalize = GenerateMassesToLocalize(crosslinker, dissociationType, otherPeptideMass);

            var matrix = BuildMatrix(dissociationType, massesToLocalize, peptide);

            CalMatrix(matrix, ms2Scan, tolerance);

            //var scores = GetAllScore(matrix);

            return matrix;
        }

        public static CrosslinkMatrix XLGetDeadendMatrix(Ms2ScanWithSpecificMass ms2Scan, DissociationType dissociationType, double deadendMass, PeptideWithSetModifications peptide, Tolerance tolerance)
        {
            var matrix = BuildMatrix(dissociationType, new List<double> { deadendMass }, peptide);

            CalMatrix(matrix, ms2Scan, tolerance);

            //var scores = GetAllScore(matrix);

            return matrix;
        }

        public static CrosslinkMatrix BuildMatrix(DissociationType dissociationType, List<double> massesToLocalize, PeptideWithSetModifications peptide)
        {
            List<Product> nfragments = new List<Product>();
            peptide.Fragment(dissociationType, FragmentationTerminus.N, nfragments);

            List<Product> cfragments = new List<Product>();
            peptide.Fragment(dissociationType, FragmentationTerminus.C, cfragments);

            CrosslinkMatrix matrix = new CrosslinkMatrix(peptide.Length, massesToLocalize.Count + 1);

            matrix.ConstructMatrix(nfragments, cfragments, massesToLocalize);

            return matrix;
        }

        public void ConstructMatrix(List<Product> nfragments, List<Product> cfragments, List<double> massesToLocalize)
        {
            for (int i = 0; i < Depth; i++)
            {
                N_Matrix[i] = new XLNode[nfragments.Count];
                C_Matrix[i] = new XLNode[cfragments.Count];
            }

            for (int i = 0; i < nfragments.Count; i++)
            {
                N_Matrix[0][i] = new XLNode(nfragments[i]);
            }

            for (int i = 0; i < cfragments.Count; i++)
            {
                C_Matrix[0][i] = new XLNode(cfragments[i]);
            }

            for (int j = 0; j < massesToLocalize.Count; j++)
            {
                for (int i = 0; i < nfragments.Count; i++)
                {
                    Product product = new Product(nfragments[i].ProductType, nfragments[i].Terminus, nfragments[i].NeutralMass + massesToLocalize[j], nfragments[i].FragmentNumber, nfragments[i].AminoAcidPosition, nfragments[i].NeutralLoss);
                    N_Matrix[j + 1][i] = new XLNode(product);
                }

                for (int i = 0; i < cfragments.Count; i++)
                {
                    Product product = new Product(cfragments[i].ProductType, cfragments[i].Terminus, cfragments[i].NeutralMass + massesToLocalize[j], cfragments[i].FragmentNumber, cfragments[i].AminoAcidPosition, cfragments[i].NeutralLoss);
                    C_Matrix[j + 1][i] = new XLNode(product);
                }
            }
        }

        public static void CalMatrix(CrosslinkMatrix crosslinkMatrix, Ms2ScanWithSpecificMass theScan, Tolerance productTolerance)
        {
            foreach (var rows in crosslinkMatrix.N_Matrix)
            {
                foreach (var n in rows)
                {
                    n.Cost = CalCost(theScan, productTolerance, n.Product);
                }
            }

            foreach (var rows in crosslinkMatrix.C_Matrix)
            {
                foreach (var n in rows)
                {
                    n.Cost = CalCost(theScan, productTolerance, n.Product);
                }
            }
        } 

        public static double CalCost(Ms2ScanWithSpecificMass theScan, Tolerance productTolerance, Product product)
        {
            double cost = 0;

            var closestExperimentalMass = theScan.GetClosestExperimentalIsotopicEnvelope(product.NeutralMass);

            if (productTolerance.Within(closestExperimentalMass.MonoisotopicMass, product.NeutralMass) && closestExperimentalMass.Charge <= theScan.PrecursorCharge)
            {
                cost = 1 + closestExperimentalMass.TotalIntensity / theScan.TotalIonCurrent;
            }

            return cost;
        }

        public static double[] GetAllScore(CrosslinkMatrix crosslinkMatrix)
        {
            double[] allScores = new double[crosslinkMatrix.PeptideLength];

            // crosslink at peptide position i+1
            for (int i = 0; i < crosslinkMatrix.PeptideLength; i++)
            {               
                allScores[i] = crosslinkMatrix.N_Matrix[0].Where(p => p.Product.AminoAcidPosition < i + 1).Sum(p => p.Cost);
                allScores[i] += crosslinkMatrix.C_Matrix[0].Where(p => p.Product.AminoAcidPosition > i + 1).Sum(p => p.Cost);

                for (int j = 1; j < crosslinkMatrix.Depth; j++)
                {
                    allScores[i] += crosslinkMatrix.N_Matrix[j].Where(p => p.Product.AminoAcidPosition >= i + 1).Sum(p => p.Cost);
                    allScores[i] += crosslinkMatrix.C_Matrix[j].Where(p => p.Product.AminoAcidPosition <= i+1).Sum(p => p.Cost);
                }
            }

            return allScores;
        }

        // crosslink at peptide position position+1
        public static List<Product> GetProducts(CrosslinkMatrix crosslinkMatrix, int position)
        {
            List<Product> products = new List<Product>();

            products.AddRange(crosslinkMatrix.N_Matrix[0].Where(p => p.Product.AminoAcidPosition < position + 1).Select(p=>p.Product));
            products.AddRange(crosslinkMatrix.C_Matrix[0].Where(p => p.Product.AminoAcidPosition > position + 1).Select(p => p.Product));

            for (int j = 1; j < crosslinkMatrix.Depth; j++)
            {
                products.AddRange(crosslinkMatrix.N_Matrix[j].Where(p => p.Product.AminoAcidPosition >= position + 1).Select(p => p.Product));
                products.AddRange(crosslinkMatrix.C_Matrix[j].Where(p => p.Product.AminoAcidPosition <= position + 1).Select(p => p.Product));
            }

            return products;
        }
    }

    public class XLNode
    {
        public XLNode(Product product)
        {
            Product = product;
        }

        public Product Product { get; }

        public double Cost { get; set; }

        public double ChildCost { get; set; }

    }
}

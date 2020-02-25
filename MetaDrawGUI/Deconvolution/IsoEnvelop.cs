﻿using System.Collections.Generic;
using System.Linq;
using Proteomics.Fragmentation;
using Chemistry;
using System.Text;

namespace MassSpectrometry
{
    public class IsoEnvelop
    {
        public IsoEnvelop() { }

        public IsoEnvelop(MzPeak[] exp, MzPeak[] theo, double mass, int charge, List<int> theoPeakIndex)
        {
            ExperimentIsoEnvelop = exp;
            TheoIsoEnvelop = theo;
            MonoisotopicMass = mass;
            Charge = charge;
            TheoPeakIndex = theoPeakIndex;
        }

        public MzPeak[] ExperimentIsoEnvelop { get; set; }

        public MzPeak[] TheoIsoEnvelop { get; set; }

        public List<int> TheoPeakIndex { get; set; }

        public double MonoisotopicMass { get; set; }

        public double Mz
        {
            get
            {
                return MonoisotopicMass.ToMz(Charge);
            }
        }

        public MzPeak[] ExistedExperimentPeak
        {
            get
            {
                return ExperimentIsoEnvelop.Where(p => p.Intensity > 0).ToArray();
            }
        }

        public double TotalIntensity
        {
            get
            {
                return ExperimentIsoEnvelop.Sum(p => p.Intensity);
            }
        }

        public double IntensityRatio { get; set; }

        public int Charge { get; set; }

        public double MsDeconvScore { get; set; }

        public double MsDeconvSignificance { get; set; }

        public int ScanNum { get; set; }
        public double RT { get; set; }

        //For NeuCode Feature
        public bool HasPartner { get; set; } = false;

        public bool IsLight { get; set; } = false;

        public IsoEnvelop Partner { get; set; }

        //For Ms2 Spectrum Match
        public Product Product { get; set; }

        public static string TabSeparatedHeader
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append("ScanNum" + "\t");
                sb.Append("RT" + "\t");
                sb.Append("monoisotopicMass" + "\t");
                sb.Append("MZ" + "\t");
                sb.Append("TotalIntensity" + "\t");
                sb.Append("charge" + "\t");
                return sb.ToString();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(ScanNum + "\t");
            sb.Append(RT + "\t");
            sb.Append(MonoisotopicMass + "\t");
            sb.Append(ClassExtensions.ToMz(MonoisotopicMass, Charge) + "\t");
            sb.Append(TotalIntensity + "\t");
            sb.Append(Charge + "\t");
            return sb.ToString();
        }

    }
}
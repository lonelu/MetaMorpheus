using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaDrawGUI
{
    public class MsFeature
    {
        public MsFeature(string line, char[] split, Dictionary<string, int> parsedHeader)
        {
            generateMsFeatures(line, split, parsedHeader);
        }

        public MsFeature(int aid, double monoMass, double abundance, double apexRT)
        {
            id = aid;
            MonoMass = monoMass;
            Abundance = abundance;
            ApexRT = apexRT;
        }

        public int id { get; set; }
        public double MonoMass { get; set; }
        public double Abundance { get; set; }
        public double ApexRT { get; set; }

        public int ScanNum {get; set;}
        public double AvgMass { get; set; }
        public int PeakChargeRange { get; set; }
        public int PeakMinCharge { get; set; }
        public int PeakMaxCharge { get; set; }
        public int PeakCount { get; set; }
        public List<double> PeakMzs { get; set; }
        public List<int> PeakCharges { get; set; }
        public List<double> PeakMasses { get; set; }
        public List<int> PeakIsotopeIndices { get; set; }
        public List<double> PeakIntensities { get; set; }
        public double IsotopeCosineScore { get; set; }
        public double ChargeIntensityCosineScore { get; set; }


        //The feature is from ms2 scan
        public bool ContainOxiniumIon { get; set; }

        private void generateMsFeatures(string line, char[] split, Dictionary<string, int> parsedHeader)
        {
            var spl = line.Split(split);

            MonoMass = parsedHeader[TsvHeader_MsFeature.monoMass] > 0 ? double.Parse(spl[parsedHeader[TsvHeader_MsFeature.monoMass]]) : -1;
            Abundance = parsedHeader[TsvHeader_MsFeature.abundance] > 0 ? double.Parse(spl[parsedHeader[TsvHeader_MsFeature.abundance]]) : -1;
            ApexRT = parsedHeader[TsvHeader_MsFeature.apexRT] > 0 ? double.Parse(spl[parsedHeader[TsvHeader_MsFeature.apexRT]]) : -1;

            if (parsedHeader[TsvHeader_MsFeature.specID] > 0)
            {
                var specID = spl[parsedHeader[TsvHeader_MsFeature.specID]];
                ScanNum = int.Parse( specID.Split('=').Last());
            }

            MonoMass = parsedHeader[TsvHeader_MsFeature.monoisotopicMass] > 0 ? double.Parse(spl[parsedHeader[TsvHeader_MsFeature.monoisotopicMass]]) : -1;
            Abundance = parsedHeader[TsvHeader_MsFeature.aggregatedIntensity] > 0 ? double.Parse(spl[parsedHeader[TsvHeader_MsFeature.aggregatedIntensity]]) : -1;
            ApexRT = parsedHeader[TsvHeader_MsFeature.retentionTime] > 0 ? double.Parse(spl[parsedHeader[TsvHeader_MsFeature.retentionTime]]) : -1;

            AvgMass = parsedHeader[TsvHeader_MsFeature.avgMass] > 0 ? double.Parse(spl[parsedHeader[TsvHeader_MsFeature.avgMass]]) : -1;
            PeakChargeRange = parsedHeader[TsvHeader_MsFeature.peakChargeRange] > 0 ? int.Parse(spl[parsedHeader[TsvHeader_MsFeature.peakChargeRange]]) : -1;
            PeakMinCharge = parsedHeader[TsvHeader_MsFeature.peakMinCharge] > 0 ? int.Parse(spl[parsedHeader[TsvHeader_MsFeature.peakMinCharge]]) : -1;
            PeakMaxCharge = parsedHeader[TsvHeader_MsFeature.peakMaxCharge] > 0 ? int.Parse(spl[parsedHeader[TsvHeader_MsFeature.peakMaxCharge]]) : -1;
            PeakCount = parsedHeader[TsvHeader_MsFeature.peakCount] > 0 ? int.Parse(spl[parsedHeader[TsvHeader_MsFeature.peakCount]]) : -1;

            if (parsedHeader[TsvHeader_MsFeature.peakMzs] > 0)
            {
                PeakMzs = new List<double>();
                var peakMzsString = spl[parsedHeader[TsvHeader_MsFeature.peakMzs]].Split(';').Select(tag => tag.Trim()).Where(tag => !string.IsNullOrEmpty(tag));
                foreach (var p in peakMzsString)
                {
                    
                    PeakMzs.Add(double.Parse(p));
                }
            }

            if (parsedHeader[TsvHeader_MsFeature.peakCharges] > 0)
            {
                PeakCharges = new List<int>();
                var peakChargesString = spl[parsedHeader[TsvHeader_MsFeature.peakCharges]].Split(';').Select(tag => tag.Trim()).Where(tag => !string.IsNullOrEmpty(tag)); ;
                foreach (var p in peakChargesString)
                {
                    PeakCharges.Add(int.Parse(p));
                }
            }

            if (parsedHeader[TsvHeader_MsFeature.peakMasses] > 0)
            {
                PeakMasses = new List<double>();
                var peakMassesString = spl[parsedHeader[TsvHeader_MsFeature.peakMasses]].Split(';').Select(tag => tag.Trim()).Where(tag => !string.IsNullOrEmpty(tag)); ;
                foreach (var p in peakMassesString)
                {
                    PeakMasses.Add(double.Parse(p));
                }
            }

            if (parsedHeader[TsvHeader_MsFeature.peakIsotopeIndices] > 0)
            {
                PeakIsotopeIndices = new List<int>();
                var PeakIsotopeIndicesString = spl[parsedHeader[TsvHeader_MsFeature.peakIsotopeIndices]].Split(';').Select(tag => tag.Trim()).Where(tag => !string.IsNullOrEmpty(tag)); ;
                foreach (var p in PeakIsotopeIndicesString)
                {
                    PeakIsotopeIndices.Add(int.Parse(p));
                }
            }

            if (parsedHeader[TsvHeader_MsFeature.peakIntensities] > 0)
            {
                PeakIntensities = new List<double>();
                var peakIntensitiesString = spl[parsedHeader[TsvHeader_MsFeature.peakIntensities]].Split(';').Select(tag => tag.Trim()).Where(tag => !string.IsNullOrEmpty(tag)); ;
                foreach (var p in peakIntensitiesString)
                {
                    PeakIntensities.Add(double.Parse(p));
                }
            }

            IsotopeCosineScore = parsedHeader[TsvHeader_MsFeature.isotopeCosineScore] > 0 ? double.Parse(spl[parsedHeader[TsvHeader_MsFeature.isotopeCosineScore]]) : -1;
            ChargeIntensityCosineScore = parsedHeader[TsvHeader_MsFeature.chargeIntensityCosineScore] > 0 ? double.Parse(spl[parsedHeader[TsvHeader_MsFeature.chargeIntensityCosineScore]]) : -1;
        }
    }
}

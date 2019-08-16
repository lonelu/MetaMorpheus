using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using EngineLayer;


namespace MetaDrawGUI
{
    public class TsvReader_MsFeature
    {
        public static List<MsFeature> ReadTsv(string filepath)
        {
            List<MsFeature> msFeatures = new List<MsFeature>();

            StreamReader reader = null;
            try
            {
                reader = new StreamReader(filepath);
            }
            catch (Exception e)
            {
                throw new MetaMorpheusException("Could not read file: " + e.Message);
            }

            int lineCount = 0;
            string line;
            Dictionary<string, int> parsedHeader = null;

            char[] Split = new char[] { '\t' };

            while (reader.Peek() > 0)
            {
                lineCount++;
                line = reader.ReadLine();

                if (lineCount == 1)
                {
                    parsedHeader = ParseHeader_MsFeature(line, Split);
                    continue;
                }

                //try
                //{
                    msFeatures.Add(new MsFeature(line, Split, parsedHeader));
                //}
                //catch (Exception e)
                //{
                //    throw new MetaMorpheusException("Could not read file: " + e.Message);
                //}
            }
            reader.Close();
            if ((lineCount - 1) != msFeatures.Count)
            {
                throw new MetaMorpheusException("Warning: " + ((lineCount - 1) - msFeatures.Count) + " PSMs were not read.");
            }
            return msFeatures;
        }

        private static Dictionary<string, int> ParseHeader_MsFeature(string header, char[] Split)
        {
            var parsedHeader = new Dictionary<string, int>();
            var spl = header.Split(Split);
            parsedHeader.Add(TsvHeader_MsFeature.monoMass, Array.IndexOf(spl, TsvHeader_MsFeature.monoMass));
            parsedHeader.Add(TsvHeader_MsFeature.abundance, Array.IndexOf(spl, TsvHeader_MsFeature.abundance));
            parsedHeader.Add(TsvHeader_MsFeature.apexRT, Array.IndexOf(spl, TsvHeader_MsFeature.apexRT));

            parsedHeader.Add(TsvHeader_MsFeature.specID, Array.IndexOf(spl, TsvHeader_MsFeature.specID));
            parsedHeader.Add(TsvHeader_MsFeature.monoisotopicMass, Array.IndexOf(spl, TsvHeader_MsFeature.monoisotopicMass));
            parsedHeader.Add(TsvHeader_MsFeature.avgMass, Array.IndexOf(spl, TsvHeader_MsFeature.avgMass));
            parsedHeader.Add(TsvHeader_MsFeature.peakChargeRange, Array.IndexOf(spl, TsvHeader_MsFeature.peakChargeRange));
            parsedHeader.Add(TsvHeader_MsFeature.peakMinCharge, Array.IndexOf(spl, TsvHeader_MsFeature.peakMinCharge));
            parsedHeader.Add(TsvHeader_MsFeature.peakMaxCharge, Array.IndexOf(spl, TsvHeader_MsFeature.peakMaxCharge));
            parsedHeader.Add(TsvHeader_MsFeature.aggregatedIntensity, Array.IndexOf(spl, TsvHeader_MsFeature.aggregatedIntensity));
            parsedHeader.Add(TsvHeader_MsFeature.retentionTime, Array.IndexOf(spl, TsvHeader_MsFeature.retentionTime));
            parsedHeader.Add(TsvHeader_MsFeature.peakCount, Array.IndexOf(spl, TsvHeader_MsFeature.peakCount));
            parsedHeader.Add(TsvHeader_MsFeature.peakMzs, Array.IndexOf(spl, TsvHeader_MsFeature.peakMzs));
            parsedHeader.Add(TsvHeader_MsFeature.peakCharges, Array.IndexOf(spl, TsvHeader_MsFeature.peakCharges));
            parsedHeader.Add(TsvHeader_MsFeature.peakMasses, Array.IndexOf(spl, TsvHeader_MsFeature.peakMasses));
            parsedHeader.Add(TsvHeader_MsFeature.peakIsotopeIndices, Array.IndexOf(spl, TsvHeader_MsFeature.peakIsotopeIndices));
            parsedHeader.Add(TsvHeader_MsFeature.peakIntensities, Array.IndexOf(spl, TsvHeader_MsFeature.peakIntensities));
            parsedHeader.Add(TsvHeader_MsFeature.isotopeCosineScore, Array.IndexOf(spl, TsvHeader_MsFeature.isotopeCosineScore));
            parsedHeader.Add(TsvHeader_MsFeature.chargeIntensityCosineScore, Array.IndexOf(spl, TsvHeader_MsFeature.chargeIntensityCosineScore));
            return parsedHeader;
        }

    }


    public static class TsvHeader_MsFeature
    {
        public const string monoMass = "MonoMass";
        public const string abundance = "Abundance";
        public const string apexRT = "ApexRetentionTime";

        public const string specID = "SpecID";
        public const string monoisotopicMass = "MonoisotopicMass";
        public const string avgMass = "AvgMass";
        public const string peakChargeRange = "PeakChargeRange";
        public const string peakMinCharge = "PeakMinCharge";
        public const string peakMaxCharge = "PeakMaxCharge";
        public const string aggregatedIntensity = "AggregatedIntensity";
        public const string retentionTime = "RetentionTime";
        public const string peakCount = "PeakCount";
        public const string peakMzs = "PeakMZs";
        public const string peakCharges = "PeakCharges";
        public const string peakMasses = "PeakMasses";
        public const string peakIsotopeIndices = "PeakIsotopeIndices";
        public const string peakIntensities = "PeakIntensities";
        public const string isotopeCosineScore = "IsotopeCosineScore";
        public const string chargeIntensityCosineScore = "ChargeIntensityCosineScore";
    }
}
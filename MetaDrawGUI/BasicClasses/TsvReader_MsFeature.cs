using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using EngineLayer;


namespace MetaDrawGUI
{
    public enum TsvFeatureType
    {
        MetaMorpheus,
        FlashDeconv,
        MaxQuant, 
        Dinosaur,
        OpenMS
    }

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
            TsvFeatureType tsvType = TsvFeatureType.FlashDeconv;

            char[] Split = new char[] { '\t' };

            while (reader.Peek() > 0)
            {
                lineCount++;
                line = reader.ReadLine();

                if (lineCount == 1)
                {
                    if (line.StartsWith("Raw file"))
                    {
                        tsvType = TsvFeatureType.MaxQuant;
                    }
                    else if (line.StartsWith("mz"))
                    {
                        tsvType = TsvFeatureType.Dinosaur;
                    }
                    else if (line.StartsWith("#FEATURE"))
                    {
                        tsvType = TsvFeatureType.OpenMS;
                    }

                    switch (tsvType)
                    {
                        case TsvFeatureType.MetaMorpheus:
                            break;
                        case TsvFeatureType.FlashDeconv:
                            parsedHeader = ParseHeader_MsFeature(line, Split);
                            break;
                        case TsvFeatureType.MaxQuant:
                            parsedHeader = ParseHeader_MaxQuant_MsFeature(line, Split);
                            break;
                        case TsvFeatureType.Dinosaur:
                            parsedHeader = ParseHeader_Dinosaur_MsFeature(line, Split);
                            break;
                        case TsvFeatureType.OpenMS:
                            parsedHeader = ParseHeader_OpenMS_MsFeature(line, Split);
                            break;
                        default:
                            break;
                    }
                    
                    continue;
                }

                //try
                //{
                    msFeatures.Add(new MsFeature(line, Split, parsedHeader, tsvType));
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
            parsedHeader.Add(TsvHeader_MsFeature.Intensity, Array.IndexOf(spl, TsvHeader_MsFeature.Intensity));
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

        private static Dictionary<string, int> ParseHeader_MaxQuant_MsFeature(string header, char[] Split)
        {
            var parsedHeader = new Dictionary<string, int>();
            var spl = header.Split(Split);
            parsedHeader.Add(TsvHeader_MaxQuant_MsFeature.monoMass, Array.IndexOf(spl, TsvHeader_MaxQuant_MsFeature.monoMass));
            parsedHeader.Add(TsvHeader_MaxQuant_MsFeature.Mz, Array.IndexOf(spl, TsvHeader_MaxQuant_MsFeature.Mz));
            parsedHeader.Add(TsvHeader_MaxQuant_MsFeature.UncalMz, Array.IndexOf(spl, TsvHeader_MaxQuant_MsFeature.UncalMz));
            parsedHeader.Add(TsvHeader_MaxQuant_MsFeature.Intensity, Array.IndexOf(spl, TsvHeader_MaxQuant_MsFeature.Intensity));
            parsedHeader.Add(TsvHeader_MaxQuant_MsFeature.Charge, Array.IndexOf(spl, TsvHeader_MaxQuant_MsFeature.Charge));
            parsedHeader.Add(TsvHeader_MaxQuant_MsFeature.RT, Array.IndexOf(spl, TsvHeader_MaxQuant_MsFeature.RT));
            parsedHeader.Add(TsvHeader_MaxQuant_MsFeature.RTlength, Array.IndexOf(spl, TsvHeader_MaxQuant_MsFeature.RTlength));
            parsedHeader.Add(TsvHeader_MaxQuant_MsFeature.MinScanNum, Array.IndexOf(spl, TsvHeader_MaxQuant_MsFeature.MinScanNum));
            parsedHeader.Add(TsvHeader_MaxQuant_MsFeature.MaxScanNum, Array.IndexOf(spl, TsvHeader_MaxQuant_MsFeature.MaxScanNum));
            return parsedHeader;
        }

        private static Dictionary<string, int> ParseHeader_Dinosaur_MsFeature(string header, char[] Split)
        {
            var parsedHeader = new Dictionary<string, int>();
            var spl = header.Split(Split);
            parsedHeader.Add(TsvHeader_Dinosaur_MsFeature.monoMass, Array.IndexOf(spl, TsvHeader_Dinosaur_MsFeature.monoMass));
            parsedHeader.Add(TsvHeader_Dinosaur_MsFeature.Mz, Array.IndexOf(spl, TsvHeader_Dinosaur_MsFeature.Mz));
            parsedHeader.Add(TsvHeader_Dinosaur_MsFeature.Intensity, Array.IndexOf(spl, TsvHeader_Dinosaur_MsFeature.Intensity));
            parsedHeader.Add(TsvHeader_Dinosaur_MsFeature.Charge, Array.IndexOf(spl, TsvHeader_Dinosaur_MsFeature.Charge));
            parsedHeader.Add(TsvHeader_Dinosaur_MsFeature.RTStart, Array.IndexOf(spl, TsvHeader_Dinosaur_MsFeature.RTStart));
            parsedHeader.Add(TsvHeader_Dinosaur_MsFeature.RTEnd, Array.IndexOf(spl, TsvHeader_Dinosaur_MsFeature.RTEnd));
            parsedHeader.Add(TsvHeader_Dinosaur_MsFeature.apexRT, Array.IndexOf(spl, TsvHeader_Dinosaur_MsFeature.apexRT));
            return parsedHeader;
        }

        private static Dictionary<string, int> ParseHeader_OpenMS_MsFeature(string header, char[] Split)
        {
            var parsedHeader = new Dictionary<string, int>();
            var spl = header.Split(Split);
            parsedHeader.Add(TsvHeader_OpenMS_MsFeature.Mz, Array.IndexOf(spl, TsvHeader_OpenMS_MsFeature.Mz));
            parsedHeader.Add(TsvHeader_OpenMS_MsFeature.Intensity, Array.IndexOf(spl, TsvHeader_OpenMS_MsFeature.Intensity));
            parsedHeader.Add(TsvHeader_OpenMS_MsFeature.Charge, Array.IndexOf(spl, TsvHeader_OpenMS_MsFeature.Charge));
            parsedHeader.Add(TsvHeader_OpenMS_MsFeature.RTStart, Array.IndexOf(spl, TsvHeader_OpenMS_MsFeature.RTStart));
            parsedHeader.Add(TsvHeader_OpenMS_MsFeature.RTEnd, Array.IndexOf(spl, TsvHeader_OpenMS_MsFeature.RTEnd));
            parsedHeader.Add(TsvHeader_OpenMS_MsFeature.apexRT, Array.IndexOf(spl, TsvHeader_OpenMS_MsFeature.apexRT));
            return parsedHeader;
        }

    }


    public static class TsvHeader_MsFeature
    {
        public const string monoMass = "MonoMass";
        public const string Intensity = "Abundance";
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

    public static class TsvHeader_MaxQuant_MsFeature
    {
        public const string monoMass = "Mass";
        public const string Mz = "m/z";
        public const string UncalMz = "Uncalibrated m/z";
        public const string Intensity = "Intensity";       
        public const string Charge = "Charge";
        public const string RT = "Retention time";
        public const string RTlength = "Retention length";
        public const string MinScanNum = "Min scan number";
        public const string MaxScanNum = "Max scan number";

    }

    public static class TsvHeader_Dinosaur_MsFeature
    {
        public const string monoMass = "mass";
        public const string Mz = "mz";
        public const string Intensity = "intensitySum";
        public const string Charge = "charge";
        public const string apexRT = "rtApex";
        public const string RTStart = "RTStart";
        public const string RTEnd = "RTEnd";
    }

    public static class TsvHeader_OpenMS_MsFeature
    {
        public const string Mz = "mz";
        public const string Intensity = "intensity";
        public const string Charge = "charge";
        public const string apexRT = "rt";
        public const string RTStart = "rt_start";
        public const string RTEnd = "rt_end";
    }
}
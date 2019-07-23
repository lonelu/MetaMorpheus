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

                try
                {
                    msFeatures.Add(new MsFeature(line, Split, parsedHeader));
                }
                catch (Exception e)
                {
                    throw new MetaMorpheusException("Could not read file: " + e.Message);
                }
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
            return parsedHeader;
        }

    }


    public static class TsvHeader_MsFeature
    {
        public const string monoMass = "MonoMass";
        public const string abundance = "Abundance";
        public const string apexRT = "ApexRetentionTime";
    }
}

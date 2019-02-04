using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using EngineLayer;

namespace MetaDrawGUI
{
    public class TsvReader_pGlyco
    {
        public static List<SimplePsm> ReadTsv(string filepath)
        {
            List<SimplePsm> simplePsms = new List<SimplePsm>();

            StreamReader reader = null;
            try
            {
                reader = new StreamReader(filepath);
            }
            catch(Exception e)
            {
                throw new MetaMorpheusException("Could not read file: " + e.Message);
            }

            int lineCount = 0;
            string line;
            Dictionary<string, int> parsedHeader = null;

            while (reader.Peek() > 0)
            {
                lineCount++;
                line = reader.ReadLine();

                if (lineCount == 1)
                {

                    continue;
                }

                try
                {
                    var spl = line.Split('\t');
                    simplePsms.Add(new SimplePsm(Int32.Parse(spl[0].Split('.')[1]), spl[5], spl[9]));
                }
                catch (Exception e)
                {
                    throw new MetaMorpheusException("Could not read file: " + e.Message);
                }
            }
            reader.Close();
            if ((lineCount - 1) != simplePsms.Count)
            {
                throw new MetaMorpheusException("Warning: " + ((lineCount - 1) - simplePsms.Count) + " PSMs were not read.");
            }
            return simplePsms;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MetaDrawGUI
{
    public class MultiproteaseCrosslink
    {
        public static void Read(string filepath)
        {
            HashSet<Tuple<int, int>>[] crosslinks = new HashSet<Tuple<int, int>>[4];

            HashSet<Tuple<int, int>> crosslinksAll = new HashSet<Tuple<int, int>>();

            HashSet<Tuple<int, int>> crosslinks12 = new HashSet<Tuple<int, int>>();
            HashSet<Tuple<int, int>> crosslinks13 = new HashSet<Tuple<int, int>>();
            HashSet<Tuple<int, int>> crosslinks14 = new HashSet<Tuple<int, int>>();            
            HashSet<Tuple<int, int>> crosslinks23 = new HashSet<Tuple<int, int>>();
            HashSet<Tuple<int, int>> crosslinks24 = new HashSet<Tuple<int, int>>();
            HashSet<Tuple<int, int>> crosslinks34 = new HashSet<Tuple<int, int>>();
            HashSet<Tuple<int, int>> crosslinks123 = new HashSet<Tuple<int, int>>();
            HashSet<Tuple<int, int>> crosslinks124 = new HashSet<Tuple<int, int>>();
            HashSet<Tuple<int, int>> crosslinks234 = new HashSet<Tuple<int, int>>();
            HashSet<Tuple<int, int>> crosslinks134 = new HashSet<Tuple<int, int>>();


            for (int i = 0; i < 4; i++)
            {
                crosslinks[i] = new HashSet<Tuple<int, int>>();
            }

            StreamReader reader = null;
            try
            {
                reader = new StreamReader(filepath);
            }
            catch (Exception e)
            {
                //throw new MetaMorpheusException("Could not read file: " + e.Message);
            }

            int lineCount = 0;
            string line;

            while (reader.Peek() > 0)
            {
                lineCount++;
                line = reader.ReadLine();

                if (lineCount == 1)
                {
                    continue;
                }
                var sps = line.Split(',');
                for (int i = 0; i < 8; i++)
                {
                    if (i%2 == 0)
                    {
                        if (sps[i] != "")
                        {
                            int n1 = int.Parse(sps[i]);
                            int n2 = int.Parse(sps[i + 1]);
                            if (n2 < n1)
                            {
                                int temp = n1;
                                n1 = n2;
                                n2 = temp;
                            }
                            var tuple = new Tuple<int, int>(n1, n2);
                            crosslinksAll.Add(tuple);
                            crosslinks[i / 2].Add(tuple);

                            if (i==0)
                            {
                                crosslinks12.Add(tuple);
                                crosslinks13.Add(tuple);
                                crosslinks14.Add(tuple);
                                crosslinks123.Add(tuple);
                                crosslinks134.Add(tuple);
                                crosslinks124.Add(tuple);
                            }
                            if (i==2)
                            {
                                crosslinks12.Add(tuple);
                                crosslinks23.Add(tuple);
                                crosslinks24.Add(tuple);
                                crosslinks123.Add(tuple);
                                crosslinks124.Add(tuple);
                                crosslinks234.Add(tuple);
                            }
                            if (i==4)
                            {
                                crosslinks13.Add(tuple);
                                crosslinks23.Add(tuple);
                                crosslinks34.Add(tuple);
                                crosslinks123.Add(tuple);
                                crosslinks134.Add(tuple);
                                crosslinks234.Add(tuple);
                            }
                            if (i==6)
                            {
                                crosslinks14.Add(tuple);
                                crosslinks24.Add(tuple);
                                crosslinks34.Add(tuple);
                                crosslinks124.Add(tuple);
                                crosslinks134.Add(tuple);
                                crosslinks234.Add(tuple);
                            }
                        }                       
                    }
                }
                            
            }

            var c12 = crosslinks12.Count();
            var c13 = crosslinks13.Count();
            var c14 = crosslinks14.Count();
            var c23 = crosslinks23.Count();
            var c24 = crosslinks24.Count();
            var c34 = crosslinks34.Count();
            var c123 = crosslinks123.Count();
            var c124 = crosslinks124.Count();
            var c134 = crosslinks134.Count();
            var c234 = crosslinks234.Count();
            var all = crosslinksAll.Count();
        }

    }
}

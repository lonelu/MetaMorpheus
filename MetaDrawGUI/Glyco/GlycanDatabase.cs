using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using EngineLayer;


namespace MetaDrawGUI.Glyco
{
    public class GlycanDatabase
    {
        private static IEnumerable<Glycan> NGlycans { get; set; }

        #region LoadKindGlycan based on Structured Glycan
        public static IEnumerable<Glycan> LoadKindGlycan(string filePath, IEnumerable<Glycan> NGlycans)
        {
            var groupedGlycans = NGlycans.GroupBy(p => Glycan.GetKindString(p.Kind)).ToDictionary(p => p.Key, p => p.ToList());

            using (StreamReader lines = new StreamReader(filePath))
            {
                int id = 1;
                while (lines.Peek() != -1)
                {
                    string line = lines.ReadLine().Split('\t').First();

                    byte[] kind = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                    var x = line.Split('(', ')');
                    int i = 0;
                    while (i < x.Length - 1)
                    {
                        kind[Glycan.NameCharDic[x[i]].Item2] = byte.Parse(x[i + 1]);
                        i = i + 2;
                    }

                    var mass = Glycan.GetMass(kind);

                    var glycans = GetAllIonMassFromKind(kind, groupedGlycans);

                    var glycan = new Glycan(glycans.First().Struc, mass, kind, glycans.First().Ions, true);
                    glycan.GlyId = id++;
                    yield return glycan;
                }
            }
        }

        //Find glycans from structured glycan database
        public static List<Glycan> GetAllIonMassFromKind(byte[] kind, Dictionary<string, List<Glycan>> groupedGlycans)
        {
            var kindKey = Glycan.GetKindString(kind);
            List<Glycan> glycans = new List<Glycan>();

            groupedGlycans.TryGetValue(kindKey, out glycans);

            if (glycans == null)
            {
                //if not in the structured glycan database, find a smaller one.
                bool notFound = true;
                while (notFound)
                {
                    var childKinds = BuildChildKindKey(kind);
                    foreach (var child in childKinds)
                    {
                        var key = Glycan.GetKindString(child);
                        if (groupedGlycans.TryGetValue(key, out glycans))
                        {
                            notFound = false;
                            break;
                        }
                    }

                    if (notFound == true)
                    {
                        glycans = GetAllIonMassFromKind(childKinds[0], groupedGlycans);
                        notFound = false;
                    }
                }
            }

            return glycans;
        }

        private static List<byte[]> BuildChildKindKey(byte[] kind)
        {
            List<byte[]> childKinds = new List<byte[]>();
            for (int i = kind.Length - 1; i >= 0; i--)
            {
                if (kind[i] >= 1)
                {
                    var childKind = new byte[kind.Length];
                    Array.Copy(kind, childKind, kind.Length);
                    childKind[i]--;
                    childKinds.Add(childKind);
                }
            }
            return childKinds;
        }

        public static void GlyTest_GenerateDataBase(IEnumerable<Glycan> NGlycans)
        {

            string aietdpath = @"GlycoTestData/ComboGlycanDatabase.csv";
            var glycans = GlycanDatabase.LoadKindGlycan(aietdpath, NGlycans).ToList();

            string aietdpathWritePath = "E:\\MassData\\Glycan\\GlycanDatabase\\AIETD\\GlycansAIETD.tsv";
            using (StreamWriter output = new StreamWriter(aietdpathWritePath))
            {
                foreach (var glycan in glycans)
                {
                    output.WriteLine(glycan.Mass.ToString() + "\t" + glycan.Struc + "\t" + Glycan.GetKindString(glycan.Kind) + "\t" + Glycan.GetKindString(Glycan.GetKind(glycan.Struc)));
                }
            }
        }


        //This is not exactly a test. The function is used for N-Glycan database generation. The function maybe useful in the future.
        public static void GlyTest_GenerateUniprotDataBase()
        {
            var groupedGlycans = NGlycans.GroupBy(p => Glycan.GetKindString(p.Kind)).ToDictionary(p => p.Key, p => p.ToList());
            string pathWritePath = "E:\\MassData\\Glycan\\GlycanDatabase\\UniprotGlycanDatabase.txt";


            using (StreamWriter output = new StreamWriter(pathWritePath))
            {
                foreach (var glycan in groupedGlycans)
                {
                    var mod = Glycan.NGlycanToModification(glycan.Value.First());
                    List<string> temp = new List<string>();
                    temp.Add(mod.ToString());
                    temp.Add(@"//");
                    foreach (var v in temp)
                    {
                        output.WriteLine(v);
                    }
                }
            }

        }

        #endregion

        //public static void GlyTest_GetAllIonMassFromKind()
        //{
        //    var groupedGlycans = NGlycans.GroupBy(p => Glycan.GetKindString(p.Kind)).ToDictionary(p => p.Key, p => p.ToList());

        //    byte[] k36101 = new byte[] { 3, 6, 1, 0, 1, 0, 0, 0, 0 };
        //    var glycan36101 = GlycanDatabase.GetAllIonMassFromKind(k36101, groupedGlycans);

        //    Glycan glycan = Glycan.Struct2Glycan("(N(F)(N(H(H(N))(H(N)))))", 0);
        //    var x = GlycanDatabase.GetAllIonMassFromKind(glycan.Kind, groupedGlycans);
        //    Assert.AreEqual(x.First().Ions.Count, 14);

        //    string aietdpath = @"GlycoTestData/ComboGlycanDatabase.csv";
        //    var glycans = GlycanDatabase.LoadKindGlycan(aietdpath, NGlycans).ToList();
        //    Assert.AreEqual(glycans.Count, 182);

        //}
    }
}

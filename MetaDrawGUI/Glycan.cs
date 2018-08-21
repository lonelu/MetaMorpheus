using System.Collections.Generic;
using MetaDrawGUI;
using System.IO;
using System.Linq;
using System;
using System.Text.RegularExpressions;

namespace EngineLayer
{
    public class Glycan
    {
        public Glycan(int glyId, int glyType, string struc, double mass, int[] kind, List<GlycanIon> ions)
        {
            GlyId = glyId;
            GlyType = glyType;
            Struc = struc;
            Mass = mass;
            Kind = kind;
            Ions = ions;
        }
        public int GlyId { get; set; }
        public int GlyType { get; set; }
        public string Struc { get; set; }
        public double Mass { get; set; }
        public int[] Kind { get; set; }
        public List<GlycanIon> Ions { get; set; }
        public static Dictionary<char, double> CharMassDic = new Dictionary<char, double>() { { 'H', 162.0528 } , { 'N', 203.0794 }, { 'A', 291.0954 }, { 'G', 307.0903 }, { 'F', 146.0579 } };

        public Dictionary<int, double> GetDiagnosticIons()
        {
            Dictionary<int, double> diagnosticIons = new Dictionary<int, double>();
            if (Kind[1] >= 1)
            {
                diagnosticIons.Add(126, 126.055);
                diagnosticIons.Add(138, 138.055);
                diagnosticIons.Add(144, 144.065);
                diagnosticIons.Add(168, 168.066);
                diagnosticIons.Add(186, 186.076);
                diagnosticIons.Add(204, 204.087);
            }
            if (Kind[1] >= 1 && Kind[0] >= 1)
            {
                diagnosticIons.Add(366, 366.140);
            }
            if (Kind[2] >= 1)
            {
                diagnosticIons.Add(274, 274.092);
                diagnosticIons.Add(292, 292.103);
            }
            return diagnosticIons;
        }

        public bool SameComponentGlycan(Glycan glycan)
        {
            return this.Kind == glycan.Kind;
        }

        public static Node ReadGlycan(string theGlycanStruct)
        {
            Node curr = new Node(theGlycanStruct[1]);
            for (int i = 2; i < theGlycanStruct.Length - 1; i++)
            {

                if (theGlycanStruct[i] != null)
                {
                    if (theGlycanStruct[i] == '(')
                    {
                        continue;
                    }
                    if (theGlycanStruct[i] == ')')
                    {
                        curr = curr.father;
                    }
                    else
                    {
                        if (curr.lChild == null)
                        {
                            curr.lChild = new Node(theGlycanStruct[i]);
                            curr.lChild.father = curr;
                            curr = curr.lChild;
                        }
                        else
                        {
                            curr.rChild = new Node(theGlycanStruct[i]);
                            curr.rChild.father = curr;
                            curr = curr.rChild;
                        }
                    }

                }
            }
            return curr;
        }

        public static List<Node> LoadGlycanStruct(string glycanStructLocation)
        {
            List<Node> trees = new List<Node>();
            using (StreamReader glycans = new StreamReader(glycanStructLocation))
            {
                while (glycans.Peek() != -1)
                {
                    string line = glycans.ReadLine();
                    var t = ReadGlycan(line);
                    trees.Add(t);
                }
            }
            return trees;
        }

        public static string PrintOutGlycan(Node node)
        {
            string output = "";
            if (node!=null)
            {
                output += "(" + node.value + PrintOutGlycan(node.lChild) + PrintOutGlycan(node.rChild) + ")";
            }
            return output;
        }

        public static List<Node> GetAllChildrenCombination(Node node)
        {
            List<Node> nodes = new List<Node>();
            var curr = node;
            if (curr.lChild == null && curr.rChild == null)
            {
                nodes.Add(curr);
            }
            else
            {
                List<Node> l = GetAllChildrenCombination(curr.lChild);
                nodes.Add(new Node(curr.value));
                if (curr.rChild != null)
                {
                    List<Node> r = GetAllChildrenCombination(curr.rChild);
                    foreach (var lNode in l)
                    {
                        var c = new Node(curr.value);
                        c.lChild = lNode;
                        nodes.Add(c);
                    }
                    foreach (var rNode in r)
                    {
                        var c = new Node(curr.value);
                        c.rChild = rNode;
                        nodes.Add(c);
                    }
                    foreach (var lNode in l)
                    {
                        foreach (var rNode in r)
                        {
                            var c = new Node(curr.value);
                            c.lChild = lNode;
                            c.rChild = rNode;
                            nodes.Add(c);
                        }
                    }
                }
                else
                {
                    foreach (var lNode in l)
                    {
                        var c = new Node(curr.value);
                        c.lChild = lNode;
                        nodes.Add(c);
                    }
                }
            }

            return nodes;
        }

        public static double GetNodeMass(Node node)
        {
            string x = PrintOutGlycan(node);
            double y = CharMassDic['H'] * x.Count(p=>p=='H') +
                CharMassDic['N'] * x.Count(p => p == 'N') +
                CharMassDic['A'] * x.Count(p => p == 'A') +
                CharMassDic['G'] * x.Count(p => p == 'G') +
                CharMassDic['F'] * x.Count(p => p == 'F');
            return y;
        }

        public static SortedSet<double> GetAllChildrenMass(Node node)
        {
            SortedSet<double> masses = new SortedSet<double>();
            var allC = GetAllChildrenCombination(node);
            foreach (var aC in allC)
            {
                masses.Add(GetNodeMass(aC));
            }
            return masses;
        }

        public static Node GobackRootNode(Node node)
        {
            while (node.father != null)
            {
                node = node.father;
            }
            return node;
        }        

        public static IEnumerable<Glycan> LoadGlycanDatabase(string pGlycoLocation)
        {

            using (StreamReader glycans = new StreamReader(pGlycoLocation))
            {
                List<string> theGlycanString = new List<string>();

                while (glycans.Peek() != -1)
                {
                    string line = glycans.ReadLine();
                    theGlycanString.Add(line);
                    if (line.StartsWith("End"))
                    {
                        yield return ReadGlycan(theGlycanString);
                        theGlycanString = new List<string>();
                    }

                }
            }
        }   

        private static Glycan ReadGlycan(List<string> theGlycanString)
        {
            int _id = Convert.ToInt32(theGlycanString[1].Split('\t')[1]);
            int _type = Convert.ToInt32(theGlycanString[1].Split('\t')[3]); ;
            string _struc = theGlycanString[2].Split('\t')[1];
            double _mass = Convert.ToDouble(theGlycanString[3].Split('\t')[1]);
            var test = theGlycanString[4].Split('\t').Skip(1);
            int id;
            int[] _kind = theGlycanString[4].Split('\t').SelectMany(s => int.TryParse(s, out id) ? new[] { id } : new int[0]).ToArray();
            List<GlycanIon> glycanIons = new List<GlycanIon>();

            for (int i = 0; i < theGlycanString.Count; i++)
            {
                if (theGlycanString[i].StartsWith("IonStruct"))
                {
                    double _ionMass = Convert.ToDouble(theGlycanString[i + 1].Split('\t')[1]);
                    id = 0;
                    int[] _ionKind = theGlycanString[i + 2].Split('\t').SelectMany(s => int.TryParse(s, out id) ? new[] { id } : new int[0]).ToArray();
                    GlycanIon glycanIon = new GlycanIon(0, _ionMass, _ionKind);
                    glycanIons.Add(glycanIon);
                }
            }
            Glycan glycan = new Glycan(_id, _type, _struc, _mass, _kind, glycanIons);
            return glycan;
        }

        public static List<GlycanBox> SortGlycanDatabase(IEnumerable<Glycan> glycans)
        {
            List<GlycanBox> glycanBoxes = new List<GlycanBox>();

            var groupedGlycans = glycans.GroupBy(p => p.Mass).ToDictionary(p => p.Key, p => p.ToList());

            foreach (var aGroupedGlycan in groupedGlycans)
            {
                GlycanBox glycanBox = new GlycanBox();
                glycanBox.Mass = aGroupedGlycan.Key;
                glycanBox.glycans = aGroupedGlycan.Value;
                glycanBox.keyValuePairs = new Dictionary<double, List<int>>();
                foreach (var aGlycan in aGroupedGlycan.Value)
                {
                    foreach (var aIon in aGlycan.Ions)
                    {
                        if (glycanBox.keyValuePairs.ContainsKey(aIon.IonMass))
                        {
                            glycanBox.keyValuePairs[aIon.IonMass].Add(aGlycan.GlyId);
                        }
                        else
                        {
                            glycanBox.keyValuePairs.Add(aIon.IonMass, new List<int> { aGlycan.GlyId });
                        }
                    }
                }
                glycanBoxes.Add(glycanBox);
            }

            return glycanBoxes;
        }
    }

    public class GlycanIon
    {
        public GlycanIon(int ionStruct, double ionMass, int[] ionKind)
        {
            IonStruct = ionStruct;
            IonMass = ionMass;
            IonKind = ionKind;
        }
        public int IonStruct { get; set; }
        public double IonMass { get; set; }
        public int[] IonKind { get; set; }
    }

    public class GlycanBox
    {
        public double Mass { get; set; }

        public List<Glycan> glycans { get; set; }

        public List<GlycanIon> CommonGlycanIons { get; set; }

        public int NumberOfGlycans { get { return glycans.Count; } }

        public Dictionary<double, List<int>> keyValuePairs { get; set; }

    }
}
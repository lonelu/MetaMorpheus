using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaDrawGUI.Crosslink
{
    public static class SyntheticLibrary
    {
        public static Dictionary<string, Tuple<string, string, int, int>> TheoryCrosslinks
        {
            get
            {
                //<alpha peptide base sequence, beta peptide base sequence, alpha peptide link site, beta peptide link site>.
                Dictionary<string, Tuple<string, string, int, int>> dict = new Dictionary<string, Tuple<string, string, int, int>>();
       
                foreach (var g in Groups)
                {
                    for (int i = 0; i < g.Length; i++)
                    {
                        for (int j = i; j < g.Length; j++)
                        {

                            if (dict.Keys.Contains(g[i] + "_" + g[j]) || dict.Keys.Contains(g[j] + "_" + g[i]))
                            {
                                continue;
                            }

                            int a = g[i].IndexOf('K');
                            int b = g[j].IndexOf('K');

                            Tuple<string, string, int, int> tuple = new Tuple<string, string, int, int>(g[i], g[j], a, b);
                            dict.Add(g[i] + "_" + g[j], tuple);
                        }
                    }
                }

                return dict;
            }
        }

        public static List<string[]> Groups
        {
            get
            {
                var gs = new List<string[]>();
                gs.Add(group1); gs.Add(group2); gs.Add(group3); gs.Add(group4);
                gs.Add(group5); gs.Add(group6); gs.Add(group7); gs.Add(group8);
                gs.Add(group9); gs.Add(group10); gs.Add(group11); gs.Add(group12);
                return gs;
            }
        }

        //None peptides are replicated in the group1 to group12.
        public static string[] group1 = new string[8]
        {
            "SDKNR",
            "KLINGIR",
            "KFDNLTK",
            "FIKPILEK",
            "APLSASMIKR",
            "NPIDFLEAKGYK",
            "LPKYSLFELENGR",
            "TEVQTGGFSKESILPK"
        };

        public static string[] group2 = new string[8]
        {
            //"VKYVTEGMR",
            "FDNLTKAER",
            "DFQFYKVR",
            "YDENDKLIR",
            "MIAKSEQEIGK",
            "HKPENIVIEMAR",
            "TILDFLKSDGFANR",
            "KIECFDSVEISGVEDR",
            "YVNFLYLASHYEKLK"
        };

        public static string[] group3 = new string[7]
        {
            "LSKSR",
            "DKPIR",
            "KDLIIK",
            "MKNYWR",
            "KGILQTVK",
            "NSDKLIAR",
            "DDSIDNKVLTR"
        };

        public static string[] group4 = new string[8]
        {
            "KLVDSTDK",
            "IEKILTFR",
            "KAIVDLLFK",
            "VLSAYNKHR",
            "IEEGIKELGSQILK",
            "SSFEKNPIDFLEAK",
            "SNFDLAEDAKLQLSK",
            "HSLLYEYFTVYNELTKVK"
        };

        public static string[] group5 = new string[7]
        {
            "KVTVK",
            "EKIEK",
            "VITLKSK",
            "QLKEDYFK",
            "QLLNAKLITQR",
            "GGLSELDKAGFIK",
            "MDGTEELLVKLNR"
        };

        public static string[] group6 = new string[9]
        {
            "EVKVITLK",
            "KPAFLSGEQK",
            "ENQTTQKGQK",
            "KTEVQTGGFSK",
            "VVDELVKVMGR",
            "LESEFVYGDYKVYDVR",
            "MLASAGELQKGNELALPSK",
            "NFMQLIHDDSLTFKEDIQK",
            "VLPKHSLLYEYFTVYNELTK"
        };

        public static string[] group7 = new string[9]
        {
            "KMIAK",
            "ESILPKR",
            "DLIIKLPK",
            "FKVLGNTDR",
            "SEQEIGKATAK",
            "AIVDLLFKTNR",
            "LKTYAHLFDDK",
            "VNTEITKAPLSASMIK",
            "YDEHHQDLTLLKALVR"
        };

        public static string[] group8 = new string[8]
        {
            "KDWDPK",
            "QQLPEKYK",
            "KVLSMPQVNIVK",
            "MTNFDKNLPNEK",
            "QITKHVAQILDSR",
            "KSEETITPWNFEEVVDK",
            "KNGLFGNLIALSLGLTPNFK",
            "SKLVSDFR"
        };

        public static string[] group9 = new string[8]
        {
            "LKSVK",
            "IIKDK",
            "DWDPKK",
            "LKGSPEDNEQK",
            "VLSMPQVNIVKK",
            "LENLIAQLPGEKK",
            "LIYLALAHMIKFR",
            "YPKLESEFVYGDYK"
        };

        public static string[] group10 = new string[7]
        {
            "VPSKK",
            "VTVKQLK",
            "EDYFKK",
            "VKYVTEGMR",
            "GKSDNVPSEEVVK",
            "LEESFLVEEDKK",
            "QEDFYPFLKDNR"
        };

        public static string[] group11 = new string[8]
        {
            "GQKNSR",
            "AGFIKR",
            "GYKEVK",
            "VMKQLK",
            "KDFQFYK",
            "LVDSTDKADLR",
            "SDNVPSEEVVKK",
            "KNLIGALLFDSGETAEATR"
        };

        public static string[] group12 = new string[8]
        {
            "HSIKK",
            "DKQSGK",
            "NLPNEKVLPK",
            "QSGKTILDFLK",
            "MNTKYDENDK",
            "SVKELLGITIMER",
            "TYAHLFDDKVMK",
            "FNASLGTYHDLLKIIK"
        };

    }
}
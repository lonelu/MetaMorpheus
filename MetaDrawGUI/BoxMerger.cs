using System;
using System.Collections.Generic;
using BoxCar;
using System.IO;
using MassSpectrometry;
using EngineLayer;
using TaskLayer;
using System.Linq;


namespace MetaDrawGUI
{
    public class BoxMerger
    {
        //BoxCar for bottom-up
        public static void MergeBoxScans(List<string> MsDataFilePaths, MyFileManager spectraFileManager)
        {
            var boxcarRanges = GenerateRealRanges_td2_2_12();

            foreach (var filePath in MsDataFilePaths)
            {
                var msDataFile = spectraFileManager.LoadFile(filePath, new CommonParameters(trimMs1Peaks: false, trimMsMsPeaks: false));

                var scanSets = BoxCarFunctions.GenerateScanSet(msDataFile);

                scanSets = BoxCarFunctions.MergeBoxScans(scanSets, boxcarRanges);

                var scans = BoxCarFunctions.OutputMergedBoxScans(scanSets);

                WriteMzmlFile(scans, msDataFile, filePath + "_MergedBox_allScans2.mzML");
            }
        }

        public static void WriteMzmlFile(List<MsDataScan> scans, MsDataFile originalFile, string fileName)
        {

            SourceFile sourceFile = originalFile.SourceFile;
            MsDataFile msDataFile = new MsDataFile(scans.ToArray(), sourceFile);

            IO.MzML.MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(msDataFile, fileName, false);
        }

        private static List<BoxcarRange>[] GenerateRealRanges_bottomup_3_12()
        {
            List<BoxcarRange> toAddA = new List<BoxcarRange>();
            toAddA.Add(new BoxcarRange(400, 416.3));
            toAddA.Add(new BoxcarRange(441.2, 454.2));
            toAddA.Add(new BoxcarRange(476.3, 488.8));
            toAddA.Add(new BoxcarRange(510.3, 523.3));
            toAddA.Add(new BoxcarRange(545, 557.8));
            toAddA.Add(new BoxcarRange(580.8, 594));
            toAddA.Add(new BoxcarRange(618.4, 633));
            toAddA.Add(new BoxcarRange(660.3, 676.4));
            toAddA.Add(new BoxcarRange(708.3, 726.3));
            toAddA.Add(new BoxcarRange(764.4, 788.4));
            toAddA.Add(new BoxcarRange(837.9, 868.8));
            toAddA.Add(new BoxcarRange(945, 999));

            List<BoxcarRange> toAddB = new List<BoxcarRange>();
            toAddB.Add(new BoxcarRange(415.3, 429.7));
            toAddB.Add(new BoxcarRange(453.2, 465.9));
            toAddB.Add(new BoxcarRange(487.8, 499.9));
            toAddB.Add(new BoxcarRange(522.3, 534.8));
            toAddB.Add(new BoxcarRange(556.8, 569.6));
            toAddB.Add(new BoxcarRange(593, 606.6));
            toAddB.Add(new BoxcarRange(632, 646.8));
            toAddB.Add(new BoxcarRange(675.4, 692.3));
            toAddB.Add(new BoxcarRange(725.3, 745));
            toAddB.Add(new BoxcarRange(787.4, 812.4));
            toAddB.Add(new BoxcarRange(867.8, 903.5));
            toAddB.Add(new BoxcarRange(998, 1071.1));

            List<BoxcarRange> toAddC = new List<BoxcarRange>();
            toAddC.Add(new BoxcarRange(428.7, 442.2));
            toAddC.Add(new BoxcarRange(464.9, 477.3));
            toAddC.Add(new BoxcarRange(498.9, 511.3));
            toAddC.Add(new BoxcarRange(533.8, 546));
            toAddC.Add(new BoxcarRange(568.6, 581.8));
            toAddC.Add(new BoxcarRange(605.6, 619.4));
            toAddC.Add(new BoxcarRange(645.8, 661.3));
            toAddC.Add(new BoxcarRange(691.3, 709.3));
            toAddC.Add(new BoxcarRange(744, 765.4));
            toAddC.Add(new BoxcarRange(811.4, 838.9));
            toAddC.Add(new BoxcarRange(902.5, 946));
            toAddC.Add(new BoxcarRange(1070.1, 1201));

            var boxcarRanges = new List<BoxcarRange>[3] { new List<BoxcarRange>(toAddA), new List<BoxcarRange>(toAddB), new List<BoxcarRange>(toAddC) };
            return boxcarRanges;
        }

        private static List<BoxcarRange>[] GenerateRealRanges_td_2_12()
        {
            List<BoxcarRange> toAddA = new List<BoxcarRange>();
            toAddA.Add(new BoxcarRange(400, 423.2));
            toAddA.Add(new BoxcarRange(441.2, 459.9));
            toAddA.Add(new BoxcarRange(476.3, 494.3));
            toAddA.Add(new BoxcarRange(510.3, 528.8));
            toAddA.Add(new BoxcarRange(545, 563.8));
            toAddA.Add(new BoxcarRange(580.8, 600.3));
            toAddA.Add(new BoxcarRange(618.4, 639.8));
            toAddA.Add(new BoxcarRange(660.3, 684.3));
            toAddA.Add(new BoxcarRange(708.3, 735.4));
            toAddA.Add(new BoxcarRange(764.4, 799.9));
            toAddA.Add(new BoxcarRange(837.9, 885.4));
            toAddA.Add(new BoxcarRange(945, 1032));

            List<BoxcarRange> toAddB = new List<BoxcarRange>();
            toAddB.Add(new BoxcarRange(422.2, 442.2));
            toAddB.Add(new BoxcarRange(458.9, 477.3));
            toAddB.Add(new BoxcarRange(493.3, 511.3));
            toAddB.Add(new BoxcarRange(527.8, 546));
            toAddB.Add(new BoxcarRange(562.8, 581.8));
            toAddB.Add(new BoxcarRange(599.3, 619.4));
            toAddB.Add(new BoxcarRange(638.8, 661.3));
            toAddB.Add(new BoxcarRange(683.3, 709.3));
            toAddB.Add(new BoxcarRange(734.4, 765.4));
            toAddB.Add(new BoxcarRange(798.9, 838.9));
            toAddB.Add(new BoxcarRange(884.4, 946));
            toAddB.Add(new BoxcarRange(1031, 1201));

            var boxcarRanges = new List<BoxcarRange>[2] { new List<BoxcarRange>(toAddA), new List<BoxcarRange>(toAddB) };
            return boxcarRanges;
        }

        private static List<BoxcarRange>[] GenerateRealRanges_td2_2_12()
        {
            List<BoxcarRange> toAddA = new List<BoxcarRange>();
            toAddA.Add(new BoxcarRange(499, 546.8));
            toAddA.Add(new BoxcarRange(591.2, 638));
            toAddA.Add(new BoxcarRange(682.8, 729.7));
            toAddA.Add(new BoxcarRange(774.5, 821.3));
            toAddA.Add(new BoxcarRange(866.2, 913));
            toAddA.Add(new BoxcarRange(957.8, 1004.7));
            toAddA.Add(new BoxcarRange(1049.5, 1096.3));
            toAddA.Add(new BoxcarRange(1141.2, 1188));
            toAddA.Add(new BoxcarRange(1232.8, 1279.7));
            toAddA.Add(new BoxcarRange(1324.5, 1371.3));
            toAddA.Add(new BoxcarRange(1416.2, 1463));
            toAddA.Add(new BoxcarRange(1507.8, 1554.7));

            List<BoxcarRange> toAddB = new List<BoxcarRange>();
            toAddB.Add(new BoxcarRange(545.8, 592.7));
            toAddB.Add(new BoxcarRange(637, 683.8));
            toAddB.Add(new BoxcarRange(728.7, 775.5));
            toAddB.Add(new BoxcarRange(820.3, 867.2));
            toAddB.Add(new BoxcarRange(912, 958.8));
            toAddB.Add(new BoxcarRange(1003.7, 1050.5));
            toAddB.Add(new BoxcarRange(1095.3, 1142.2));
            toAddB.Add(new BoxcarRange(1187, 1233.8));
            toAddB.Add(new BoxcarRange(1278.7, 1325.5));
            toAddB.Add(new BoxcarRange(1370.3, 1417.2));
            toAddB.Add(new BoxcarRange(1462, 1508.8));
            toAddB.Add(new BoxcarRange(1553.7, 1601));

            var boxcarRanges = new List<BoxcarRange>[2] { new List<BoxcarRange>(toAddA), new List<BoxcarRange>(toAddB) };
            return boxcarRanges;
        }

        //BoxCar for top-down

        public static void FixPrecursorAndWriteFile(List<string> MsDataFilePaths, MyFileManager spectraFileManager)
        {
            foreach (var filePath in MsDataFilePaths)
            {
                var msDataFile = spectraFileManager.LoadFile(filePath, new CommonParameters(trimMs1Peaks:false, trimMsMsPeaks:false));

                var scanSets = GenerateScanSetForFixPrecursor(msDataFile);

                var scans = FixPrecursorAndGenerateScanList(scanSets);

                WriteMzmlFile(scans, msDataFile, filePath + "_MergedBox_allScans2.mzML");
            }
        }

        private static List<SetOfScans> GenerateScanSetForFixPrecursor(MsDataFile file)
        {
            List<SetOfScans> sorted = new List<SetOfScans>();

            var scans = file.GetAllScansList();
            SetOfScans set = new SetOfScans();
            for (int i = 0; i < file.NumSpectra; i++)
            {
                if (scans[i].ScanFilter.Contains("Full ms "))
                {
                    if (i > 0)
                    {
                        sorted.Add(set);
                        set = new SetOfScans();
                    }
                    set.AddToMs1Scans(scans[i]);
                }
                else
                {
                    set.AddToMs2Scans(scans[i]);
                }
            }
            return sorted;
        }

        private static List<MsDataScan> FixPrecursorAndGenerateScanList(List<SetOfScans> setOfScans)
        {
            List<MsDataScan> scans = new List<MsDataScan>();
            foreach (var set in setOfScans)
            {
                scans.Add(set.Ms1scans.First());

                if (set.Ms2scans.Count == 0)
                {
                    continue;
                }
                for (int i = 0; i < set.Ms2scans.Count - 1; i = i + 2)
                {
                    set.Ms2scans[i].SetIsolationMz(set.Ms2scans[i + 1].IsolationMz.Value);
                    scans.Add(set.Ms2scans[i]);
                    scans.Add(set.Ms2scans[i+1]);
                }
            }
            return scans;
        }

        public static void WriteExtractedBoxVsNormal(string FilePath, List<(int scanNum, double RT, int isMatch, double diff, int same)> diff_plot)
        {
            var writtenFile = Path.Combine(Path.GetDirectoryName(FilePath), "box_vs_normal.tsv");
            using (StreamWriter output = new StreamWriter(writtenFile))
            {
                output.WriteLine("Scan\tRT\tIsMatched\tDiff\tSame");
                foreach (var s in diff_plot)
                {
                    output.WriteLine(s.scanNum + "\t" + s.RT + "\t" + s.isMatch + "\t" + s.diff + "\t" + s.same);
                }
            }
        }
    }
}

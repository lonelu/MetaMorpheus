using System;
using System.Collections.Generic;
using BoxCar;
using System.IO;
using MassSpectrometry;
using EngineLayer;
using TaskLayer;


namespace MetaDrawGUI
{
    public class BoxMerger
    {
        public void MergeBoxScans(List<string> MsDataFilePaths, MyFileManager spectraFileManager)
        {
            var boxcarRanges = GenerateRealRanges_td_2_12();

            foreach (var filePath in MsDataFilePaths)
            {
                var msDataFile = spectraFileManager.LoadFile(filePath, new CommonParameters());

                var scanSets = BoxCarFunctions.GenerateScanSet(msDataFile);

                scanSets = BoxCarFunctions.MergeBoxScans(scanSets, boxcarRanges);

                var scans = BoxCarFunctions.OutputMergedBoxScans(scanSets);

                WriteBoxMzmlFile(scans, msDataFile, Path.GetDirectoryName(filePath), msDataFile.SourceFile.FileName + "_MergedBox_allScans.mzML");
            }
        }

        public static void WriteBoxMzmlFile(List<MsDataScan> scans, MsDataFile originalFile, string filepath, string fileName)
        {

            SourceFile sourceFile = originalFile.SourceFile;
            MsDataFile msDataFile = new MsDataFile(scans.ToArray(), sourceFile);

            IO.MzML.MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(msDataFile, filepath + fileName, false);
        }

        private List<BoxcarRange>[] GenerateRealRanges_bottomup_3_12()
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

        private List<BoxcarRange>[] GenerateRealRanges_td_2_12()
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

            var boxcarRanges = new List<BoxcarRange>[2] { new List<BoxcarRange>(toAddA), new List<BoxcarRange>(toAddB) };
            return boxcarRanges;
        }


    }
}

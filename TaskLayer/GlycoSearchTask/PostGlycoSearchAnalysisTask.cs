﻿using EngineLayer;
using EngineLayer.GlycoSearch;
using EngineLayer.FdrAnalysis;
using Proteomics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MathNet.Numerics.LinearRegression;

namespace TaskLayer
{
    public class PostGlycoSearchAnalysisTask : MetaMorpheusTask
    {
        public PostGlycoSearchAnalysisTask() : base(MyTask.Search)
        {
        }

        protected override MyTaskResults RunSpecific(string OutputFolder, List<DbForTask> dbFilenameList, List<string> currentRawFileList, string taskId, FileSpecificParameters[] fileSettingsList)
        {
            return null;
        }

        public MyTaskResults Run(string OutputFolder, List<DbForTask> dbFilenameList, List<string> currentRawFileList, string taskId, FileSpecificParameters[] fileSettingsList, List<GlycoSpectralMatch> allPsms, CommonParameters commonParameters, GlycoSearchParameters glycoSearchParameters, List<Protein> proteinList, List<Modification> variableModifications, List<Modification> fixedModifications, List<string> localizeableModificationTypes, MyTaskResults MyTaskResults)
        {
            if (glycoSearchParameters.GlycoSearchType == GlycoSearchType.NGlycanSearch)
            {
                var allPsmsSingle = allPsms.Where(p => p.NGlycan == null ).OrderByDescending(p => p.Score).ToList();
                SingleFDRAnalysis(allPsmsSingle, commonParameters, new List<string> { taskId });

                var writtenFileSingle = Path.Combine(OutputFolder, "single" + ".psmtsv");
                WriteFile.WritePsmGlycoToTsv(allPsmsSingle, writtenFileSingle, 1);
                FinishedWritingFile(writtenFileSingle, new List<string> { taskId });

                var allPsmsGly = allPsms.Where(p => p.NGlycan != null ).OrderByDescending(p => p.Score).ToList();
                SingleFDRAnalysis(allPsmsGly, commonParameters, new List<string> { taskId });

                var writtenFileNGlyco = Path.Combine(OutputFolder, "nglyco" + ".psmtsv");
                WriteFile.WritePsmGlycoToTsv(allPsmsGly, writtenFileNGlyco, 3);
                FinishedWritingFile(writtenFileNGlyco, new List<string> { taskId });

                return MyTaskResults;
            }
            else if (glycoSearchParameters.GlycoSearchType == GlycoSearchType.OGlycanSearch)
            {
                var allPsmsSingle = allPsms.Where(p => p.Routes == null ).OrderByDescending(p => p.Score).ToList();
                SingleFDRAnalysis(allPsmsSingle, commonParameters, new List<string> { taskId });

                var writtenFileSingle = Path.Combine(OutputFolder, "single" + ".psmtsv");
                WriteFile.WritePsmGlycoToTsv(allPsmsSingle, writtenFileSingle, 1);
                FinishedWritingFile(writtenFileSingle, new List<string> { taskId });

                var allPsmsGly = allPsms.Where(p => p.Routes != null ).OrderByDescending(p => p.Score).ToList();
                SingleFDRAnalysis(allPsmsGly, commonParameters, new List<string> { taskId });

                var writtenFileOGlyco = Path.Combine(OutputFolder, "oglyco" + ".psmtsv");
                WriteFile.WritePsmGlycoToTsv(allPsmsGly, writtenFileOGlyco, 2);
                FinishedWritingFile(writtenFileOGlyco, new List<string> { taskId });

                var ProteinLevelLocalization = GlycoProteinParsimony.ProteinLevelGlycoParsimony(allPsmsGly.Where(p=>p.ProteinAccession!=null && p.OneBasedStartResidueInProtein.HasValue).ToList());

                var seen_oglyco_localization_file = Path.Combine(OutputFolder, "seen_oglyco_localization" + ".tsv");
                WriteFile.WriteSeenProteinGlycoLocalization(ProteinLevelLocalization, seen_oglyco_localization_file);
                FinishedWritingFile(seen_oglyco_localization_file, new List<string> { taskId });

                var protein_oglyco_localization_file = Path.Combine(OutputFolder, "protein_oglyco_localization" + ".tsv");
                WriteFile.WriteProteinGlycoLocalization(ProteinLevelLocalization, protein_oglyco_localization_file);
                FinishedWritingFile(protein_oglyco_localization_file, new List<string> { taskId });

                return MyTaskResults;
            }
            else
            {
                var allPsmsSingle = allPsms.Where(p => p.NGlycan == null && p.Routes == null).OrderByDescending(p => p.Score).ToList();
                SingleFDRAnalysis(allPsmsSingle, commonParameters, new List<string> { taskId });

                var writtenFileSingle = Path.Combine(OutputFolder, "single" + ".psmtsv");
                WriteFile.WritePsmGlycoToTsv(allPsmsSingle, writtenFileSingle, 1);
                FinishedWritingFile(writtenFileSingle, new List<string> { taskId });

                var allPsmsNGly = allPsms.Where(p => p.NGlycan != null).OrderByDescending(p => p.Score).ToList();
                SingleFDRAnalysis(allPsmsNGly, commonParameters, new List<string> { taskId });

                var writtenFileNGlyco = Path.Combine(OutputFolder, "nglyco" + ".psmtsv");
                WriteFile.WritePsmGlycoToTsv(allPsmsNGly, writtenFileNGlyco, 3);
                FinishedWritingFile(writtenFileNGlyco, new List<string> { taskId });

                var allPsmsOGly = allPsms.Where(p => p.Routes != null).OrderByDescending(p => p.Score).ToList();
                SingleFDRAnalysis(allPsmsOGly, commonParameters, new List<string> { taskId });

                var writtenFileOGlyco = Path.Combine(OutputFolder, "oglyco" + ".psmtsv");
                WriteFile.WritePsmGlycoToTsv(allPsmsOGly, writtenFileOGlyco, 2);
                FinishedWritingFile(writtenFileOGlyco, new List<string> { taskId });

                return MyTaskResults;
            }
        }

        //Calculate the FDR of single peptide FP/TP
        private void SingleFDRAnalysis(List<GlycoSpectralMatch> items, CommonParameters commonParameters, List<string> taskIds)
        {
            // calculate single PSM FDR
            List<PeptideSpectralMatch> psms = items.Select(p => p as PeptideSpectralMatch).ToList();
            new FdrAnalysisEngine(psms, 0, commonParameters, this.FileSpecificParameters, taskIds).Run();

        }

        public double[] RetentionTimeRegression(List<GlycoSpectralMatch> glycoSpectralMatches)
        {
            double[][] xs = new double[glycoSpectralMatches.Count][];

            double[] ys = glycoSpectralMatches.Select(p => p.ScanRetentionTime).ToArray();
       
            for (int i = 0; i < glycoSpectralMatches.Count; i++)
            {
                var glycanKind = GlycanBox.OGlycanBoxes[glycoSpectralMatches[i].Routes.First().ModBoxId].Kind;

                xs[i] = new double[1 + glycanKind.Length];

                xs[i][0] = glycoSpectralMatches[i].PredictedHydrophobicity;

                for (int j = 1; j <= glycanKind.Length; j++)
                {
                    xs[i][j] = glycanKind[j - 1];
                }   
            }

            using (StreamWriter output = new StreamWriter(@"E:\MassData\Glycan\Nick_2019_StcE\Rep1\_temp2\2020-05-19-17-12-20\Task1-GlycoSearchTask\RetentionTime.csv"))
            {
                for (int i = 0; i < xs.Length; i++)
                {
                    string line = "";
                    for (int j = 0; j < xs[0].Length; j++)
                    {
                        line += xs[i][j].ToString() + "\t";
                    }
                    line += ys[i].ToString();
                    output.WriteLine(line);
                }
            }

            double[] p = MultipleRegression.QR(xs, ys, intercept: false);

            //double[] p = Fit.MultiDim(xs, ys, intercept: true);

            return p;
        } 

        public void RetentionTimePrediction(List<GlycoSpectralMatch> glycoSpectralMatches, double[] regression)
        {
            foreach (var gsm in glycoSpectralMatches)
            {
                var glycanKind = GlycanBox.OGlycanBoxes[gsm.Routes.First().ModBoxId].Kind;

                var xs = new double[1 + glycanKind.Length];

                xs[0] = gsm.PredictedHydrophobicity;

                for (int j = 1; j <= glycanKind.Length; j++)
                {
                    xs[j] = glycanKind[j - 1];
                }

                double prt = regression[0];

                for (int j = 1; j <= xs.Length; j++)
                {
                    prt += regression[j] * xs[j - 1];
                }

                gsm.PredictedRT = prt;
            }
        }

    }
}


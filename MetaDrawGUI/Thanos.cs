using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.IO;
using ViewModels;
using EngineLayer;
using System.Collections.Generic;
using MzLibUtil;
using System.Text.RegularExpressions;
using MassSpectrometry;
using System.Globalization;
using System.ComponentModel;
using TaskLayer;
using System.Threading.Tasks;
using System.Threading;

namespace MetaDrawGUI
{
    public class Thanos
    {
        public Thanos()
        {
            MsDataFilePaths = new List<string>();
            spectraFileManager = new MyFileManager(true);
        }

        public List<string> MsDataFilePaths { get; set; }

        MyFileManager spectraFileManager { get; set; }

        public void AllFilesForBoxCar(double low, double high, int binNum)
        {
            double[] allFileBoxCar = new double[binNum];


            foreach (var filePath in MsDataFilePaths)
            {
                var msDataFile = spectraFileManager.LoadFile(filePath, new CommonParameters());

                var x = CalculateNormalizedIntensitiesForBoxCar(msDataFile, low, high, binNum);

                for (int i = 0; i < x.Length; i++)
                {
                    allFileBoxCar[i] += x[i];
                }
            }

            WriteNormalizedIntensities(MsDataFilePaths.First(), binNum, allFileBoxCar);
        }

        private double[] CalculateNormalizedIntensitiesForBoxCar(MsDataFile msDataFile, double low, double high, int binNum)
        {
            double[] all = new double[binNum];

            var ranges = GenerateBinsForIntensities(low, high, binNum);

            var ms1scans = msDataFile.GetMS1Scans();

            double[][] allBins = new double[ms1scans.Count()][];


            Parallel.For(0, ms1scans.Count(), i =>
            {
                allBins[i] = new double[binNum];
                allBins[i] = BinIntensities(ms1scans.ElementAt(i), ranges);
            });

            Parallel.For(0, binNum, i =>
            {
                for (int j = 0; j < ms1scans.Count(); j++)
                {
                    all[i] += allBins[j][i];
                }
            });

            return all;
        }

        private double[] BinIntensities(MsDataScan msDataScan, List<Tuple<double, double>> ranges)
        {
            double[] vs = new double[ranges.Count];
            int idx = 0;
            foreach (var range in ranges)
            {
                int lowInd = Array.BinarySearch(msDataScan.MassSpectrum.XArray, range.Item1);
                if (lowInd < 0)
                {
                    lowInd = ~lowInd;
                }
                int highInd = Array.BinarySearch(msDataScan.MassSpectrum.XArray, range.Item2);
                if (highInd < 0)
                {
                    highInd = ~highInd;
                }
                highInd--;

                for (int i = lowInd; i <= highInd; i++)
                {
                    vs[idx] += msDataScan.MassSpectrum.YArray[i];
                }
                idx++;
            }
            return vs;
        }
        
        private List<Tuple<double, double>> GenerateBinsForIntensities(double low, double high, int binNum)
        {
            var binSize = (high - low) / binNum;

            List<Tuple<double, double>> tuples = new List<Tuple<double, double>>();

            for (int i = 0; i < binNum; i++)
            {
                tuples.Add(new Tuple<double, double>(low + binSize*i, low+binSize*(i+1)));
            }

            return tuples;
        }

        private void WriteNormalizedIntensities(string FilePath, int binNum, double[] all)
        {
            var writtenFile = Path.Combine(Path.GetDirectoryName(FilePath), "IntensityNormalization_" + DateTime.Now.ToFileTimeUtc() + ".mytsv");
            using (StreamWriter output = new StreamWriter(writtenFile))
            {
                output.WriteLine("BinNum\tIntensity");
                for (int i = 0; i < binNum; i++)
                {
                    output.WriteLine(i + "\t" + all[i]);   
                }
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using MzLibUtil;
using MassSpectrometry;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;

using ViewModels;
using EngineLayer;
using System.Text.RegularExpressions;
using System.Globalization;
using System.ComponentModel;
using TaskLayer;
using System.Threading;

/// <summary>
/// Written by Nicole Frey, May-June 2019 for the Smith Group in the UW Madison chemistry department, with direction from Leah Schaffer.
/// </summary>
namespace BoxCar
{
    /// <summary>
    /// Merges the scans in a data file that were taken using the boxcar method.
    /// Writes an mzml file containing the merged scans.
    /// </summary>
    public class BoxCarFunctions
    {

        /// <summary>
        /// Removes the overlap from the boxcar ranges
        /// so that each boxcar shares a startpoint with the endpoint of the previous car
        /// and an endpoint with the startpoint of the next car.
        /// 
        /// Does this process by taking the mean between the boxcar ranges and setting the cars' start and endpoints to the means of their neighbors.
        /// Overlap removal is important because when these boxcar ranges are matched with the scan data, they will remove the edge effects from the scan.
        /// Edge effects: sometimes the scan picks up on peaks that it shouldn't near the beginning and end of that boxcar.
        /// 
        /// EXAMPLE:
        /// 
        /// boxcars[0]      boxcars[1]      boxcars[2] ... (if you took more kinds of boxcar scans they would be additional columns)
        /// (400, 426)      (424, 451)      (449, 476)
        /// (474, 501)      (499, 526)      (524, 551)      (these rows are the BoxcarRanges)
        ///     .               .               .
        ///     .               .               .
        ///     .               .               .
        /// (1124, 1151)    (1149, 1176)    (1174,1201) 
        /// 
        /// How the loop works:
        /// there are x columns and y rows.
        /// Each column is a SetOfBoxcarRanges (which is stored in an arraylist) 
        /// Each row contains some BoxcarRanges, and if you read across the rows or down the columns they increase.
        /// The nested for loops look at the rows first, and then the columns. 
        /// This way, each BoxcarRange that is looped through is greater than the last BoxcarRange and you can calculate the means.
        /// 
        /// So, once the function loops through, the entries are changed to:
        /// 
        /// boxcars[0]      boxcars[1]      boxcars[2] ... 
        /// (400, 425)      (425, 450)      (450, 475)
        /// (475, 500)      (500, 525)      (525, 550)      
        ///     .               .               .
        ///     .               .               .
        ///     .               .               .
        /// (1125, 1150)    (1150, 1175)    (1175,1201) - the last entry (and the first entry) doesn't change because there's no next entry to compute the mean
        /// 
        /// In reality, the intervals are not all the same, so it's slightly more complicated than the example
        /// 
        /// </summary>
        /// <param name="boxcars"></param>
        /// <returns></returns>
        public static List<BoxcarRange>[] RemoveOverlap(List<BoxcarRange>[] boxcars)
        {
            int numBoxcarScans = boxcars.Count();

            List<BoxcarRange>[] newBoxcars = new List<BoxcarRange>[numBoxcarScans];

            if (numBoxcarScans > 1)
            {
                BoxcarRange rangeA = boxcars[0].ElementAt(0);
                BoxcarRange rangeB = boxcars[1].ElementAt(0);
                // loop through all complete rows and columns of the "matrix":
                for (int y = 0; y < boxcars[0].Count(); y++) // loops through all rows including a possibly incomplete last row w/ nulls/empty cells in it
                {
                    for (int x = 0; x < numBoxcarScans; x++)
                    {
                        if (x < (numBoxcarScans - 1) && (y <= boxcars[x + 1].Count())) // if you aren't on the last column and there is an entry in the yth row of the next column, the rangeB is in the next column
                        {
                            rangeB = boxcars[x + 1].ElementAt(y);
                        }
                        else if (x == (numBoxcarScans - 1) && (y != (boxcars[0].Count() - 1))) // if you're on the last column and there is another row (even a partial row), rangeB is the first column in the next row
                        {
                            rangeB = boxcars[0].ElementAt(y + 1);
                        }
                        else // if you've reached the last entry
                        {
                            return boxcars;
                        }
                        // find the mean of the endpoint of rangeA and the startpoint of rangeB
                        double endA = rangeA.End;
                        double startB = rangeB.Start;
                        double mean = CalculateMean(endA, startB);
                        // change the endpoint of rangeA and the startpoint of rangeB to be that number
                        rangeA.End = mean;
                        rangeB.Start = mean;
                        // insert rangeA and rangeB
                        //boxcars[x].ReplaceAtIndex(rangeA, y);
                        boxcars[x][y] = rangeA;
                        if (x < (numBoxcarScans - 1) && (y <= boxcars[x + 1].Count())) // if you aren't on the last column, insert rangeB into the next column
                        {
                            //boxcars[x + 1].ReplaceAtIndex(rangeB, y);
                            boxcars[x + 1][y] = rangeB;
                        }
                        else if (x == (numBoxcarScans - 1) && (y != boxcars[0].Count())) // if you're on the last column, insert rangeB into the first column in the next row
                        {
                            //boxcars[0].ReplaceAtIndex(rangeB, y + 1);
                            boxcars[0][y + 1] = rangeB;
                        }
                        rangeA = rangeB;
                    }
                }
                return boxcars;
            }
            else
            {
                return boxcars;
            }
        }

        /// <summary>
        /// Helper method for RemoveOverlap
        /// given 2 numbers, returns the mean
        /// </summary>
        /// <param name="num1"></param>
        /// <param name="num2"></param>
        /// <returns></returns> double mean
        public static double CalculateMean(double num1, double num2)
        {
            return ((num1 + num2) / 2);
        }

        public static List<SetOfScans> GenerateScanSet(MsDataFile file)
        {
            List<SetOfScans> sorted = new List<SetOfScans>();

            var scans = file.GetAllScansList();
            SetOfScans set = new SetOfScans();
            for (int i = 0; i < file.NumSpectra; i++)
            {
                if (scans[i].ScanFilter.Contains("Full ms "))
                {
                    if (i>0)
                    {
                        sorted.Add(set);
                        set = new SetOfScans();
                    }                   
                    set.AddToMs1Scans(scans[i]);
                }
                else if (scans[i].ScanFilter.Contains("Full msx ms"))
                {
                    set.AddToBoxcarScans(scans[i]);
                }
                else
                {
                    set.AddToMs2Scans(scans[i]);
                }
            }
            return sorted;
        }

        public static List<SetOfScans> MergeBoxScans(List<SetOfScans> setOfScans, List<BoxcarRange>[] boxcarRanges)
        {
            boxcarRanges = RemoveOverlap(boxcarRanges);
            Parallel.ForEach(setOfScans, set =>
            {
                set.MergedBoxScan = SetOfScans.MergeBoxScans(set.BoxcarScans, boxcarRanges);
            });

            return setOfScans;

        }

        public static List<MsDataScan> OutputMergedBoxScans(List<SetOfScans> setOfScans)
        {
            List<MsDataScan> scans = new List<MsDataScan>();
            int oneBasedScanNumber = 1;
            foreach (var set in setOfScans)
            {
                scans.Add(set.Ms1scans.First());
                oneBasedScanNumber++;
                if (set.MergedBoxScan != null)
                {
                    set.MergedBoxScan.SetOneBasedScanNumber(oneBasedScanNumber);
                    scans.Add(set.MergedBoxScan);
                    oneBasedScanNumber++;
                }
                if (set.Ms2scans.Count != 0)
                {
                    foreach (var ms2scan in set.Ms2scans)
                    {
                        ms2scan.SetOneBasedScanNumber(oneBasedScanNumber);
                        ms2scan.SetOneBasedPrecursorScanNumber(set.MergedBoxScan.OneBasedScanNumber);
                        scans.Add(ms2scan);
                        oneBasedScanNumber++;
                    }
                }
            }

            return scans;
        }

        public static List<MsDataScan> OutputScans_ExceptFullScan(List<SetOfScans> setOfScans)
        {
            List<MsDataScan> scans = new List<MsDataScan>();
            int oneBasedScanNumber = 1;
            foreach (var set in setOfScans)
            {
                if (set.MergedBoxScan != null)
                {
                    set.MergedBoxScan.SetOneBasedScanNumber(oneBasedScanNumber);
                    scans.Add(set.MergedBoxScan);
                    oneBasedScanNumber++;
                }
                if (set.Ms2scans.Count != 0)
                {
                    foreach (var ms2scan in set.Ms2scans)
                    {
                        ms2scan.SetOneBasedScanNumber(oneBasedScanNumber);
                        ms2scan.SetOneBasedPrecursorScanNumber(set.MergedBoxScan.OneBasedScanNumber);
                        scans.Add(ms2scan);
                        oneBasedScanNumber++;
                    }
                }
            }

            return scans;
        }

        public static List<MsDataScan> OutputScans_ExceptBoxScan(List<SetOfScans> setOfScans)
        {
            List<MsDataScan> scans = new List<MsDataScan>();
            int oneBasedScanNumber = 1;
            foreach (var set in setOfScans)
            {
                scans.Add(set.Ms1scans.First());
                oneBasedScanNumber++;
                if (set.Ms2scans.Count != 0)
                {
                    foreach (var ms2scan in set.Ms2scans)
                    {
                        ms2scan.SetOneBasedScanNumber(oneBasedScanNumber);
                        ms2scan.SetOneBasedPrecursorScanNumber(set.Ms1scans.First().OneBasedScanNumber);
                        scans.Add(ms2scan);
                        oneBasedScanNumber++;
                    }
                }
            }

            return scans;
        }
    }
}


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
        public Accumulator accumulator = new Accumulator();

        public BoxMerger boxMerger = new BoxMerger();

        public Thanos()
        {
            MsDataFilePaths = new List<string>();
            spectraFileManager = new MyFileManager(true);
        }

        public List<string> MsDataFilePaths { get; set; }

        public MyFileManager spectraFileManager { get; set; }

        //Accumulate intensities for boxcar range decision.
        public void Accumulate()
        {
            accumulator.AllFilesForBoxCar(300, 1650, 200, MsDataFilePaths, spectraFileManager);
        }

        public void MergeBoxCarScan()
        {
            boxMerger.MergeBoxScans(MsDataFilePaths, spectraFileManager);
        }

    }
}

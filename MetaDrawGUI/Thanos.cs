﻿using System;
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
    public class Thanos:INotifyPropertyChanged
    {
        public Accumulator accumulator = new Accumulator();

        public BoxMerger boxMerger = new BoxMerger();

        public Accountant accountant = new Accountant();

        public Sweetor sweetor = new Sweetor();

        public List<SimplePsm> simplePsms = new List<SimplePsm>();

        public Thanos()
        {
            MsDataFilePaths = new List<string>();
            spectraFileManager = new MyFileManager(true);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public List<string> MsDataFilePaths { get; set; }

        public List<string> ResultFilePaths { get; set; }

        public MyFileManager spectraFileManager { get; set; }

        public PsmAnnotationViewModel psmAnnotationViewModel { get; set; }

        //Accumulate intensities for boxcar range decision.
        public void Accumulate()
        {
            accumulator.AllFilesForBoxCar(300, 1650, 200, MsDataFilePaths, spectraFileManager);
        }

        public void MergeBoxCarScan()
        {
            boxMerger.MergeBoxScans(MsDataFilePaths, spectraFileManager);
        }

        public void ExtractScanInfor()
        {
            accountant.ExtractScanNumTime(MsDataFilePaths, spectraFileManager, new Tuple<double, double>(45, 115));
        }

        public void WritePGlycoResult()
        {
            sweetor.WritePGlycoResult(ResultFilePaths, simplePsms);
        }

        public void PlotGlycoFamily()
        {
            psmAnnotationViewModel.privateModel = sweetor.PlotGlycoRT(simplePsms);
            NotifyPropertyChanged("PsmAnnoModel");
        }

        public void ExtractGlycoScanInfor()
        {
            accountant.ExtractScanInfo_Glyco(MsDataFilePaths, spectraFileManager, new Tuple<double, double>(0, 90));
        }
    }
}

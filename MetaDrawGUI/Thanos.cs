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
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace MetaDrawGUI
{
    public class Thanos:INotifyPropertyChanged
    {
        //Thanos' subordinate
        public Accumulator accumulator = new Accumulator();

        public BoxMerger boxMerger = new BoxMerger();

        public Accountant accountant = new Accountant();

        public Sweetor sweetor = new Sweetor();

        public Deconvolutor deconvolutor = new Deconvolutor();        

        public MsDataFileDecon msDataFileDecon = new MsDataFileDecon(); //For charge decovolution

        //Thanos' control setting
        public CommonParameters CommonParameters = new CommonParameters();

        public DeconvolutionParameter DeconvolutionParameter = new DeconvolutionParameter();

        //Thanos' resource
        public List<SimplePsm> simplePsms = new List<SimplePsm>();

        public List<MsFeature> msFeatures = new List<MsFeature>();

        public List<HashSet<MsFeature>> familyFeatures = new List<HashSet<MsFeature>>();       

        public Thanos()
        {
            MsDataFilePaths = new List<string>();
            ResultFilePaths = new List<string>();
            spectraFileManager = new MyFileManager(true);

            deconvolutor._thanos = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        //Thanos' resource

        public List<string> MsDataFilePaths { get; set; }

        public List<string> ResultFilePaths { get; set; }

        public MyFileManager spectraFileManager { get; set; }

        public List<MsDataScan> msDataScans { get; set; }

        public MsDataScan msDataScan { get; set; }

        public PsmAnnotationViewModel psmAnnotationViewModel{ get; set; }

        public PlotModel PsmAnnoModel
        {
            get
            {
                return psmAnnotationViewModel.privateModel;
            }
            set
            {
                psmAnnotationViewModel.privateModel = value;
                NotifyPropertyChanged("PsmAnnoModel");
            }
        }

        //Thanos' action
        //Accumulate intensities for boxcar range decision.
        public void Accumulate()
        {
            accumulator.AllFilesForBoxCar(300, 1650, 200, MsDataFilePaths, spectraFileManager);
        }

        public void MergeBoxCarScan()
        {
            boxMerger.MergeBoxScans(MsDataFilePaths, spectraFileManager);
        }

        public void ExtractScanInfo_BoxCar()
        {
            accountant.ExtractBoxCarScanNumTime(MsDataFilePaths, spectraFileManager, new Tuple<double, double>(0, 150));
        }

        public void WritePGlycoResult()
        {
            sweetor.WritePGlycoResult(ResultFilePaths, simplePsms);
        }

        public void PlotGlycoFamily()
        {
            //PsmAnnoModel = sweetor.PlotGlycoRT(simplePsms);
            PsmAnnoModel = sweetor.PlotGlycoRT(simplePsms.Where(p => p.QValue < 0.01).ToList());
        }

        public void ExtractScanInfo_Shotgun()
        {
            accountant.ExtractNumTime_ShotgunScan(MsDataFilePaths, spectraFileManager, new Tuple<double, double>(0, 90));
        }

        public void BuildGlycoFamily()
        {
            familyFeatures = Sweetor.GetGlycoFamilies(msFeatures.ToArray());
            PsmAnnoModel = Sweetor.PlotGlycoFamily(familyFeatures);
        }

        public void ExtractPrecursorInfo()
        {
            accountant.ExtractPrecursorInfo(MsDataFilePaths, spectraFileManager);
        }

        public void ExtractPrecursorInfo_Decon()
        {
            accountant.ExtractPrecursorInfo_Decon(MsDataFilePaths, spectraFileManager);
        }
    }
}

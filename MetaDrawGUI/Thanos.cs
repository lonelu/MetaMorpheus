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
using OxyPlot.Wpf;
using Proteomics;
using Proteomics.Fragmentation;
using Proteomics.ProteolyticDigestion;
using Proteomics.Fragmentation;
using MetaDrawGUI.Crosslink;

namespace MetaDrawGUI
{
    public class Thanos : INotifyPropertyChanged
    {
        //Thanos' subordinate
        public Accumulator accumulator = new Accumulator();

        public Accountant accountant = new Accountant();

        public Sweetor sweetor = new Sweetor();

        public Deconvolutor deconvolutor = new Deconvolutor();

        public CrosslinkHandler crosslinkHandler = new CrosslinkHandler();

        public MsDataFileDecon msDataFileDecon = new MsDataFileDecon(); //For charge decovolution

        //Thanos' control setting
        public ControlParameter ControlParameter = new ControlParameter();

        public CommonParameters CommonParameters = new CommonParameters();

        public DeconvolutionParameter DeconvolutionParameter = new DeconvolutionParameter();

        //Constructor
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
        public List<PsmFromTsv> psms = new List<PsmFromTsv>();

        public List<SimplePsm> simplePsms = new List<SimplePsm>();

        public List<MsFeature> msFeatures = new List<MsFeature>();

        public List<HashSet<MsFeature>> familyFeatures = new List<HashSet<MsFeature>>();

        public List<string> MsDataFilePaths { get; set; }

        public List<string> ResultFilePaths { get; set; }

        public MyFileManager spectraFileManager { get; set; }

        public MsDataFile msDataFile { get; set;}

        public List<MsDataScan> msDataScans { get; set; }

        public MsDataScan msDataScan { get; set; }

        private PsmAnnotationViewModel psmAnnotationViewModel = new PsmAnnotationViewModel();

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
            BoxMerger.MergeBoxScans(MsDataFilePaths, spectraFileManager);
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

        public void ExtractScanInfo()
        {
            accountant.ExtractNumTime(MsDataFilePaths, spectraFileManager, ControlParameter.LCTimeRange);
        }

        public void BuildGlycoFamily()
        {
            familyFeatures = Sweetor.GetGlycoFamilies(msFeatures.ToArray());
            PsmAnnoModel = GlycoViewModel.PlotGlycoFamily(familyFeatures);
        }

        public void ExtractPrecursorInfo()
        {
            accountant.ExtractPrecursorInfo(MsDataFilePaths, spectraFileManager);
        }

        public void ExtractPrecursorInfo_Decon()
        {
            accountant.ExtractPrecursorInfo_Decon(MsDataFilePaths, spectraFileManager);
        }

        public void FixPrecursorBoxCarScan()
        {
            BoxMerger.FixPrecursorAndWriteFile(MsDataFilePaths, spectraFileManager);
        }

        public void SavePNG(PlotModel plotModel)
        {
            string fileName = Path.GetDirectoryName(this.MsDataFilePaths.First()) + "\\" + Path.GetFileNameWithoutExtension(this.MsDataFilePaths.First()) + "_" + plotModel.Title + "_.png";
            PngExporter.Export(plotModel, fileName, 1200, 800, OxyColors.White);
        }

        public PeptideWithSetModifications PeptideFromInput(string baseSeq, List<(int, string, double)> mods)
        {
            Dictionary<int, Modification> allModsOneIsNterminus = new Dictionary<int, Modification>();
            foreach (var mod in mods)
            {
                ModificationMotif.TryGetMotif(mod.Item2, out ModificationMotif motif);
                Modification modification = new Modification(_originalId: "mod", _modificationType: "myModType", _target: motif, _locationRestriction: "Anywhere.", _monoisotopicMass: mod.Item3);
                allModsOneIsNterminus.Add(mod.Item1, modification);
            }

            Protein protein = new Protein(baseSeq, "prot");

            PeptideWithSetModifications pwsm = new PeptideWithSetModifications(protein, new DigestionParams(), 1, baseSeq.Length, CleavageSpecificity.Unknown, null, 0, allModsOneIsNterminus, 0);

            return pwsm;
        }

        public List<IsoEnvelop> DoPeptideSpectrumMatch(PeptideWithSetModifications pep)
        {
            List<Product> peptideTheorProducts = pep.Fragment(CommonParameters.DissociationType, FragmentationTerminus.Both).ToList();

            //Ms2ScanWithSpecificMass scan = new Ms2ScanWithSpecificMass(msDataScan, 4, 1, null, new CommonParameters());

            //List<MatchedFragmentIon> matchedIons = MetaMorpheusEngine.MatchFragmentIons(scan, peptideTheorProducts, CommonParameters);

            MzSpectrumXY mzSpectrumXY = new MzSpectrumXY(this.msDataScan.MassSpectrum.XArray, this.msDataScan.MassSpectrum.YArray, true);

            var matchedIons = M2Scan.MatchFragments(mzSpectrumXY, peptideTheorProducts, DeconvolutionParameter, 50);

            return matchedIons;
        }

    }
}

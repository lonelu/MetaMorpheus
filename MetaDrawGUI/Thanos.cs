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
using UsefulProteomicsDatabases;

namespace MetaDrawGUI
{
    public class Thanos: INotifyPropertyChanged
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
            sweetor._thanos = this;
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

        public List<Protein> Proteins = new List<Protein>();

        public List<MsFeature> msFeatures = new List<MsFeature>();

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


        public void ExtractScanInfo()
        {
            accountant.ExtractNumTime(MsDataFilePaths, spectraFileManager, ControlParameter.LCTimeRange);
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

        public void ModelDrawPdf()
        {
            var plotModel = deconvolutor.XicModel;
            string fileName = Path.GetDirectoryName(this.MsDataFilePaths.First()) + "\\" + Path.GetFileNameWithoutExtension(this.MsDataFilePaths.First()) +  "current.pdf";
            using (var stream = File.Create(fileName))
            {
                PdfExporter pdf = new PdfExporter { Width = 800, Height = 500 };
                pdf.Export(plotModel, stream);
            }
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
            List<Product> peptideTheorProducts = new List<Product>();
            pep.Fragment(CommonParameters.DissociationType, FragmentationTerminus.Both, peptideTheorProducts);

            //Ms2ScanWithSpecificMass scan = new Ms2ScanWithSpecificMass(msDataScan, 4, 1, null, new CommonParameters());

            //List<MatchedFragmentIon> matchedIons = MetaMorpheusEngine.MatchFragmentIons(scan, peptideTheorProducts, CommonParameters);

            MzSpectrumXY mzSpectrumXY = new MzSpectrumXY(this.msDataScan.MassSpectrum.XArray, this.msDataScan.MassSpectrum.YArray, true);

            var matchedIons = M2Scan.MatchFragments(mzSpectrumXY, peptideTheorProducts, DeconvolutionParameter, 50);

            return matchedIons;
        }

        //Copied from MetaMorpheus task due to protection level.
        public List<Protein> LoadProteins(string taskId, List<DbForTask> dbFilenameList, bool searchTarget, DecoyType decoyType, List<string> localizeableModificationTypes, CommonParameters commonParameters)
        {
            int emptyProteinEntries = 0;
            List<Protein> proteinList = new List<Protein>();
            foreach (var db in dbFilenameList)
            {
                var dbProteinList = LoadProteinDb(db.FilePath, searchTarget, decoyType, localizeableModificationTypes, db.IsContaminant, out Dictionary<string, Modification> unknownModifications, out int emptyProteinEntriesForThisDb, commonParameters);
                proteinList = proteinList.Concat(dbProteinList).ToList();
                emptyProteinEntries += emptyProteinEntriesForThisDb;
            }
            return proteinList;
        }

        private static List<Protein> LoadProteinDb(string fileName, bool generateTargets, DecoyType decoyType, List<string> localizeableModificationTypes, bool isContaminant, out Dictionary<string, Modification> um,
    out int emptyEntriesCount, CommonParameters commonParameters)
        {
            List<string> dbErrors = new List<string>();
            List<Protein> proteinList = new List<Protein>();

            string theExtension = Path.GetExtension(fileName).ToLowerInvariant();
            bool compressed = theExtension.EndsWith("gz"); // allows for .bgz and .tgz, too which are used on occasion
            theExtension = compressed ? Path.GetExtension(Path.GetFileNameWithoutExtension(fileName)).ToLowerInvariant() : theExtension;

            if (theExtension.Equals(".fasta") || theExtension.Equals(".fa"))
            {
                um = null;
                proteinList = ProteinDbLoader.LoadProteinFasta(fileName, generateTargets, decoyType, isContaminant, ProteinDbLoader.UniprotAccessionRegex, ProteinDbLoader.UniprotFullNameRegex, ProteinDbLoader.UniprotFullNameRegex, ProteinDbLoader.UniprotGeneNameRegex,
                    ProteinDbLoader.UniprotOrganismRegex, out dbErrors, commonParameters.MaxThreadsToUsePerFile);
            }
            else
            {
                List<string> modTypesToExclude = GlobalVariables.AllModTypesKnown.Where(b => !localizeableModificationTypes.Contains(b)).ToList();
                proteinList = ProteinDbLoader.LoadProteinXML(fileName, generateTargets, decoyType, GlobalVariables.AllModsKnown, isContaminant, modTypesToExclude, out um, commonParameters.MaxThreadsToUsePerFile, commonParameters.MaxHeterozygousVariants, commonParameters.MinVariantDepth);
            }

            emptyEntriesCount = proteinList.Count(p => p.BaseSequence.Length == 0);
            return proteinList.Where(p => p.BaseSequence.Length > 0).ToList();
        }

        public List<PeptideWithSetModifications> GeneratePeptides(CommonParameters commonParameters)
        {
            List<PeptideWithSetModifications> peptides = new List<PeptideWithSetModifications>();


            List<Modification> VariableModifications = GlobalVariables.AllModsKnown.OfType<Modification>().Where(b => commonParameters.ListOfModsVariable.Contains((b.ModificationType, b.IdWithMotif))).ToList();
            List<Modification> FixedModifications = GlobalVariables.AllModsKnown.OfType<Modification>().Where(b => commonParameters.ListOfModsFixed.Contains((b.ModificationType, b.IdWithMotif))).ToList();

            int maxThreadsPerFile = commonParameters.MaxThreadsToUsePerFile;
            int[] threads = Enumerable.Range(0, maxThreadsPerFile).ToArray();
            Parallel.ForEach(threads, (i) =>
            {
                List<PeptideWithSetModifications> localPeptides = new List<PeptideWithSetModifications>();

                for (; i < Proteins.Count; i += maxThreadsPerFile)
                {
                    // Stop loop if canceled
                    if (GlobalVariables.StopLoops) { return; }

                    localPeptides.AddRange(Proteins[i].Digest(commonParameters.DigestionParams, FixedModifications, VariableModifications));
                }

                lock (peptides)
                {
                    peptides.AddRange(localPeptides);
                }
            });

            return peptides;
        }
    }
}

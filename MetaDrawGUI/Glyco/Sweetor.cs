﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ViewModels;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using MzLibUtil;
using TaskLayer;
using EngineLayer;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Proteomics.ProteolyticDigestion;
using Proteomics;
using FlashLFQ;
using MassSpectrometry;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using MathNet.Numerics.LinearRegression;

namespace MetaDrawGUI
{
    public enum SweetorSkill
    {
        PlotGlycoFamily = 0,
        BuildGlycoFamily = 1,
        Write_GlycoResult = 2,
        FilterSemiTrypsinResult = 3, //Filter Semi-tryptic peptides with StcE-tryptic peptides. Byonic only work on semi-trypsin. 
        FilterPariedScan = 4, //In Byonic, PariedScan HCD-EThcD can generate different identifications if search separately.
        Compare_Byonic_MetaMorpheus_EachScan = 5,
        MetaMorpheus_coisolation_Evaluation = 6,
        Compare_Seq_overlap = 7,
        CorrectRT = 8,
        PredictRT = 9
    }

    public class Sweetor:INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        //Glyco
        private ObservableCollection<RawDataForDataGrid> GlycoResultObservableCollection = new ObservableCollection<RawDataForDataGrid>();
        public ObservableCollection<RawDataForDataGrid> GlycoResultCollection
        {
            get
            {
                return GlycoResultObservableCollection;
            }
            set
            {
                GlycoResultObservableCollection = value;
                NotifyPropertyChanged("envolopCollection");
            }
        }

        private  ObservableCollection<GlycoStructureForDataGrid> GlycoStrucureObservableCollection = new ObservableCollection<GlycoStructureForDataGrid>();
        public ObservableCollection<GlycoStructureForDataGrid> GlycoStrucureCollection
        {
            get
            {
                return GlycoStrucureObservableCollection;
            }
            set
            {
                GlycoStrucureObservableCollection = value;
                NotifyPropertyChanged("envolopCollection");
            }
        }

        private ObservableCollection<MsFeatureForDataGrid> MsFeatureObservableCollection = new ObservableCollection<MsFeatureForDataGrid>();
        public ObservableCollection<MsFeatureForDataGrid> MsFeatureCollection
        {
            get
            {
                return MsFeatureObservableCollection;
            }
            set
            {
                MsFeatureObservableCollection = value;
                NotifyPropertyChanged("envolopCollection");
            }
        }

        private ObservableCollection<GlycanDatabaseForDataGrid> glycanDatabaseObervableCollection = new ObservableCollection<GlycanDatabaseForDataGrid>();
        public ObservableCollection<GlycanDatabaseForDataGrid> glycanDatabaseCollection
        {
            get
            {
                return glycanDatabaseObervableCollection;
            }
            set
            {
                glycanDatabaseObervableCollection = value;
                NotifyPropertyChanged("envolopCollection");
            }
        }

        public Thanos _thanos { get; set; }

        public List<Glycan> NGlycans { get; set; }

        public GlycanBox[] OGlycanGroup { get; set; }

        public IOrderedEnumerable<IGrouping<string, SimplePsm>> Psms_byId
        {
            get
            {
                return GetFamilySimplePsm();
            }
        }

        public int GlycoFamilyIndex { get; set; }

        public List<HashSet<MsFeature>> familyFeatures = new List<HashSet<MsFeature>>();

        //Write O-Glycan Group info.
        public void WriteOGlycanGroupResult(string filepath)
        {
            var writtenFile = Path.Combine(Path.GetDirectoryName(filepath), "OGlycanGroup.tsv");
            using (StreamWriter output = new StreamWriter(writtenFile))
            {
                output.WriteLine("TotalMass\tGlycanNumber\tCompostion\tIds");
                foreach (var c in OGlycanGroup)
                {
                    var ids = "[" +  c.GlycanIdString + "]";
                    output.WriteLine((double)c.Mass/100000.0 + "\t" + c.NumberOfMods + "\t" +  ids); //c.Structure + "\t" +
                }
            }
        }

        //Write pGlyco result into out format.

        public IOrderedEnumerable<IGrouping<string, SimplePsm>> GetFamilySimplePsm()
        {
            foreach (var psm in _thanos.simplePsms)
            {
                psm.iD = psm.BaseSeq + "_" + psm.PeptideMassNoGlycan.ToString("0.0");
            }

            var psms_byId = _thanos.simplePsms.Where(p => p.QValue < 0.01 && p.DecoyContamTarget == "T").GroupBy(p => p.iD).OrderByDescending(p => p.Count());

            return psms_byId;
        }

        public void PlotAllGlycoFamily()
        {
            _thanos.PsmAnnoModel = _thanos.sweetor.PlotGlycoRT(_thanos.simplePsms.Where(p => p.QValue < 0.01 && p.DecoyContamTarget == "T").ToList());
        }

        public void PlotGlycoFamily()
        {
            if (GlycoFamilyIndex >= Psms_byId.Count())
            {
                GlycoFamilyIndex = 0;
            }
            else
            {
                _thanos.PsmAnnoModel = _thanos.sweetor.PlotGlycoRT(Psms_byId.ElementAt(GlycoFamilyIndex).ToList());
                GlycoFamilyIndex++;
            }
        }

        public void RelativeRTPrediction()
        {
            List<double[]> x = new List<double[]>();
            List<double> y = new List<double>();

            int typeOfPsms = 0;
            int psmTypeCount = Psms_byId.Where(p=>p.Count() >= 3).Count();
            while (typeOfPsms < psmTypeCount)
            {
                var psms = Psms_byId.ElementAt(typeOfPsms);

                foreach (var psm in psms)
                {
                    double[] _x = new double[psmTypeCount + 5];
                    _x[typeOfPsms] = 1;
                    _x[psmTypeCount] = psm.GlycanKind[0];
                    _x[psmTypeCount + 1] = psm.GlycanKind[1];
                    _x[psmTypeCount + 2] = psm.GlycanKind[2];
                    _x[psmTypeCount + 3] = psm.GlycanKind[3];
                    _x[psmTypeCount + 4] = psm.GlycanKind[4];
                    x.Add(_x);
                    y.Add(psm.RT);
                }

                typeOfPsms++;
            }

            double[] p = MultipleRegression.QR(x.ToArray(), y.ToArray(), intercept: false);

            List<double> preY = new List<double>();
            foreach (var _x in x)
            {
                double z = 0;
                for (int i = 0; i < _x.Length; i++)
                {
                    z += p[i] * _x[i];
                }
                preY.Add(z);
            }



            //Output all parameters.
            var filterPath = Path.Combine(Path.GetDirectoryName(_thanos.ResultFilePaths.First()), Path.GetFileNameWithoutExtension(_thanos.ResultFilePaths.First()) + "_RRT.tsv");

            using (StreamWriter output = new StreamWriter(filterPath))
            {
                for (int i = 0; i < x.Count(); i++)
                {
                    string line = "";
                    for (int j = 0; j < x.First().Length; j++)
                    {
                        line += x[i][j] + "\t";
                    }
                    line += y[i] + "\t";
                    line += preY[i];
                    output.WriteLine(line);

                }
            }
        }

        public void BuildGlycoFamily()
        {
            _thanos.sweetor.familyFeatures = GetGlycoFamilies(_thanos.msFeatures.ToArray());
            _thanos.PsmAnnoModel = GlycoViewModel.PlotGlycoFamily(_thanos.sweetor.familyFeatures);
        }

        //To plot identified glycopeptide family mass vs retention time
        public PlotModel PlotGlycoRT(List<SimplePsm> simplePsms)
        {
            //simplePsms = simplePsms.Where(p => p.BaseSeq == "GLFIPFSVSSVTHK").ToList();

            if (simplePsms.Count <= 0)
            {
                var reportModel = new PlotModel { Title = "Glycopeptide family", Subtitle = "no psms" };
                return reportModel;
            }

            OxyColor[] oxyColors = new OxyColor[15] { OxyColor.Parse("#F8766D"), OxyColor.Parse("#E58700"), OxyColor.Parse("#C99800"), OxyColor.Parse("#A3A500"),
                OxyColor.Parse("#6BB100"),OxyColor.Parse("#00BA38"), OxyColor.Parse("#00BF7D"), OxyColor.Parse("#00C0AF"), OxyColor.Parse("#00BCD8"), OxyColor.Parse("#00B0F6"),
                OxyColor.Parse("#619CFF"), OxyColor.Parse("#B983FF"), OxyColor.Parse("#E76BF3"), OxyColor.Parse("#FD61D1"), OxyColor.Parse("#FF67A4")};

            var largestRT = simplePsms.Max(p => p.RT) * 1.2;
            var leastMass = simplePsms.Min(p => p.MonoisotopicMass) * 0.8;
            var largestMass = simplePsms.Max(p => p.MonoisotopicMass) * 1.2;

            var model = new PlotModel { Title = "Glycopeptide family", Subtitle = "using OxyPlot" };


            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "RT",
                Minimum = 0,
                Maximum = largestRT,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = largestRT
            });
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Monoisotopic Mass",
                Minimum = leastMass,
                Maximum = largestMass,
                AbsoluteMinimum = leastMass,
                AbsoluteMaximum = largestMass
            });

            foreach (var psm in simplePsms)
            {
                psm.iD = psm.BaseSeq + "_" + psm.PeptideMassNoGlycan.ToString("0.0");
            }

            var psms_byId = simplePsms.GroupBy(p => p.iD);

            Random rand = new Random();
            foreach (var id_psms in psms_byId)
            {
                List<DataPoint> dataPoints = new List<DataPoint>();
                int colorId = rand.Next(0, 14);

                foreach (var psm in id_psms.OrderBy(p => p.GlycanAGNumber).ThenBy(p => p.MonoisotopicMass))
                {
                    dataPoints.Add(new DataPoint(psm.RT, psm.MonoisotopicMass));

                    var peakAnnotation = new TextAnnotation();
                    peakAnnotation.Font = "Arial";
                    peakAnnotation.FontSize = 8;
                    peakAnnotation.FontWeight = 1.5;
                    peakAnnotation.TextColor = oxyColors[colorId];
                    peakAnnotation.StrokeThickness = 0;
                    peakAnnotation.Text = psm.GlycanComposition;
                    peakAnnotation.TextPosition = new DataPoint(psm.RT, psm.MonoisotopicMass);
                    peakAnnotation.TextHorizontalAlignment = HorizontalAlignment.Left;
                    model.Annotations.Add(peakAnnotation);
                }

                var line = new LineSeries();
                line.Color = oxyColors[colorId];
                line.MarkerType = MarkerType.Circle;
                line.MarkerFill = oxyColors[colorId];
                line.StrokeThickness = 1.5;
                line.Points.AddRange(dataPoints);
                model.Series.Add(line);
            }

            return model;
        }

        //TO DO: not finished
        public void extractGsmInfo(List<SimplePsm> simplePsms)
        {
            var psms = simplePsms.ToDictionary(p => p.FullSeq, p=>p).Select(p=>p.Value).ToList();

            foreach (var psm in simplePsms)
            {
                psm.iD = psm.BaseSeq + "_" + psm.PeptideMassNoGlycan.ToString("0.0");
            }

            var psms_byId = simplePsms.GroupBy(p => p.iD);

            foreach (var id_psms in psms_byId)
            {

                var psms_byAG = id_psms.GroupBy(p => p.GlycanAGNumber);

                foreach (var ag_psms in psms_byAG)
                {
                    foreach (var psm in ag_psms.OrderBy(p => p.MonoisotopicMass))
                    {

                    }

                }
            }
        }

        #region Build Glycofamily based on features.
        //For simplicity, the A 291.09542 may not be considered.
        static double[] SugarMass = new double[10] { -406.15874, -365.13219, -203.07937, -162.05282, -146.05791, 146.05791, 162.05282, 203.07937, 365.13219, 406.15874 };
        static Tolerance tolerance = new PpmTolerance(10);

        public static List<HashSet<MsFeature>> GetGlycoFamilies(MsFeature[] msFeatures)
        {
            msFeatures = msFeatures.OrderBy(p => p.ApexRT).ToArray();
            for (int i = 0; i < msFeatures.Length; i++)
            {
                msFeatures[i].id = i;
            }

            HashSet<MsFeature>[] extractedFeatures = new HashSet<MsFeature>[msFeatures.Length];
            Object locker = new object();
            Parallel.For(0, msFeatures.Length, ind => {
                MsFeature[] allFeatures = new MsFeature[msFeatures.Length];
                lock (locker)
                {
                    Array.Copy(msFeatures, allFeatures, msFeatures.Length);
                }
                var featuresWithInRange = allFeatures.Where(p => p.ApexRT < msFeatures[ind].ApexRT + 0.5 && p.ApexRT > msFeatures[ind].ApexRT - 0.5).OrderBy(p => p.MonoMass).ToArray();

                extractedFeatures[ind] = ExtractGlycoMS1features(msFeatures[ind], featuresWithInRange);
            });

            int[] mark = new int[msFeatures.Length];

            for (int i = 0; i < msFeatures.Length; i++)
            {
                if (extractedFeatures[i].Count() == 0)
                {
                    mark[i] = 0;
                }
                else
                {
                    mark[i] = 1;
                }
            }

            List<HashSet<MsFeature>> allFamilies = new List<HashSet<MsFeature>>();

            for (int i = 0; i < msFeatures.Length; i++)
            {
                if (mark[i] == 1)
                {
                    HashSet<MsFeature> thisFamily = new HashSet<MsFeature>();
                    FamilyFeatures(i, extractedFeatures, mark, ref thisFamily);
                    allFamilies.Add(thisFamily);
                }
            }

            return allFamilies;
        }

        //To use this function, the input neuCodeIsotopicEnvelops has to be ordered already by monoisotopicMass
        private static HashSet<MsFeature> ExtractGlycoMS1features(MsFeature theFeature, MsFeature[] msFeatures)
        {
            HashSet<MsFeature> glycanCandidates = new HashSet<MsFeature>();

            if (msFeatures.Length == 0)
            {
                return glycanCandidates;
            }

            var masses = msFeatures.Select(p => p.MonoMass).ToArray();

            var families = SugarMass.Select(p => theFeature.MonoMass + p).ToArray();

            List<int> matchedInd = new List<int>();

            foreach (var fm in families)
            {
                var ind = Array.BinarySearch(masses, fm);
                if (ind < 0)
                {
                    ind = ~ind;
                }

                if (ind < masses.Length && tolerance.Within(fm, masses[ind]))
                {
                    matchedInd.Add(ind);
                }
                else if (ind > 0 && tolerance.Within(fm, masses[ind - 1]))
                {
                    matchedInd.Add(ind - 1);
                }
            }

            if (matchedInd.Count() >= 2)
            {
                glycanCandidates.Add(theFeature);
                foreach (var m in matchedInd)
                {
                    if (!glycanCandidates.Contains(msFeatures[m]))
                    {
                        glycanCandidates.Add(msFeatures[m]);

                    }
                }
            }


            return glycanCandidates;
        }

        private static void FamilyFeatures(int index, HashSet<MsFeature>[] extractedFeatures, int[] mark, ref HashSet<MsFeature> thisFamily)
        {
            if (mark[index] == 1)
            {
                thisFamily.Add(extractedFeatures[index].First());
                mark[index]--;
                foreach (var feature in extractedFeatures[index])
                {
                    if (mark[feature.id] == 1)
                    {
                        FamilyFeatures(feature.id, extractedFeatures, mark, ref thisFamily);
                    }
                    else
                    {
                        thisFamily.Add(feature);
                    }
                }
            }
        }

        //Extract precursor info with deconvolution from all ms2Scans. Calculate oxinium-ion-containing-ms2sancs.
        //TO DO: not finished
        public void DeconPrecursorGlycoFamily(List<string> MsDataFilePaths, MyFileManager spectraFileManager)
        {
            MassDiffAcceptor massDiffAcceptor_oxiniumIons = new SinglePpmAroundZeroSearchMode(5);
            foreach (var filePath in MsDataFilePaths)
            {
                var msDataFile = spectraFileManager.LoadFile(filePath, new CommonParameters());
                var commonPara = new CommonParameters(deconvolutionMaxAssumedChargeState: 6);
                var scans = MetaMorpheusTask.GetMs2Scans(msDataFile, null, commonPara).Where(p => p.PrecursorCharge > 1).ToArray();

                MsFeature[] msFeatures = new MsFeature[scans.Length];
                for (int i = 0; i < scans.Length; i++)
                {
                    var oxi = EngineLayer.GlycoSearch.GlycoPeptides.ScanOxoniumIonFilter(scans[i], massDiffAcceptor_oxiniumIons, MassSpectrometry.DissociationType.HCD);
                    msFeatures[i] = new MsFeature(i, scans[i].PrecursorMass, scans[i].TotalIonCurrent, scans[i].RetentionTime);
                    msFeatures[i].ContainOxiniumIon = EngineLayer.GlycoSearch.GlycoPeptides.OxoniumIonsAnalysis(oxi, GlycanBox.OGlycanBoxes.First());
                }

                var glycoFamily = GetGlycoFamilies(msFeatures);

            }
        }

        #endregion
        public void Write_GlycoResult()
        {

            foreach (var filepath in _thanos.ResultFilePaths)
            {
                var ForderPath = Path.Combine(Path.GetDirectoryName(filepath), Path.GetFileNameWithoutExtension(filepath) + "_Glyco.mytsv");

                TsvReader_Id.WriteTsv(ForderPath, _thanos.simplePsms, "Glyco");
            }
        }

        public void FilterSemiTrypsinResult()
        {

            var digestPara = new DigestionParams(
               minPeptideLength: 5,
               maxPeptideLength: 60,
               protease: "StcE-trypsin",
               maxMissedCleavages: 5
           );

           var  commonParameters = new CommonParameters(
                digestionParams: digestPara
            );

            var theoryPeptides = _thanos.GeneratePeptides(commonParameters);

            HashSet<string> peptideHash = theoryPeptides.Select(p => p.BaseSequence).ToHashSet();

            List<Tuple<string, bool>> filter = new List<Tuple<string, bool>>();

            foreach (var psm in _thanos.simplePsms)
            {
                if (peptideHash.Contains(psm.BaseSeq))
                {
                    filter.Add(new Tuple<string, bool>(psm.BaseSeq, true));
                }
                else
                {
                    filter.Add(new Tuple<string, bool>(psm.BaseSeq, false));
                }              
            }

            var filterPath = Path.Combine(Path.GetDirectoryName(_thanos.ResultFilePaths.First()), Path.GetFileNameWithoutExtension(_thanos.ResultFilePaths.First())+ "_filter.mytsv");

            using (StreamWriter output = new StreamWriter(filterPath))
            {
                output.WriteLine("In DataBase");
                foreach (var t in filter)
                {
                    output.WriteLine(t.Item1 + "\t" + t.Item2);
                }
            }
        }

        public void FilterPariedScan()
        {
            //<bool, bool>: <Has pairedScan, same Base sequence>
            List<Tuple<bool, bool>> filter = new List<Tuple<bool, bool>>();

            foreach (var psm in _thanos.simplePsms)
            {
                if (psm.DissociateType == "HCD")
                {
                    if (_thanos.simplePsms.Where(p => p.PrecursorScanNum == psm.Ms2ScanNumber).Count() > 0)
                    {
                        bool same = false;
                        foreach (var c in _thanos.simplePsms.Where(p => p.PrecursorScanNum == psm.Ms2ScanNumber))
                        {
                            if (c.BaseSeq == psm.BaseSeq)
                            {
                                same = true;
                            }
                        }
                        filter.Add(new Tuple<bool, bool>(true, same));
                    }
                    else
                    {
                        filter.Add(new Tuple<bool, bool>(false, false));
                    }
                }
                else
                {
                    if (_thanos.simplePsms.Where(p => p.Ms2ScanNumber == psm.PrecursorScanNum).Count() > 0)
                    {
                        bool same = false;
                        foreach (var c in _thanos.simplePsms.Where(p => p.Ms2ScanNumber == psm.PrecursorScanNum))
                        {
                            if (c.BaseSeq == psm.BaseSeq)
                            {
                                same = true;
                            }
                        }
                        filter.Add(new Tuple<bool, bool>(true, same));
                    }
                    else
                    {
                        filter.Add(new Tuple<bool, bool>(false, false));
                    }
                }

            }

            var filterPath = Path.Combine(Path.GetDirectoryName(_thanos.ResultFilePaths.First()), Path.GetFileNameWithoutExtension(_thanos.ResultFilePaths.First())+ "_PairFilter.mytsv");

            using (StreamWriter output = new StreamWriter(filterPath))
            {
                output.WriteLine("Has pairedScan\tSame Base sequence");
                foreach (var t in filter)
                {
                    output.WriteLine(t.Item1 + "\t" + t.Item2);
                }
            }
        }

        public void Compare_Byonic_MetaMorpheus_EachScan()
        {
            var x = _thanos.simplePsms.GroupBy(p => p.FileName);

            var byonic_result = Pair_Byonic_Scan(x.ElementAt(0).ToList());
            //var byonic_result = x.ElementAt(0).Where(p => p.DecoyContamTarget == "T" && p.QValue <= 0.01).ToList();

            var mm_result = x.ElementAt(1).Where(p=>p.DecoyContamTarget == "T" && p.QValue <= 0.01).ToList();

            List<List<SimplePsm>> overlap_byonic_by_mm = new List<List<SimplePsm>>();

            foreach (var b in byonic_result)
            {
                List<SimplePsm> thisId = new List<SimplePsm>();
                thisId.AddRange(byonic_result.Where(p => p.Ms2ScanNumber == b.Ms2ScanNumber));
                thisId.AddRange(mm_result.Where(p => p.Ms2ScanNumber == b.Ms2ScanNumber));

                overlap_byonic_by_mm.Add(thisId);
            }


            var filterPath_byonic_by_mm = Path.Combine(Path.GetDirectoryName(_thanos.ResultFilePaths.First()), Path.GetFileNameWithoutExtension(_thanos.ResultFilePaths.First()) + "_Scan_overlap_byonic_by_mm.tsv");

            using (StreamWriter output = new StreamWriter(filterPath_byonic_by_mm))
            {
                foreach (var os in overlap_byonic_by_mm)
                {
                    string line = "";
                    foreach (var o in os)
                    {
                        line += o.Ms2ScanNumber + "\t" + o.BaseSeq + "\t" + o.FullSeq + "\t" + o.GlycanComposition + "\t";
                    }
                    output.WriteLine(line);
                }
            }

            List<List<SimplePsm>> overlap_mm_by_byonic = new List<List<SimplePsm>>();

            foreach (var m in mm_result)
            {
                List<SimplePsm> thisId = new List<SimplePsm>();
                thisId.AddRange(mm_result.Where(p => p.Ms2ScanNumber == m.Ms2ScanNumber));
                thisId.AddRange(byonic_result.Where(p => p.Ms2ScanNumber == m.Ms2ScanNumber));

                overlap_mm_by_byonic.Add(thisId);
            }


            var filterPath_mm_by_byonic = Path.Combine(Path.GetDirectoryName(_thanos.ResultFilePaths.First()), Path.GetFileNameWithoutExtension(_thanos.ResultFilePaths.First()) + "_Scan_overlap_mm_by_byonic.tsv");

            using (StreamWriter output = new StreamWriter(filterPath_mm_by_byonic))
            {
                foreach (var os in overlap_mm_by_byonic)
                {
                    string line = "";
                    foreach (var o in os)
                    {
                        line += o.Ms2ScanNumber + "\t" + o.BaseSeq + "\t" + o.FullSeq + "\t" + o.GlycanComposition + "\t";
                    }
                    output.WriteLine(line);
                }
            }

        }

        private List<SimplePsm> Pair_Byonic_Scan(List<SimplePsm> simplePsms)
        {           
            List<SimplePsm> paired_psms = new List<SimplePsm>();

            foreach (var psm in simplePsms)
            {
                if (psm.DissociateType == "HCD")
                {
                    foreach (var c in simplePsms.Where(p => p.PrecursorScanNum == psm.Ms2ScanNumber && p.DissociateType == "ETD"))
                    {
                        psm.PairedPsm = c;
                    }

                    paired_psms.Add(psm);
                }
                else
                {
                    if (simplePsms.Where(p => p.Ms2ScanNumber == psm.PrecursorScanNum).Count() == 0)
                    {                        
                        //psm.Ms2ScanNumber = psm.PrecursorScanNum; //ETD scan number to HCD scan number.
                        paired_psms.Add(psm);
                    }
                }

            }

            foreach (var pp in paired_psms)
            {
                if (pp.DissociateType == "ETD")
                {
                    pp.Ms2ScanNumber = pp.PrecursorScanNum;
                }
            }

            return paired_psms;
        }

        public void MetaMorpheus_coisolation_Evaluation()
        {
            var psms = _thanos.simplePsms.Where(p => p.DecoyContamTarget == "T" && p.QValue <= 0.01).GroupBy(p=>p.Ms2ScanNumber).ToList();

            var filterPath = Path.Combine(Path.GetDirectoryName(_thanos.ResultFilePaths.First()), Path.GetFileNameWithoutExtension(_thanos.ResultFilePaths.First()) + "_MetaMorpheusCoiso.tsv");

            using (StreamWriter output = new StreamWriter(filterPath))
            {
                foreach (var os in psms)
                {
                    string line = "";
                    foreach (var o in os)
                    {
                        line += o.Ms2ScanNumber + "\t" + o.BaseSeq + "\t" + o.GlycanComposition + "\t";
                    }
                    output.WriteLine(line);
                }
            }


        }

        public void Compare_Seq_overlap()
        {
            var x = _thanos.simplePsms.GroupBy(p => p.FileName);

            var byonic_result = x.ElementAt(0).ToList();

            var mm_result = x.ElementAt(1).Where(p=>p.DecoyContamTarget == "T" && p.QValue <= 0.01).ToList();

            List<bool> byonic_by_mm = new List<bool>();

            foreach (var b in byonic_result)
            {
                //if (mm_result.Where(p=>p.BaseSeq == b.BaseSeq && p.GlycanComposition == b.GlycanComposition).Count() > 0)
                if (mm_result.Where(p => p.BaseSeq == b.BaseSeq).Count() > 0)
                {
                    byonic_by_mm.Add(true);
                }
                else
                {
                    byonic_by_mm.Add(false);
                }
            }

            var filterPath = Path.Combine(Path.GetDirectoryName(_thanos.ResultFilePaths.First()), Path.GetFileNameWithoutExtension(_thanos.ResultFilePaths.First()) + "_byonic_by_mm_Seq_Overlap.tsv");

            using (StreamWriter output = new StreamWriter(filterPath))
            {
                for (int i = 0; i < byonic_result.Count; i++)
                {
                    string line = "";
                    line += byonic_result[i].Ms2ScanNumber + "\t" + byonic_result[i].BaseSeq + "\t" + byonic_result[i].GlycanComposition + "\t" + byonic_by_mm[i];
                    output.WriteLine(line);
                }
            }

            List<bool> mm_by_byonic = new List<bool>();

            foreach (var m in mm_result)
            {
                //if (byonic_result.Where(p => p.BaseSeq == m.BaseSeq && p.GlycanComposition == m.GlycanComposition).Count() > 0)
                if (byonic_result.Where(p => p.BaseSeq == m.BaseSeq).Count() > 0)
                {
                    mm_by_byonic.Add(true);
                }
                else
                {
                    mm_by_byonic.Add(false);
                }
            }


            filterPath = Path.Combine(Path.GetDirectoryName(_thanos.ResultFilePaths.First()), Path.GetFileNameWithoutExtension(_thanos.ResultFilePaths.First()) + "_mm_by_byonic_Seq_Overlap.tsv");

            using (StreamWriter output = new StreamWriter(filterPath))
            {
                for (int i = 0; i < mm_result.Count; i++)
                {
                    string line = "";
                    line += mm_result[i].Ms2ScanNumber + "\t" + mm_result[i].BaseSeq + "\t" + mm_result[i].GlycanComposition + "\t" + mm_by_byonic[i];
                    output.WriteLine(line);
                }
            }


        }

        public void CorrectRT()
        {
            var ms1ScanForDecon = new List<MsDataScan>();
            foreach (var scan in _thanos.msDataScans.Where(p => p.MsnOrder == 1))
            {         
                ms1ScanForDecon.Add(scan);
            }
            QuantFile(_thanos.simplePsms, ms1ScanForDecon, _thanos.MsDataFilePaths.First(), _thanos.CommonParameters);
        }

        public void QuantFile(List<SimplePsm> simplePsms, List<MsDataScan> ms1DataScanList, string filePath, CommonParameters commonParameters)
        {
            SpectraFileInfo mzml = new SpectraFileInfo(filePath, "", 0, 0, 0);

            List<Identification> ids = new List<Identification>();
            for (int scanIndex = 0; scanIndex < simplePsms.Count; scanIndex++)
            {
                var id = new Identification(mzml, simplePsms[scanIndex].BaseSeq, simplePsms[scanIndex].FullSeq, simplePsms[scanIndex].MonoisotopicMass, simplePsms[scanIndex].RT, simplePsms[scanIndex].ChargeState, new List<FlashLFQ.ProteinGroup>(), useForProteinQuant: false);
                ids.Add(id);
            }

            FlashLfqEngine engine = new FlashLfqEngine(ids, integrate:true, ppmTolerance: 5, isotopeTolerancePpm: 3);
            var results = engine.Run();
            var peaks = results.Peaks.SelectMany(p => p.Value).ToList();
            //MsDataFileDecon.WritePeakResults(Path.Combine(Path.GetDirectoryName(filePath), @"Peaks.tsv"), peaks);

            foreach (var psm in simplePsms)
            {
                var apex = peaks.Where(p => p.Identifications.Select(x => x.ModifiedSequence).Contains(psm.FullSeq)).First().Apex;
                if (apex!=null)
                {
                    psm.CorrectedRT = apex.IndexedPeak.RetentionTime;
                }
            }


            Write_GlycoResult();
        }
    }
}

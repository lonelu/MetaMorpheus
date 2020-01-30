using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EngineLayer;
using System.ComponentModel;
using System.Collections.ObjectModel;
using OxyPlot;
using ViewModels;

namespace MetaDrawGUI.Crosslink
{
    public class CrosslinkHandler:INotifyPropertyChanged
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

        //Isotopic deconvolution envolop Data Grid
        private ObservableCollection<SpectrumForDataGrid> IncorrectCsmsObservableCollection = new ObservableCollection<SpectrumForDataGrid>();
        public ObservableCollection<SpectrumForDataGrid> IncorrectCsmsCollection
        {
            get
            {
                return IncorrectCsmsObservableCollection;
            }
            set
            {
                IncorrectCsmsObservableCollection = value;
                NotifyPropertyChanged("IncorrectCsmsCollection");
            }
        }

        private PsmAnnotationViewModel crosslinkModel = new PsmAnnotationViewModel();

        public PlotModel CrosslinkModel
        {
            get
            {
                return crosslinkModel.privateModel;
            }
            set
            {
                crosslinkModel.privateModel = value;
                NotifyPropertyChanged("CrosslinkModel");
            }
        }

        public static List<SimplePsm> validateIncorrectCrosslinks(SimplePsm[] psms_filtered, double fdr)
        {
            List<SimplePsm> incorrectCsms = new List<SimplePsm>();
            for (int i = 0; i < psms_filtered.Length; i++)
            {
                
                if (SyntheticLibrary.TheoryCrosslinks.Keys.Contains(psms_filtered[i].BaseSeq + "_" + psms_filtered[i].BetaPeptideBaseSequence) ||
                    SyntheticLibrary.TheoryCrosslinks.Keys.Contains(psms_filtered[i].BetaPeptideBaseSequence + "_" + psms_filtered[i].BaseSeq))
                {
                    continue;
                }

                incorrectCsms.Add(psms_filtered[i]);
            }

            return incorrectCsms;
        }

        public static List<SimplePsm> Csms2Crosslinks(SimplePsm[] psms)
        {
            HashSet<string> seen = new HashSet<string>();
            List<SimplePsm> crosslinks = new List<SimplePsm>();

            foreach (var csm in psms)
            {
                if (seen.Contains(csm.BaseSeq + "_" + csm.BetaPeptideBaseSequence) ||
                    seen.Contains(csm.BetaPeptideBaseSequence + "_" + csm.BaseSeq))
                {
                    continue;
                }

                seen.Add(csm.BaseSeq + "_" + csm.BetaPeptideBaseSequence);
                crosslinks.Add(csm);
            }

            return crosslinks;
        }

        public static string ValidateOutput(List<SimplePsm> psms, double fdr)
        {
            string output = Validate(psms, fdr);

            if (psms.GroupBy(p => p.FileName).Count() > 1)
            {
                var groups = psms.GroupBy(p => p.FileName).ToList();

                int index = 1;
                foreach (var g in groups)
                {
                    output += "R" + index.ToString() + ": \r";
                    output += Validate(g.ToList(), fdr);
                    index++;
                }
            }
            return output;
        }

        private static string Validate(List<SimplePsm> psms, double fdr)
        {
            string output = "";

            var psms_filtered = psms.Where(p => p.DecoyContamTarget == "T" && p.QValue <= fdr).ToArray();
            var total = psms_filtered.Length;
            var incorrect = validateIncorrectCrosslinks(psms_filtered, fdr).Count();

            output += "    Total Csms: " + total.ToString() + "\r";
            output += "    Incorrect Csms: " + incorrect.ToString() + "\r";
            output += "    Valid Fdr: " + ((double)incorrect / total).ToString("0.0000") + "\r";

            var crosslinks = Csms2Crosslinks(psms_filtered);
            var total_crosslink = crosslinks.Count();
            var incorrect_corsslink = validateIncorrectCrosslinks(crosslinks.ToArray(), fdr).Count;

            output += "    Total Crosslinks: " + total_crosslink.ToString() + "\r";
            output += "    Incorrect Crosslinks: " + incorrect_corsslink.ToString() + "\r";
            output += "    Valid Crosslinks Fdr: " + ((double)incorrect_corsslink / total_crosslink).ToString("0.0000") + "\r";

            output += "\r";

            var pep_psms_filtered = psms.Where(p => p.DecoyContamTarget == "T" && p.PEP_QValue <= fdr).ToArray();
            var pep_total = pep_psms_filtered.Length;
            var pep_incorrect = validateIncorrectCrosslinks(pep_psms_filtered, fdr).Count();

            output += "    PEP_Total Csms: " + pep_total.ToString() + "\r";
            output += "    PEP_Incorrect Csms: " + pep_incorrect.ToString() + "\r";
            output += "    PEP_Valid Fdr: " + ((double)pep_incorrect / pep_total).ToString("0.0000") + "\r";

            var pep_crosslinks = Csms2Crosslinks(pep_psms_filtered);
            var pep_total_crosslink = pep_crosslinks.Count();
            var pep_incorrect_corsslink = validateIncorrectCrosslinks(pep_crosslinks.ToArray(), fdr).Count;

            output += "    PEP_Total Crosslinks: " + pep_total_crosslink.ToString() + "\r";
            output += "    PEP_Incorrect Crosslinks: " + pep_incorrect_corsslink.ToString() + "\r";
            output += "    PEP_Valid Crosslinks Fdr: " + ((double)pep_incorrect_corsslink / pep_total_crosslink).ToString("0.0000") + "\r";

            return output;
        }
    }
}

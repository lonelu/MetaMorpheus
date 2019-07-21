using System;
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

namespace MetaDrawGUI
{
    public class Sweetor
    {
        public void WritePGlycoResult(List<string> ResultFilePaths, List<SimplePsm> simplePsms)
        {
            foreach (var filepath in ResultFilePaths)
            {
                var ForderPath = Path.Combine(Path.GetDirectoryName(filepath), Path.GetFileNameWithoutExtension(filepath), "_pGlyco.mytsv");

                TsvReader_Glyco.WriteTsv(ForderPath, simplePsms.Where(p=>p.FileName == Path.GetFileName(filepath)).ToList());
            }
        }

        public PlotModel PlotGlycoRT(List<SimplePsm> simplePsms)
        {
            foreach (var psm in simplePsms)
            {
                psm.iD = psm.BaseSeq + "_" + psm.Mod;
            }

            var glycoPsms = simplePsms.GroupBy(p=>p.iD);

            var model = new PlotModel { Title = "Glycopeptide family", Subtitle = "using OxyPlot" };

            return model;
        }
    }
}

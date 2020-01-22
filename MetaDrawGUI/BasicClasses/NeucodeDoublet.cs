using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaDrawGUI
{
    public class NeucodeDoublet
    {
        public NeucodeDoublet(FlashLFQ.ChromatographicPeak aPeak, FlashLFQ.ChromatographicPeak bPeak)
        {
            aMass = aPeak.Identifications.First().MonoisotopicMass;
            bMass = bPeak.Identifications.First().MonoisotopicMass;

            aRT = aPeak.Identifications.First().Ms2RetentionTimeInMinutes;
            bRT = bPeak.Identifications.First().Ms2RetentionTimeInMinutes;

            aIntensity = aPeak.Intensity;
            bIntensity = bPeak.Intensity;
        }

        public NeucodeDoublet(MsFeature aPeak, MsFeature bPeak)
        {
            aMass = aPeak.MonoMass;
            bMass = bPeak.MonoMass;

            aRT = aPeak.StartRT;
            bRT = bPeak.StartRT;

            aIntensity = aPeak.Intensity;
            bIntensity = bPeak.Intensity;
        }

        public double aMass { get;  }
        public double bMass { get;  }

        public double aRT { get; }
        public double bRT { get; }

        public double aIntensity { get; }
        public double bIntensity { get; }

        public double IntensityRatio
        {
            get
            {
                if (bIntensity!= 0)
                {
                    return aIntensity / bIntensity;
                }
                return -1;
            }
        }

        public static string TabSeparatedHeader
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append("alpha mass" + "\t");
                sb.Append("beta mass" + "\t");
                sb.Append("alpha RT" + "\t");
                sb.Append("beta RT" + "\t");
                sb.Append("alpha Intensity" + "\t");
                sb.Append("beta Intensity" + "\t");
                sb.Append("Intensity Ratio" + "\t");

                return sb.ToString();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(aMass + "\t");
            sb.Append(bMass + "\t");
            sb.Append(aRT + "\t");
            sb.Append(bRT + "\t");
            sb.Append(aIntensity + "\t");
            sb.Append(bIntensity + "\t");
            sb.Append(IntensityRatio + "\t");
            return sb.ToString();
        }
    }
}

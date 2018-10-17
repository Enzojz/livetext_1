using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;

namespace Livetext
{
    public class FontParams
    {
        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern uint GetKerningPairsW(IntPtr hdc, uint nPairs, [Out] KERNINGPAIR[] pairs);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObject);

        [DllImport("gdi32.dll")]
        static extern bool GetCharABCWidthsFloatW(IntPtr hdc, uint iFirstChar, uint iLastChar, [Out] ABCFLOAT[] lpABCF);

        public struct ABCFLOAT
        {
            public float abcfA;
            public float abcfB;
            public float abcfC;
        }

        public struct KERNINGPAIR
        {
            public ushort wFirst;
            public ushort wSecond;
            public int iKernelAmount;
        }

        public static (KERNINGPAIR[], IEnumerable<(uint, ABCFLOAT)>) GetFontParams(Font font, List<uint> cp)
        {
            using (var graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                graphics.PageUnit = GraphicsUnit.Pixel;
                IntPtr hdc = graphics.GetHdc();
                IntPtr hfont = font.ToHfont();
                KERNINGPAIR[] kern;
                IEnumerable<(uint, ABCFLOAT)> abcWidths;
                IntPtr hObject = SelectObject(hdc, hfont);
                try
                {
                    var kerningPairs = GetKerningPairsW(hdc, 0, null);
                    if (kerningPairs <= 0)
                        kern = new KERNINGPAIR[0];
                    else
                    {
                        kern = new KERNINGPAIR[kerningPairs];
                        var r = GetKerningPairsW(hdc, kerningPairs, kern);
                    }

                    abcWidths = cp.Select(c =>
                    {
                        var w = new ABCFLOAT[1];
                        GetCharABCWidthsFloatW(hdc, c, c, w);
                        return (c, w[0]);
                    }
                    ).ToList();

                    return (kern.OrderBy(k => k.wFirst).ToArray(), abcWidths);
                }
                finally
                {
                    SelectObject(hdc, hObject);
                }
            }
        }

        public List<(uint, float, float, float, List<(ushort, int)>)> ExaminePairs(Font font, List<uint> cp)
        {
            (KERNINGPAIR[] k, IEnumerable<(uint, ABCFLOAT)> w) = GetFontParams(font, cp);
            return w
            .Select(r => (
                r.Item1,
                r.Item2.abcfA,
                r.Item2.abcfB,
                r.Item2.abcfC,
                k.Where(ke => ke.wSecond == r.Item1).Select(ke => (ke.wFirst, ke.iKernelAmount)).ToList()
                )
            )
            .ToList();
        }
    }
}

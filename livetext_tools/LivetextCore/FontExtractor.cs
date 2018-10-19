using Squish;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Livetext
{
    class FontExtractor
    {
        public void generate()
        {
            var cp = Enumerable.Range(0x20, 95)
                .Concat(Enumerable.Range(0x0A1, 431)) // Latin
                .Concat(Enumerable.Range(0x1E00, 256)) // Latin
                //.Concat(Enumerable.Range(0x400, 256)) // Cyrillic
                //.Concat(Enumerable.Range(0x500, 48)) // Cyrillic
                //.Concat(Enumerable.Range(0x370, 144)) // Greek
                .Concat(Enumerable.Range(0x2030, 2)) // per mille
                //.Concat(Enumerable.Range(0x2190, 112)) // Arrows
                //.Concat(Enumerable.Range(0x2900, 128)) // Arrows
                                                       //.Concat(Enumerable.Range(0x3041, 86)) // Hiragana
                                                       //.Concat(Enumerable.Range(0x3099, 7)) // Hiragana
                                                       //.Concat(Enumerable.Range(0x30A0, 112)) // Katakana 

                .Except(new List<int>() { 0x46D, 0x4FD, 0x3D6 })
                .Select(c => checked((uint)c))
                .ToList()
                ;

            if (!Directory.Exists(materialPath + midFix)) Directory.CreateDirectory(materialPath + midFix);
            if (!Directory.Exists(texturesPath + midFix)) Directory.CreateDirectory(texturesPath + midFix);
            if (!Directory.Exists(meshPath + midFix)) Directory.CreateDirectory(meshPath + midFix);
            if (!Directory.Exists(modelPath + midFix)) Directory.CreateDirectory(modelPath + midFix);
            if (!Directory.Exists(scriptsPath + "livetext/")) Directory.CreateDirectory(scriptsPath + "livetext/");

            //cp.ToList().AsParallel()
            //.ForAll(c =>
            //{
            //    Output.drawGlyph(font, midFix, texturesPath, materialPath, c);
            //    Output.generateMesh(midFix, meshPath + midFix, c);
            //    Output.generateModel(midFix, modelPath + midFix, c);
            //}
            //);

            var mat = Output.drawColorTexture(midFix, texturesPath, materialPath, Color.FromArgb(255, 255, 255, 255));
            var trans = Output.drawColorTexture(midFix, texturesPath, materialPath, Color.FromArgb(0, 255, 255, 255));

            cp.ToList()
            .ForEach(c =>
            {
                Output.extractPolygon(mat, trans, meshPath + midFix, font, c);
                //Output.drawGlyph(font, midFix, texturesPath, materialPath, c);
                //Output.generateMesh(midFix, meshPath + midFix, c);
                Output.generateModel(midFix, modelPath + midFix, c);
            }
            );

            Output.generateDescription(new Font(font.FontFamily, 60f, font.Style), cp, scriptsPath + "livetext/" + key + ".lua");
        }

        public FontExtractor(string facename, FontStyle style = FontStyle.Regular)
        {
            font = new Font(facename, 60f, style);
            key = (font.FontFamily.Name + (font.Style == FontStyle.Regular ? "" : ("_" + font.Style.ToString()))).ToLower();

            midFix = "livetext/" + key + "/";
        }

        string midFix;
        static string scriptsPath = "res/scripts/";
        static string texturesPath = "res/textures/";
        static string meshPath = "res/models/mesh/";
        static string materialPath = "res/models/material/";
        static string modelPath = "res/models/model/";

        Font font;
        string key;
    }
}

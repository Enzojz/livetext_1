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
        void drawLetter(uint l)
        {
            Livetext.Output.drawGlyph(font, texturesPath, l);
        }


        void createModel(uint c)
        {
            string bTexturePath = texturesPath.Replace("res/textures/", "");
            string bMaterialPath = materialPath.Replace("res/models/material/", "");
            string bModelPath = modelPath.Replace("res/models/model/", "");

            string mdl =
                @"
function data()
return {
	collider = {
		params = {
			center = { .0, .0, .0 },
			halfExtents = { .0, .0, .0 }
		},
		type = ""BOX""

    },
	lods = {
        {
            animations = {},
            children = {
                {
                    id = """ + bModelPath + c.ToString() + @".msh"",
                    transf = {
                        1, 0, 0, 0, 
                        0, 1, 0, 0, 
                        0, 0, 1, 0, 
                        0, 0, 0, 1,
                    },
                    type = ""MESH"",
                },
            },
            events = {},
            matConfigs = {{0}},
            static = true,
            visibleFrom = 0,
            visibleTo = 2000,
            
        },
	},
	metadata = {
	}	
}
end
";
            Output.generateMesh(bMaterialPath, modelPath, c);
            Output.generateMaterial(materialPath, bTexturePath, c);
            System.IO.File.WriteAllText(modelPath + c.ToString() + ".mdl", mdl);
            //System.IO.File.Copy("31.msh.blob", meshPath + c.ToString() + ".msh.blob", true);
        }

        void createFontDescription(List<uint> cp)
        {
            string output = "";
            var k = new FontParams();
            var ke = k.ExaminePairs(new Font(font.FontFamily, 60f, font.Style), cp);

            output += "local abc = {";
            foreach (var (l, a, b, c, ker) in ke)
            {
                output += String.Format(
                    "[{0}] = {{" +
                    "a = {1}, b = {2}, c = {3}" +
                    "}},\n",
                    l, a, b, c
                    );
            }
            output += "}\n";


            output += "local kern = {";
            foreach (var (l, a, b, c, ker) in ke)
            {
                if (ker.Count > 0)
                {
                    output += "[" + l.ToString() + "] = {";
                    foreach (var (s, kern) in ker)
                        if (cp.Contains(s))
                            output += "[" + s.ToString() + "] = " + kern.ToString() + ",\n";
                    output += "},\n";
                }
            }
            output += "}\n";
            output += "return {abc, kern}";
            System.IO.File.WriteAllText(scriptsPath + key + ".lua", output);
        }

        public void generate()
        {
            var cp = Enumerable.Range(0x20, 95)
                .Concat(Enumerable.Range(0x0A1, 431)) // Latin
                .Concat(Enumerable.Range(0x1E00, 256)) // Latin
                .Concat(Enumerable.Range(0x400, 256)) // Cyrillic
                .Concat(Enumerable.Range(0x500, 48)) // Cyrillic
                .Concat(Enumerable.Range(0x370, 144)) // Greek
                .Concat(Enumerable.Range(0x2030, 2)) // per mille
                .Concat(Enumerable.Range(0x2190, 112)) // Arrows
                .Concat(Enumerable.Range(0x2900, 128)) // Arrows
                                                       //.Concat(Enumerable.Range(0x3041, 86)) // Hiragana
                                                       //.Concat(Enumerable.Range(0x3099, 7)) // Hiragana
                                                       //.Concat(Enumerable.Range(0x30A0, 112)) // Katakana 
                .Select(c => checked((uint)c))
                .ToList()
                ;

            System.IO.Directory.CreateDirectory(scriptsPath);
            System.IO.Directory.CreateDirectory(texturesPath);
            System.IO.Directory.CreateDirectory(meshPath);
            System.IO.Directory.CreateDirectory(modelPath);
            System.IO.Directory.CreateDirectory(materialPath);

            cp.ToList().AsParallel()
            .ForAll(drawLetter);

            cp.ToList()
            .ForEach(createModel);

            createFontDescription(cp);
        }

        public FontExtractor(string facename, FontStyle style = FontStyle.Regular)
        {
            font = new Font(facename, 60f, style);
            key = (font.FontFamily.Name + (font.Style == FontStyle.Regular ? "" : ("_" + font.Style.ToString()))).ToLower();

            scriptsPath = "res/scripts/livetext/";
            texturesPath = "res/textures/models/livetext/" + key + "/";
            meshPath = "res/models/mesh/livetext/" + key + "/";
            materialPath = "res/models/material/livetext/" + key + "/";
            modelPath = "res/models/model/livetext/" + key + "/";
        }

        string scriptsPath;
        string texturesPath;
        string meshPath;
        string materialPath;
        string modelPath;
        Font font;
        string key;
    }
}

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
        [Flags]
        public enum Flags
        {
            Caps = 1,
            Height = 2,
            Width = 4,
            Pitch = 8,
            PixelFormat = 4096,
            MipMapCount = 131072,
            LinearSize = 524288,
            DepthTexture = 8388608
        }

        void drawLetter(uint l)
        {
            var mipmaps =
            new List<int>() { 100, 50, 25 }
            .Select(s =>
            {
                var size = new Size(s, s);
                Bitmap bmp = new Bitmap(size.Width, size.Height);
                Rectangle rect = new Rectangle(new Point(0, 0), size);
                Graphics g = Graphics.FromImage(bmp);
                g.FillRectangle(new SolidBrush(Color.FromArgb(0, 255, 255, 255)), rect);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                g.DrawString(((char)l).ToString(), new Font(font.FontFamily, s * 0.6f, font.Style), new SolidBrush(Color.FromArgb(255, 255, 255, 255)), new PointF(0, 0));
                g.Flush();

                var data = new byte[size.Width * size.Height * 4];
                var bmpdata = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Marshal.Copy(bmpdata.Scan0, data, 0, bmpdata.Stride * bmpdata.Height);
                bmp.UnlockBits(bmpdata);

                for (uint i = 0; i < data.Length - 4; i += 4)
                    (data[i + 2], data[i + 0]) = (data[i + 0], data[i + 2]);

                byte[] dest = new byte[Squish.Squish.GetStorageRequirements(size.Width, size.Height, SquishFlags.kDxt5)];
                Squish.Squish.CompressImage(data, size.Width, size.Height, ref dest, SquishFlags.kDxt5);
                return (Data: dest, size);
            }).ToList();


            using (BinaryWriter bw = new BinaryWriter(new FileStream(texturesPath + l.ToString() + ".dds", FileMode.Create)))
            {
                Flags flags = (Flags.Caps | Flags.Height | Flags.Width | Flags.PixelFormat | Flags.MipMapCount | Flags.LinearSize);

                bw.Write(new byte[] { 0x44, 0x44, 0x53, 0x20 });    // 'DDS '
                bw.Write(124);                                      // dwSize
                bw.Write((int)flags);                               // dwFlags
                bw.Write(100);                                      // dwHeight
                bw.Write(100);                                      // dwWidth
                bw.Write(mipmaps[0].Data.Length);                   // dwPitchOrLinearSize        
                bw.Write(0);                                        // dwDepth
                bw.Write(3);                                        // dwMipMapCount

                for (int i = 0; i < 11; i++) { bw.Write(0); }       // dwReserved1

                bw.Write(32);                                       // ddpfPixelFormat // dwSize

                bw.Write(0x4);                                      // dwFlags
                bw.Write(new byte[] { 0x44, 0x58, 0x54, 0x35 });
                bw.Write(0);
                bw.Write(0);
                bw.Write(0);
                bw.Write(0);
                bw.Write(0);

                bw.Write(0x1000 | 0x400000 | 0x8);
                bw.Write(0);                                        // Caps 2
                bw.Write(0);
                bw.Write(0);

                bw.Write(0);                                        // dwReserved2


                mipmaps.ForEach(m =>
                {
                    bw.Write(m.Data);
                });
            }
        }


        void createModel(uint c)
        {
            string bTexturePath = texturesPath.Replace("res/textures/", "");
            string bMaterialPath = materialPath.Replace("res/models/material/", "");
            string bModelPath = modelPath.Replace("res/models/model/", "");
            string mtl =
                @"
function data() return {
    params = {
        map_color_alpha = {
			compressionAllowed = true,
			fileName = """ + bTexturePath + c.ToString() + @".dds"",
            magFilter = ""LINEAR"",
			minFilter = ""LINEAR_MIPMAP_LINEAR"",
			mipmapAlphaScale = 0,
			type = ""TWOD"",
			wrapS = ""CLAMP_TO_EDGE"",
			wrapT = ""CLAMP_TO_EDGE"",
        },
		polygon_offset = {
			factor = 0,
			units = 0,
		},
		two_sided = {
			twoSided = true,
		},
    },
	type = ""TRANSPARENT"",
}
end";
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
            string msh =
                @"function data() return 
  { animations = {  }, matConfigs = { { 0 } }, 
    subMeshes = 
      { 
        { 
          indices = 
            { 
              normal = { count = 24, offset = 288 }, 
              position = { count = 24, offset = 288 }, 
              tangent = { count = 24, offset = 288 }, 
              uv0 = { count = 24, offset = 288 }, 
             }, 
          materials = { """ + bMaterialPath + c.ToString() + @".mtl"" }
         }
    }, 
    vertexAttr = 
      { 
        normal = 
          { count = 72, numComp = 3, offset = 120 }, 
        position = 
          { count = 72, numComp = 3, offset = 0 }, 
        tangent = 
          { count = 96, numComp = 4, offset = 192 }, 
        uv0 = 
          { count = 48, numComp = 2, offset = 72 }, 
       }
   } end
";
            System.IO.File.WriteAllText(meshPath + c.ToString() + ".msh", msh);
            System.IO.File.WriteAllText(materialPath + c.ToString() + ".mtl", mtl);
            System.IO.File.WriteAllText(modelPath + c.ToString() + ".mdl", mdl);
            System.IO.File.Copy("31.msh.blob", meshPath + c.ToString() + ".msh.blob", true);
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

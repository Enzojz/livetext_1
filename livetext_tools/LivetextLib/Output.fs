
namespace Livetext

open System.Drawing
open System.Drawing.Drawing2D
open System.Drawing.Text
open System.Runtime.InteropServices
open System.IO
open Squish
open Mesh
open Lua

module Output =
    open System.IO
    open System.Numerics

    //[<Flags>]
    type DDSFlags =
    | Caps = 1
    | Height = 2
    | Width = 4
    | Pitch = 8
    | PixelFormat = 4096
    | MipMapCount = 131072
    | LinearSize = 524288
    | DepthTexture = 8388608

    let drawGlyph (font : Font) texturesPath (gl : uint32) =
      let mipmap =
        [100; 50; 25]
        |> List.map(fun s ->
          let size = new Size(s, s)
          let bmp = new Bitmap(size.Width, size.Height)
          let rect = new Rectangle(new Point(0, 0), size)
          let g = Graphics.FromImage(bmp)
          g.SmoothingMode <- SmoothingMode.AntiAlias;
          g.InterpolationMode <- InterpolationMode.HighQualityBicubic;
          g.PixelOffsetMode <- PixelOffsetMode.HighQuality;
          g.TextRenderingHint <- TextRenderingHint.AntiAliasGridFit;

          g.FillRectangle(new SolidBrush(Color.FromArgb(0, 255, 255, 255)), rect);
          g.DrawString (
            (char gl).ToString(),
            new Font(font.FontFamily, 0.6f * float32 s, font.Style),
            new SolidBrush(Color.FromArgb(255, 255, 255, 255)),
            new PointF(0.0f, 0.0f)
            )
          g.Flush()

          let data : byte [] = Array.zeroCreate(size.Width * size.Height * 4)
          let bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb)
          Marshal.Copy(bmpData.Scan0, data, 0, bmpData.Stride * bmpData.Height);
          bmp.UnlockBits(bmpData);

          let dest : byte [] = Array.zeroCreate(Squish.GetStorageRequirements(size.Width, size.Height, SquishFlags.kDxt5))
          Squish.Squish.CompressImage(data, size.Width, size.Height, ref dest, SquishFlags.kDxt5);
          (dest, size)
        ) in

        using (new BinaryWriter(new FileStream(texturesPath + gl.ToString() + ".dds", FileMode.Create))) (
          fun bw ->
            let flags = DDSFlags.Caps ||| DDSFlags.Height ||| DDSFlags.Width ||| DDSFlags.PixelFormat ||| DDSFlags.MipMapCount ||| DDSFlags.LinearSize

            bw.Write(Array.map (byte) [|0x44; 0x44; 0x53; 0x20|]) // 'DDS '
            bw.Write(124) // dwSize
            bw.Write((int) flags) // dwFlags
            bw.Write(100) // dwHeight
            bw.Write(100) // dwWidth
            bw.Write((mipmap |> List.head |> fst).Length) // dwPitchOrLinearSize
            bw.Write(0) // dwDepth
            bw.Write(3) // dwMipMapCount

            bw.Write(0)
            bw.Write(0)
            bw.Write(0)
            bw.Write(0)
            bw.Write(0)
            bw.Write(0)
            bw.Write(0)
            bw.Write(0)
            bw.Write(0)
            bw.Write(0)
            bw.Write(0) // dwReserved1

            bw.Write(32) // ddpfPixelFormat // dwSize
            bw.Write(0x4) // dwFlags

            bw.Write(Array.map (byte) [|0x44; 0x58; 0x54; 0x35|]);
            bw.Write(0)
            bw.Write(0)
            bw.Write(0)
            bw.Write(0)
            bw.Write(0)

            bw.Write(0x1000 ||| 0x400000 ||| 0x8);
            bw.Write(0); // Caps 2
            
            bw.Write(0);
            bw.Write(0);
            bw.Write(0); // dwReserved2

            mipmap |> List.iter(fun (data, _) -> bw.Write(data))
        )

    let generateMesh materialPath modelPath (glyph : uint32) =
      let polyBase = [(-0.5f, 1.0f); (-0.5f, 0.0f); (0.5f, 0.0f); (-0.5f, 1.0f); (0.5f, 0.0f); (0.5f, 1.0f)]
      let mesh : MeshData = {
        vertices = List.map (fun (x, y) -> Vector3(x, 0.0f, y)) polyBase;
        normals = List.init 6 (fun _ -> Vector3(0.0f, 1.0f, 0.0f));
        tangents = List.init 6 (fun _ -> Vector3(1.0f, 0.0f, 0.0f));
        uv0 = List.map (fun (x, y) -> Vector2(x, y)) polyBase;
        uv1 = [];
        indices = (0, 1, 2) :: (3, 4, 5) :: []
        }
      let (blob, mesh) = Mesh.generate mesh (materialPath + glyph.ToString())

      let mshPath = modelPath + glyph.ToString() + ".msh"
      if not(Directory.Exists modelPath) then (Directory.CreateDirectory modelPath) |> ignore
      let blobPath = mshPath + ".blob"
      File.WriteAllBytes(blobPath, blob)
      File.WriteAllText(mshPath, mesh)

    let generateMaterial materialPath texturePath (glyph : uint32) = 
      let mtl = 
        F("data",
          A [
            P ("params", A [
              P ("map_color_alpha", A [
                P ("compressionAllowed", B true);
                P ("fileName", S (texturePath + glyph.ToString() + ".dds"));
                P ("magFilter", S "LINEAR");
                P ("minFilter", S "LINEAR_MIPMAP_LINEAR");
                P ("mipmapAlphaScale", V 0);
                P ("type", S "TWOD");
                P ("wrapS", S "CLAMP_TO_EDGE");
                P ("wrapT", S "CLAMP_TO_EDGE")
              ]);
              P ("polygon_offset", A [
                P ("factor", V 0);
                P ("units", V 0)
              ]);
              P ("two_sided", A [P ("twoSided", B true)])
            ]);
            P ("type", S "TRANSPARENT")
            ]
          )
        |> printLua 0 in
      if not(Directory.Exists materialPath) then (Directory.CreateDirectory materialPath) |> ignore
      File.WriteAllText(materialPath + glyph.ToString() + ".mtl", mtl)
      


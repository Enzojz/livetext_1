﻿namespace Livetext
open System.Drawing

module Task = 
  open System.IO

  let task cp (facename : string) (style : FontStyle) = 
      
      let scriptsPath = "res/scripts/"
      let texturesPath = "res/textures/models/"
      let meshPath = "res/models/mesh/"
      let materialPath = "res/models/material/"
      let modelPath = "res/models/model/"

      let font = new Font(facename, 80.0f, style)
      let key = (font.FontFamily.Name + (if font.Style = FontStyle.Regular then "" else ("_" + font.Style.ToString()))).ToLower()

      let midFix = "livetext/" + key + "/"
            
      Directory.CreateDirectory(materialPath + midFix)    |> ignore
      Directory.CreateDirectory(texturesPath + midFix)    |> ignore
      Directory.CreateDirectory(meshPath + midFix)        |> ignore
      Directory.CreateDirectory(modelPath + midFix)       |> ignore
      Directory.CreateDirectory(scriptsPath + "livetext/")|> ignore
      
      //cp.ToList().AsParallel()
      //.ForAll(c =>
      //{
      //    Output.drawGlyph(font, midFix, texturesPath, materialPath, c);
      //    Output.generateMesh(midFix, meshPath + midFix, c);
      //    Output.generateModel(midFix, modelPath + midFix, c);
      //}
      //);

      let mat = Output.drawColorTexture midFix texturesPath materialPath (Color.FromArgb(255, 255, 255, 255))
      let trans = Output.drawColorTexture midFix texturesPath materialPath (Color.FromArgb(0, 255, 255, 255))

      cp
      |> List.iter
        (fun c ->
            Output.extractPolygon mat trans (meshPath + midFix) font c
            Output.generateModel midFix (modelPath + midFix) c
        )

      Output.generateDescription font cp (scriptsPath + "livetext/" + key + ".lua")


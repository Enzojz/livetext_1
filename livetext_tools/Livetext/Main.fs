open Livetext
open System.Drawing
open System.IO

[<EntryPoint>]
let main argv = 
    let cp = 
      [
        [0x20..0x7F];//Latin
        [0x0A1..0x024F];//Latin
        [0x1E00..0x1EFF];//Latin
        //[0x400..0x52F];//Cyrilic
        //[0x0370..0x03FF];//Greek
        [0x2030..0x2031];//Per mille
        [0x2200..0x22FF];//Arrows
        [0x2190..0x21FF];//Arrows
        [0X2900..0x297F]//Math
      ]
      |> List.concat
      |> List.except [0x46D;0x4FD;0x3D6]
      |> List.map (uint32)
    
    //Directory.Delete("res", true)
    Task.task cp "Arial" FontStyle.Regular
    0 // return an integer exit code

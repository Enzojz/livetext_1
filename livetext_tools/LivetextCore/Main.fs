open Livetext
open System.Drawing
open System.IO
open System

[<EntryPoint>]
let main argv = 
    let cp = 
      [
        [0x20..0x7F];    // Latin
        [0x0A1..0x024F]; // Latin
        //[0x400..0x52F];  // Cyrilic
        //[0x0370..0x03FF];// Greek
        [0x2030..0x2031];// Per mille
        [0x2200..0x22FF];// Math
        [0x2190..0x21FF];// Arrows
        [0X2900..0x297F];// Arrows
        [0x4E00..0x9FEF]; // 基本汉字
        [0x3040..0x309F]; // 平仮名
        [0x30A0..0x30FF]; // 片仮名
        [0x1100..0x11FF]; // 한글
      ]
      |> List.concat
      |> List.map (uint32)
    
    if Directory.Exists("res") then
      Directory.Delete("res", true)
    else
      ()
    
    //Task.task cp "Lato" FontStyle.Regular
    Task.task cp "Source Han Sans CN Normal" FontStyle.Regular
    printfn "Press any key to close..."
    Console.ReadKey() |> ignore
    0 // return an integer exit code

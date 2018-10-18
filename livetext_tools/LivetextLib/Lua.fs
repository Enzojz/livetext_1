namespace Livetext
open System

module Lua = 
    type Lua = 
        | V of int
        | B of bool
        | S of string
        | P of (string * Lua)
        | A of Lua list
        | F of (string * Lua)
        | C of (bool * Lua)
    
    let rec printLua indent (node : Lua) = 
        let ind str = 
            function 
            | true -> (Environment.NewLine + (String.replicate indent "  ") + str)
            | false -> str
        match node with
        | V num -> sprintf "%d" num
        | S str -> @"""" + str + @""""
        | P(name, x) -> name + " = " + (printLua (indent + 1) x)
        | A ls -> 
            "{ " + (ls
                    |> List.map (printLua (indent + 1))
                    |> String.concat ", ")
            |> fun str -> str + (ind " }" (str.Contains(Environment.NewLine)))
        | F(f, x) -> (sprintf "function %s() return " f) + (printLua (indent + 1) x) + " end"
        | C(c, x) -> 
            if c then (printLua indent x)
            else ""
        | B bln -> sprintf "%s" (if bln then "true" else "false")
        |> fun rs -> 
            match node with
            | C _ | F _ | V _ -> rs
            | _ -> ind rs (rs.Length > 30)
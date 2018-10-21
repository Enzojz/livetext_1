namespace Livetext
open Poly2Tri

  module Poly =
    open System.Numerics
    open System.Windows

    type PolyTree = 
      {
        node : Vector2 list;
        children : PolyTree list
      }
    
    type Position =
      | Inside
      | Outside
      | On

    
    let isInside (rhs : Vector2 list) (lhs : Vector2 list) =
      let isInPolygon (poly : Vector2 list) (point : Vector2) =
        let segs = 
          List.zip poly (((List.head poly) :: (List.tail poly |> List.rev)) |> List.rev)
        let mRot = 
          List.map (fun (p1, p2) -> p2 - p1) segs @ List.map (fun p -> p - point) poly
          |> List.filter (fun v -> v.Length() > 0.0f)
          |> List.map (fun v -> atan2 v.Y v.X)
          |> List.distinct
          |> List.sortBy (fun r -> abs r)
          |> List.except [0.0f]
          |> function fst :: _ -> -0.5f * fst | _ -> 0.0f
          |> Matrix3x2.CreateRotation
        segs 
          |> List.filter (fun (p1, p2) ->
            let pA = Vector2.Transform(p1 - point, mRot)
            let pB = Vector2.Transform(p2 - point, mRot)
            pA.Y * pB.Y < 0.0f
          )
          |> function r -> r.Length % 2 = 0
      lhs |> List.filter (isInPolygon rhs) |> List.length > 0
    
    let rec tryInsertInto (node : PolyTree) (poly : Vector2 list)
      match isInside node.node poly, isInside poly node.node, node.children with
      | true, false, [] ->  

    let processLevel (poly : Vector2 list) (root : PolyTree) = 
      match isInside root.node poly, isInside poly root.node with
      | true, false -> 
        match root.children |> List.exists (fun p -> isInside p poly), root.children |> List.exists (isInside poly) with
        | false, false -> 
          {
            root with
              children = {
                node = poly;
                children = []
              }:: root.children
          }, None
        | true, false ->
            root, None
        | false, true ->
            
            {
            }

    let createSetFromList (polys : Polygon list) =
      let mutable root = new PolyTest.PolygonHierachy(List.head polys)
      
      polys
      |> List.tail
      |> List.iter (fun p -> processLevel p root)

      let mutable set = new PolygonSet()
      PolyTest.ProcessSetLevel(set, root)

      set;


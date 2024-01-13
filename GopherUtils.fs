(*
*  This Source Code Form is subject to the terms of the Mozilla Public
*  License, v. 2.0. If a copy of the MPL was not distributed with this
*  file, You can obtain one at http://mozilla.org/MPL/2.0/.
*)
module Longboat.GopherUtils

open System.IO
open System.Text.RegularExpressions

let getCanonicalSelector (s: string) =
    let stripslash (x: string) = if x.EndsWith '/' && x.Length <> 1 then x[..x.Length - 2] else x
    
    if s = "" then "/"
    else s.Trim()
        |> stripslash
    
let createSelector (file: string) (dir:string): string =
    let relPath = Path.GetRelativePath(dir, file)
    let path = if relPath.EndsWith ".gph" then relPath.Substring(0, relPath.Length - 4) else relPath
    sprintf "/%s" path

let rec createDirSelectors dir (root: option<string>) =
    let rootDir = if root = None then dir else root.Value
    let files =
        //printfn "%s\n" dir
        Directory.GetFiles dir
        |> Array.map Path.GetFullPath
        //|> Array.map(fun x -> if x.EndsWith ".gph" then Path.GetFileNameWithoutExtension x else x)
        |> Array.map (fun x -> ((createSelector x rootDir), x))
        |> Map
    let subdirs =
        Directory.GetDirectories dir
        |> Array.map (fun x -> createDirSelectors(x)(Some(rootDir)))
        |> Array.fold (Map.fold (fun acc key value -> (Map.add key value acc))) (Map [| |])
    Map.fold(fun acc key value -> Map.add key value acc) files subdirs
    
let makeGopherText (page: string) =
    let regex = Regex(@"^\.$", RegexOptions.Multiline)
    regex.Replace(page, "..")
    + "\n.\n"

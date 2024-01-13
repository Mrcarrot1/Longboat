(*
*  This Source Code Form is subject to the terms of the Mozilla Public
*  License, v. 2.0. If a copy of the MPL was not distributed with this
*  file, You can obtain one at http://mozilla.org/MPL/2.0/.
*)
module Longboat.Preprocessor

open System.Diagnostics
open System.IO
open System.Linq
open System.Text.RegularExpressions
open Longboat.Config
open Longboat.Logger

let evalCmd (cmd: string) =
    let splitCmd = cmd.Split ' '
    match splitCmd[0] with
    | "sys" ->
        let startInfo = ProcessStartInfo(splitCmd[1], splitCmd[2..] |> Array.fold (+) "")
        startInfo.RedirectStandardOutput <- true
        Process.Start(startInfo).StandardOutput.ReadToEnd().TrimEnd()
    | "serv" -> match splitCmd[1] with
                | "port" -> globalConfig.port |> string
                | "host" | "hostname" -> globalConfig.hostname
                | "hostport" -> $"{globalConfig.hostname}\t{globalConfig.port}"
                | "version" -> "pre-alpha"
                | x -> 
                    logmsg $"Invalid Longboat Preprocessor command: {x}" LogLevel.Err
                    "ERROR: Invalid Longboat Preprocessor command"
    | x ->
        logmsg $"Invalid Longboat Preprocessor command: {x}" LogLevel.Err
        "ERROR: Invalid Longboat Preprocessor command"

let preprocess file query =
    let lines = File.ReadAllLines file
    let addNewline s1 s2 =
        s1 + $"{s2}\n"
    if lines[0].Trim() <> "#longboat preproc" then lines |> Array.fold addNewline ""
    else
        let text = Array.fold addNewline "" lines[1..]
        let commands = (Regex.Matches (text, @"\$\{[^\}]*\}")).ToArray().DistinctBy(fun x -> x.Value)
        let replaceMatch (text: string)(mtch: Match) =
            let cmd = (mtch.Value[2..mtch.Value.Length - 2])
            text.Replace(mtch.Value, evalCmd cmd)
        let output = 
            seq { for c in commands -> c } 
            |> Seq.fold replaceMatch text
        output 

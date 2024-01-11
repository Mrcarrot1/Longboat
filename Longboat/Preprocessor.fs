module Longboat.Preprocessor

open System.Diagnostics
open System.IO
open System.Text.RegularExpressions
open Longboat.Config
open Longboat.Logger

let evalCmd (cmd: string) =
    let splitCmd = cmd.Split ' '
    match splitCmd[0] with
    | "sys" ->
        let startInfo = ProcessStartInfo(splitCmd[1], splitCmd[2..] |> Array.fold (+) "")
        startInfo.RedirectStandardOutput <- true
        Process.Start(startInfo).StandardOutput.ReadToEnd()
    | "serv" -> match splitCmd[1] with
                | "port" -> globalConfig.port |> string
                | "version" -> "pre-alpha"
                | x -> 
                    logmsg $"Invalid Longboat Preprocessor command: {x}" LogLevel.Err
                    "ERROR: Invalid Longboat Preprocessor command"
    | x ->
        logmsg $"Invalid Longboat Preprocessor command: {x}" LogLevel.Err
        "ERROR: Invalid Longboat Preprocessor command"
    
let preprocess file query =
    let text = File.ReadAllText file
    for x in Regex.Matches (text, "\$\{.*\}") do
        let cmd = x.Value[2..x.Value.Length - 2]
        printfn "%s" cmd
    text

(*
*  This Source Code Form is subject to the terms of the Mozilla Public
*  License, v. 2.0. If a copy of the MPL was not distributed with this
*  file, You can obtain one at http://mozilla.org/MPL/2.0/.
*)
module Longboat.Config

open System
open System.IO
open System.Runtime.InteropServices
open Longboat.Logger

type serverConfiguration = { port: int; quicPort: int; enableNoHttpMessage: bool; serveDirectory: string; hostname: string; }

let parseConfig file =
    File.ReadAllLines file
        |> Array.filter (fun x -> not (x.StartsWith '#'))
        |> Array.map (fun x -> 
            let split = x.Split ':'
            (split[0].Trim(), split[1].Trim())
        )
        |> Map

let getConfigLocation(): string =
    if RuntimeInformation.IsOSPlatform OSPlatform.Linux then
        if File.Exists $"{Environment.CurrentDirectory}/config" then $"{Environment.CurrentDirectory}/config"
        else if File.Exists $"""{Environment.GetEnvironmentVariable "XDG_CONFIG_HOME"}/longboat/config""" then 
            $"""{Environment.GetEnvironmentVariable "XDG_CONFIG_HOME"}/longboat/config"""
        else if File.Exists "/etc/longboat/config" then "/etc/longboat/config" else ""
    else
        let path = Path.Combine(Environment.CurrentDirectory, "config")
        logmsg (sprintf "The current platform does not have a default configuration path implementation. The path that will be used is %s" path) LogLevel.Warn
        path

let globalConfig: serverConfiguration = 
    let config = 
        if File.Exists (getConfigLocation()) then 
            parseConfig (getConfigLocation()) 
            else 
                logmsg "Configuration file not found; using default settings" LogLevel.Warn
                Map<string, string>([])
    {
        port = 
            match config.TryFind "port" with
            | Some(x) -> int x
            | None -> 70
        quicPort = 
            match config.TryFind "quicPort" with
            | Some(x) -> int x
            | None -> 0
        enableNoHttpMessage = 
            match config.TryFind "enableNoHttpMessage" with
            | Some(x) -> bool.Parse x
            | None -> true
        serveDirectory =
            match config.TryFind "serveDirectory" with
            | Some(x) -> x
            | None -> Path.Combine(Environment.CurrentDirectory, "srv")
        hostname =
            match config.TryFind "hostname" with
            | Some(x) -> x
            | None -> System.Net.Dns.GetHostEntry("").HostName
    }

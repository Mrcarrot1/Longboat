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
        ""

let globalConfig: serverConfiguration = 
    let config = 
        if File.Exists (getConfigLocation()) then 
            parseConfig (getConfigLocation()) 
            else 
                logmsg "Configuration file not found; using default settings" LogLevel.Warn
                Map<string, string>([])
    {
        port = if config.ContainsKey "port" then config["port"] |> int else 70;
        quicPort = if config.ContainsKey "quicPort" then config["quicPort"] |> int else 0;
        enableNoHttpMessage = if config.ContainsKey "enableNoHttpMessage" then config["enableNoHttpMessage"] |> bool.Parse else true;
        serveDirectory = if config.ContainsKey "serveDirectory" then config["serveDirectory"] else (Path.Combine(Environment.CurrentDirectory, "srv"));
        hostname = if config.ContainsKey "hostname" then config["hostname"] else System.Net.Dns.GetHostEntry("").HostName;
    }

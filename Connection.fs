(*
*  This Source Code Form is subject to the terms of the Mozilla Public
*  License, v. 2.0. If a copy of the MPL was not distributed with this
*  file, You can obtain one at http://mozilla.org/MPL/2.0/.
*)
module Longboat.Connection

open System.IO
open System.Net
open System.Text.RegularExpressions
open Utils
open Logger
open GopherUtils
open Preprocessor
open Config


//Define the message we send to HTTP clients.
//No, I'm not recommending my own client yet; it's not fit for general use.
let noHttpMessage = "<!DOCTYPE html>This is not an HTTP(web) server.<br>To access this server, you should use a client for the Gopher protocol.<br>You can download one from <a href=https://gopher.floodgap.com/overbite/>The Overbite Project</a>.</html>"

let httpUnsupported = netstring $"HTTP/1.1 418 I'm a teapot\nServer: Longboat Gopher Server\nContent-Type: text/html; charset=utf8\nContent-Length: {(netstring noHttpMessage).Length}\n\n{noHttpMessage}"

let notFound = gophererr "The requested resource was not found." + "\n.\n"

type clientInfo = { endpoint: IPEndPoint; connType: string }

let handleconn(stream: Stream)(resources: Map<string, string>)(clientInfo: clientInfo) =
    task {
        let bufferSize = 4096
        let buffer: byte[] = (Array.zeroCreate bufferSize)
        try
            logmsg "Accepting connection from {clientInfo.endpoint.Address.ToString()}({clientInfo.connType})" 
                    LogLevel.Info
            
            let! received = stream.ReadAsync(buffer, 0, bufferSize)
            
            let request = buffer[0..received - 1]
            let requestString = (localstring request).Split("\n")[0]
            let requestFields = requestString.Split '\t'
            let selector = requestFields[0]
            
            //Check to see if someone has tried to connect with a web browser and inform them that we don't like their 
            //kind here
            //This can also be disabled in configuration to prevent running a regex match on literally every selector
            if globalConfig.enableNoHttpMessage && 
                Regex.IsMatch(selector, "(:?GET|HEAD|POST|PUT|DELETE|CONNECT|OPTIONS|TRACE|PATCH).*HTTP.*") then
                do! (stream.WriteAsync(httpUnsupported))
            else
                if request[request.Length - 2..request.Length - 1] <> [| byte 0x0d; byte 0x0a |] then
                    do! stream.WriteAsync(netstring (gophererr "Query message was too long or not formatted correctly!"))
                else
                    let query = (localstring request).TrimEnd()
                    match Map.tryFind (getCanonicalSelector selector) resources with
                    | Some(file) -> do! stream.WriteAsync(netstring (makeGopherText (preprocess file query)))
                    | None -> do! stream.WriteAsync(netstring notFound)
        with
        | e -> logmsg 
                (sprintf "Exception when communicating with client; assuming disconnected\nError message: %s" e.Message) 
                LogLevel.Err
        stream.Dispose()
    }

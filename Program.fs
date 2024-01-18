(*
*  This Source Code Form is subject to the terms of the Mozilla Public
*  License, v. 2.0. If a copy of the MPL was not distributed with this
*  file, You can obtain one at http://mozilla.org/MPL/2.0/.
*)
module Longboat.Program

open System
open System.IO
open System.Net
open System.Net.Security
open System.Net.Sockets
open System.Net.Quic
open System.Security.Cryptography.X509Certificates
open System.Text
open System.Text.RegularExpressions
open System.Threading.Tasks
open Longboat.Logger
open Longboat.Config
open Utils
open GopherUtils
open Preprocessor

//Define the message we send to HTTP clients.
//No, I'm not recommending my own client yet; it's not fit for general use.
let noHttpMessage = "<!DOCTYPE html>This is not an HTTP(web) server.<br>To access this server, you should use a client for the Gopher protocol.<br>You can download one from <a href=https://gopher.floodgap.com/overbite/>The Overbite Project</a>.</html>"
let httpUnsupported = $"HTTP/1.1 418 I'm a teapot\nServer: Longboat Gopher Server\nContent-Type: text/html; charset=utf8\nContent-Length: {(netstring noHttpMessage).Length}\n\n{noHttpMessage}"

let notFound = gophererr "The requested resource was not found." + "\n.\n"

type clientInfo = { endpoint: IPEndPoint; connType: string }

let handleconn(stream: Stream)(resources: Map<string, string>)(clientInfo: clientInfo) =
    async {
        let bufferSize = 4096
        let buffer: byte[] = (Array.zeroCreate bufferSize)
        try
            logmsg (sprintf "Accepting connection from %s(%s)" (clientInfo.endpoint.Address.ToString()) clientInfo.connType) LogLevel.Info
            
            let received = waitreturn (stream.ReadAsync(buffer, 0, bufferSize))
            
            let request = buffer[0..received - 1]
            //printfn "received %d bytes" received
            //let bufferString =
            //    seq { for i in 0 .. received - 1 -> sprintf "0x%02x " buffer[i] }
            //    |> Seq.fold (+) ""
            let requestString = (localstring request).Split("\n")[0]
            let requestFields = requestString.Split '\t'
            let selector = requestFields[0]
            
            //Check to see if someone has tried to connect with a web browser and inform them that we don't like their kind here
            //This can also be disabled in configuration to prevent running a regex match on literally every selector
            if globalConfig.enableNoHttpMessage && Regex.IsMatch(selector, "(:?GET|HEAD|POST|PUT|DELETE|CONNECT|OPTIONS|TRACE|PATCH).*HTTP.*") then
                waitvtask (stream.WriteAsync(netstring httpUnsupported))
            else
                if request[request.Length - 2..request.Length - 1] <> [| byte 0x0d; byte 0x0a |] then
                    waittask (stream.WriteAsync(netstring (gophererr "Query message was too long or not formatted correctly!")).AsTask())
                else
                    match Map.tryFind (getCanonicalSelector selector) resources with
                    | Some(file) -> waitvtask (stream.WriteAsync(netstring (makeGopherText (preprocess file request))))
                    | None -> waitvtask (stream.WriteAsync(netstring notFound))
        with
        | e -> logmsg (sprintf "Exception when communicating with client; assuming disconnected\nError message: %s" e.Message) LogLevel.Err
        stream.Dispose()
    }

let startTcpServer (listener: TcpListener) resources =
    async {
        listener.Start()
        while true do
            let handler = waitreturn (listener.AcceptTcpClientAsync())
            let clientInfo: clientInfo = { endpoint = handler.Client.RemoteEndPoint :?> IPEndPoint; connType = "TCP" } 
            handleconn (handler.GetStream()) resources clientInfo |> Async.Start
    }

let startQuicServer (listener: QuicListener) resources =
    async {
        while true do
            let handler = waitvreturn (listener.AcceptConnectionAsync())
            let clientInfo: clientInfo = { endpoint = handler.RemoteEndPoint; connType = "QUIC" }
            handleconn (waitvreturn (handler.OpenOutboundStreamAsync(QuicStreamType.Bidirectional))) resources clientInfo |> Async.Start
    }

[<EntryPoint>]
let main args =
    //if not QuicListener.IsSupported then logmsg "The QUIC protocol is not supported on this platform" LogLevel.Warn
    printfn "Starting Longboat Gopher Server (F# Edition)"

    let resources = createDirSelectors globalConfig.serveDirectory None
    logmsg "Mapped Selectors:" LogLevel.Info
    for res in resources do printfn "    %s -> %s" res.Key res.Value
        
    let endpoint = IPEndPoint(IPAddress.Any, globalConfig.port)
    //let quicEndpoint = IPEndPoint(IPAddress.Any, globalConfig.quicPort)
    
    let listener = new TcpListener(endpoint)
    
    try
        let applicationProtocols = System.Collections.Generic.List<SslApplicationProtocol>( [| SslApplicationProtocol("gopher") |] )
        //let qServConnOpts = QuicServerConnectionOptions(DefaultStreamErrorCode = 0x0A, DefaultCloseErrorCode = 0x0B,
        //                                                ServerAuthenticationOptions = SslServerAuthenticationOptions(ApplicationProtocols = applicationProtocols,
        //                                                                                                             ServerCertificate = new X509Certificate("certificate.cer")))
        //let quicListener = waitvreturn (QuicListener.ListenAsync (QuicListenerOptions(ListenEndPoint = quicEndpoint,
        //                                                                 ApplicationProtocols = applicationProtocols,
        //                                                                 ConnectionOptionsCallback = Func<_,_,_,_>(fun _ _ _ -> ValueTask.FromResult(qServConnOpts)))))
        //listener.Start()
        
        
        startTcpServer listener resources |> Async.Start
        //startQuicServer quicListener resources |> Async.Start
        
        waittask (Task.Delay(-1))
    finally
        listener.Stop()
    0

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
            //use stream = handler.GetStream()
                
            logmsg (sprintf "Accepting connection from %s(%s)" (clientInfo.endpoint.Address.ToString()) clientInfo.connType) LogLevel.Info
            
            let received = waitreturn (stream.ReadAsync(buffer, 0, bufferSize))
            
            let request = buffer[0..received - 1]
            //printfn "received %d bytes" received
            let bufferString =
                seq { for i in 0 .. received - 1 -> sprintf "0x%02x " buffer[i] }
                |> Seq.fold (+) ""
            //printfn "Buffer: %s" bufferString
            let requestString = (localstring request).Split("\n")[0]
            //printfn "Received string: %s" (requestString |> String.collect(fun c -> if c = '\r' then "[CR]" elif c = '\n' then "[LF]" else c.ToString()))
            let requestFields = requestString.Split '\t'
            let selector = requestFields[0]
            
            //Check to see if someone has tried to connect with a web browser and inform them that we don't like their kind here
            if globalConfig.enableNoHttpMessage && Regex.IsMatch(selector, "(:?GET|HEAD|POST|PUT|DELETE|CONNECT|OPTIONS|TRACE|PATCH).*HTTP.*") then
                waitvtask (stream.WriteAsync(netstring httpUnsupported))
            else
                if request[request.Length - 2..request.Length - 1] <> [| byte 0x0d; byte 0x0a |] then
                    waittask (stream.WriteAsync(netstring (gophererr "Query message was too long or not formatted correctly!")).AsTask())
                else
                    //printfn "%s" ((getCanonicalSelector selector) |> String.collect(fun c -> if c = '\r' then "[CR]" elif c = '\n' then "[LF]" else c.ToString()))
                    //for res in resources do printfn "%s%s -> %s" res.Key (if res.Key = getCanonicalSelector selector then "(match)" else "") res.Value
                    match Map.tryFind (getCanonicalSelector selector) resources with
                    | Some(file) -> waitvtask (stream.WriteAsync(netstring (makeGopherText (File.ReadAllText file))))
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
    printfn "%s" (evalCmd "serv port")

    //printfn "HTTP Unsupported response will be: \n%s" httpUnsupported
    
    //let servingDir = if args.Length > 0 then args[0] else Directory.GetCurrentDirectory()
    let resources = createDirSelectors globalConfig.serveDirectory None
    logmsg "Mapped Selectors:" LogLevel.Info
    for res in resources do printfn "    %s -> %s" res.Key res.Value
    //for res in resources do preprocess res.Value ""
    //printfn "%s" (createSelector "/home/gopher.gph" "/")
        
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

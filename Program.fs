(*
*  This Source Code Form is subject to the terms of the Mozilla Public
*  License, v. 2.0. If a copy of the MPL was not distributed with this
*  file, You can obtain one at http://mozilla.org/MPL/2.0/.
*)
module Longboat.Program

open System.Net
open System.Net.Sockets
open System.Threading.Tasks
open Logger
open Config
open Utils
open GopherUtils
open Connection

[<TailCall>]
let rec runServer (listener: TcpListener) resources =
    let handler = waitreturn (listener.AcceptTcpClientAsync())
    async {
        let clientInfo = { endpoint = handler.Client.RemoteEndPoint :?> IPEndPoint; connType = "TCP" }
        do! handleconn (handler.GetStream()) resources clientInfo |> Async.AwaitTask
    } |> Async.Start
    runServer listener resources

/// <summary>Starts the TCP(standard Gopher) listener serving the specified collection of resources.</summary>
/// <param name="listener"></param>
/// <param name="resources"></param>
/// <returns></returns>
let startTcpServer (listener: TcpListener) resources =
    async {
        listener.Start()
        runServer listener resources
    }

[<EntryPoint>]
let main _ =
    logmsg "Starting Longboat Gopher Server" LogLevel.Info

    let resources = createDirSelectors globalConfig.serveDirectory None
    logmsg "Mapped Selectors:" LogLevel.Info
    for res in resources do printfn "    %s -> %s" res.Key res.Value
        
    let endpoint = IPEndPoint(IPAddress.Any, globalConfig.port)
    
    let listener = new TcpListener(endpoint)
    
    try
        startTcpServer listener resources |> Async.Start
        waittask (Task.Delay(-1))
    finally
        listener.Stop()
    0

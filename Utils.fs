(*
*  This Source Code Form is subject to the terms of the Mozilla Public
*  License, v. 2.0. If a copy of the MPL was not distributed with this
*  file, You can obtain one at http://mozilla.org/MPL/2.0/.
*)
module Longboat.Utils

open System.Net.Sockets
open System.Text
open System.Threading.Tasks
open System.Runtime.InteropServices

///Converts a local .NET string to a UTF-8 byte array for network use.
///Also replaces Unix-style LF line endings with the CRLF used by Gopher and HTTP.
let netstring (str: string) =
    Encoding.UTF8.GetBytes (str.ReplaceLineEndings("\r\n"))

///Converts a UTF-8 byte array to a local .NET string.
///Also replaces Gopher or HTTP CRLF line endings with Unix-style LF.
let localstring (bytes: byte[]) =
    (Encoding.UTF8.GetString bytes).ReplaceLineEndings("\n")

///Formats the given string as a Gopher information(i) element.
let gopherinfo str =
    sprintf "i%s\t\t(NULL)\t0\n" str
    
///Formats the given string as a Gopher error(3) element.
let gophererr str =
    sprintf "3%s\t\t(NULL)\t0\n" str
    
let pathcmp (str1: string) (str2: string) =
    let stripslash (x: string) = if x.EndsWith '/' && x.Length <> 1 then x[..x.Length - 2] else x
    
    let path1 =
        str1.Trim()
        |> stripslash
    let path2 =
        str2.Trim()
        |> stripslash
        
    path1 = path2

type platform =
 | Linux
 | FreeBSD
 | MacOS
 | Windows
 | Unknown

let getCurrentPlatform () =
    if RuntimeInformation.IsOSPlatform OSPlatform.Linux then
        Linux
    elif RuntimeInformation.IsOSPlatform OSPlatform.FreeBSD then
        FreeBSD
    elif RuntimeInformation.IsOSPlatform OSPlatform.OSX then
        MacOS
    elif RuntimeInformation.IsOSPlatform OSPlatform.Windows then
        Windows
    else 
        Unknown
    
let inline waittask (t: Task) =
    t
    |> Async.AwaitTask
    |> Async.RunSynchronously
    
let waitvtask (t: ValueTask) =
    t.AsTask()
    |> Async.AwaitTask
    |> Async.RunSynchronously

let waitreturn t =
    t
    |> Async.AwaitTask
    |> Async.RunSynchronously
    
let waitvreturn (t: ValueTask<'a>) =
    t.AsTask()
    |> Async.AwaitTask
    |> Async.RunSynchronously
    
let trywrite(stream: NetworkStream)(buffer: byte[]): bool =
    try
        stream.WriteAsync(buffer).AsTask()
        |> Async.AwaitTask
        |> Async.RunSynchronously
        true
    with
    | _ -> false
    

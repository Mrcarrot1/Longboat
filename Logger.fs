(*
*  This Source Code Form is subject to the terms of the Mozilla Public
*  License, v. 2.0. If a copy of the MPL was not distributed with this
*  file, You can obtain one at http://mozilla.org/MPL/2.0/.
*)
module Longboat.Logger

open System

type LogLevel =
    | Info = 0
    | Warn = 1
    | Err = 2
    | Exc = 3

let private loglevelstr level =
    match level with
    | LogLevel.Info -> "INF"
    | LogLevel.Warn -> "WRN"
    | LogLevel.Err -> "ERR"
    | LogLevel.Exc -> "EXC"
    | _ -> ""

let logmsg msg level =
    printfn "[%s %s] %s" (loglevelstr level) (DateTime.Now.ToString("HH:mm:ss")) msg

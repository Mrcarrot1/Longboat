module Longboat.GopherTypes

type GopherEntity = {
    filePath: string
    isSearch: bool
    preprocess: bool
    metadata: Map<string, string>
}
# Longboat Gopher Server
This is a nearly-from-scratch implementation of a server for the Gopher(+) protocol, written in F#.

It is one of two tools I have created which intend to provide a full Gopher experience. The other is the Rower client, written in C.
For the rationale behind why I chose to write software for Gopher, see [that project's page](https://github.com/Mrcarrot1/rower).

This server is written in F# for two main reasons:
1) F# is a managed language, and I don't trust myself to write a server implementation in C that doesn't have RCE or other vulnerabilities.
2) Every once in a while, I get the uncontrollable urge to flex on lesser programmers who don't know ML family languages.

I do apologize for the quality of the code, though. I am certainly not an expert in the use of this beautiful ~~witchcraft~~ language.

## Using this Server
### Build/Install
TBD. This project isn't mature enough to have a set build process yet, and you shouldn't want to install it yet.
### Running
#### NOTE: This information is not up to date. It will be updated soon with proper configuration steps.

In its most basic form, run `longboat [server root]`, where `[server root]` is an optional parameter which provides the directory that contains your Gopher data.
If this is not provided, the server root will be set to the current working directory.
### Gopher Data
Longboat uses a format for Gopher data that is likely not consistent with other servers. Aside from some features I'd like the server to provide, this is not an intentional decision.
Rather, Longboat was written without referencing other servers very much. It does not intend to replace existing Gopher servers, though it may be more compelling than those. 
The main goal of Longboat is to provide a reliable, feature-rich, and easy-to use Gopher+ server for new users.

Longboat maps selector strings for every file in the server root and its subdirectories.
It does so by taking that file's path relative to the root and, if it ends in `.gph`(which Longboat uses for Gopher menu files), removing the extension.
For example, if you have this directory structure:
```
Root
├─ .gph
├─ sub.gph
└─ sub
   └─ doc.txt
```
Longboat will create these selectors:
```
/ -> .gph
/sub -> sub.gph
/sub/doc.txt -> sub/doc.txt
```
While it is not strictly required that there be a file named simply `.gph`, it is highly recommended that there is.
If there is not, the server will not have a root or "main" page. Note that it does not fully conform to standard Gopher in this case.

Longboat will also provide pre-processing for Gopher. Preprocessing is a way to dynamically generate Gopher data on the server at time of request.
In order to indicate that a file should be preprocessed, the file should begin with `#longboat preproc` on its own line.
If the file should begin with this sequence but not be preprocessed, use two `#` symbols instead: `##longboat preproc` will evaluate to the literal string `"#longboat preproc"`.
This control sequence will be stripped from any files where it appears.

Preprocessing syntax is fairly simple: At any point where the data should be dynamically inserted, use `${<command>}`. Commands are Longboat-specific and follow simple command-line-like syntax.
A common example is the `sys` command, which inserts the text output of the provided system command. 
For example, `${sys date}` will insert the text `Wed Nov  1 02:02:48 PM CDT 2023` on the author's machine at time of writing.
Note that this example is designed for Unix-like systems which provide the `date` command, and your system's commands may vary.
It is important to keep in mind the system on which your server will be running when writing these files.

A complete example of basic preprocessing is as follows:
```
#longboat preproc
iWelcome to my server.      (NULL)  0
iThe current date and time is ${sys date}.      (NULL)      0
```
Some other preprocessing commands include `serv` for querying server-specific information, `page` for information about the page, and `req` for information about the selector/request sent by the client.

Full command list:
```
sys

serv host
serv addr
serv port

page file
page selector

req search
req selector
req isplus
```

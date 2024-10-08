# Longboat Contributor Guide
## Expectations
The Longboat Gopher Server is written in a fairly loose style, but there are some basic expectations of any code to be
accepted.

### Mostly Functional
Code should follow functional styles where possible/convenient. There are relatively few hard rules here, with one
exception: mutable shared state is to be avoided if at all possible.

Most acceptable non-functional areas of code will be those where it has to use a BCL(C#/.NET) API that does not work
well within a functional paradigm. As networking and file I/O, key operations for any competent network server, are
inherently impure, functional purity is not a major concern.

### Style
F# enforces most of the style conventions of this project automatically, but some small conventions are added. 
* Indents should always be four spaces unless a different indent makes the start of a line more clearly aligned to a 
    construct on the previous line. 
* Blank lines should be used as seems necessary to make sections of code clearer.
* Code should not be much longer than 120 characters on a line. If it is, there should be some reasonable justification.

## Project Structure
The structure of the codebase is largely organized around supporting the functionality in two places: initial loading,
which takes place in the `main` function(as well as in Config.fs), and client request handling, which primarily takes
place in the `handleconn` function in Connection.fs.

### Asynchronous Programming
Parts of the code that benefit from an asynchronous model are generally written in such a way. Because this is often
somewhat complicated in F#, there are some conventions for doing this.

Asynchronous F# code that uses C# APIs will inevitably have to deal with .NET Tasks. As such, `task {}` blocks are
usually preferred over `async {}` blocks in this project.

#r "paket:

nuget Fake ~> 5 //
nuget Fake.Core ~> 5 //
nuget Fake.Core.Target  //
nuget Fake.DotNet //
nuget Fake.DotNet.AssemblyInfoFile //
nuget Fake.DotNet.Cli //
nuget Fake.DotNet.NuGet //
nuget Fake.IO.FileSystem //
nuget Fake.Tools.Git ~> 5"

#load "./.fake/build.fsx/intellisense.fsx"

#if !FAKE
#r "netstandard"
#r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif

open Fake.Core.TargetOperators
open Fake.Core

module Operators = 
    type ITargets =
        abstract member Name: string with get
    [<Literal>]
    let private AllTargetName = "all"
    let mutable private targets =
        
        Target.create AllTargetName ignore
        [AllTargetName] |> Set.ofList

    let create (target : ITargets) f = 
        target.Name
        |> Target.create <| f
        target.Name ==> "all" |> ignore
        targets <- Set.add target.Name targets

    let (==>) (lhs : ITargets) (rhs : ITargets) =
        if targets.Contains lhs.Name && targets.Contains rhs.Name then
            lhs.Name ==> rhs.Name |> ignore
        rhs

    let (?=>) (lhs : ITargets) (rhs : ITargets) =
        if targets.Contains lhs.Name && targets.Contains rhs.Name then
            lhs.Name ?=> rhs.Name |> ignore
        rhs

    let (<===) (lhs : ITargets) (rhs : ITargets) =
        rhs ==> lhs //deliberately changing order of arguments

    let runOrDefaultWithArguments (target: ITargets) =
        target.Name
        |> Target.runOrDefaultWithArguments 
        
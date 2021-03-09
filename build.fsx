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

#load "Tools.fsx"
#load "Duzer.Operators.fsx"
#load "Docker.fsx"
#load "Packing.fsx"

open Duzer.Operators.Operators
open Docker.Docker

[<RequireQualifiedAccess>]
[<NoComparison>]
type Targets = 
    Build
    | Push
    interface ITargets with
        member x.Name 
            with get() =
                match x with
                Build -> "build"
                | Push -> "push"
let src = 
    let mutable d = System.IO.DirectoryInfo(".")
    while d.Parent <> null && d.Name <> "src" do
        match d.GetDirectories("src")
           |> Seq.tryExactlyOne with
        None -> 
           if d.Parent |> isNull then
              failwith "Couldn't find src/"
           d <- d.Parent
        | Some src ->
            d <- src
    d.FullName
       
let docker = Docker(src + "/..","kmdrd")

let projectName = 
    printfn "Source is %s" src
    System.IO.Directory.GetFiles(src,"*.*sproj")
    |> Seq.exactlyOne
    |> System.IO.Path.GetFileNameWithoutExtension

printfn "Project name is %s" projectName

let build,push = 
    let dockerFile = "docker/Dockerfile"
    if System.IO.File.Exists dockerFile then
       docker.Build(projectName,["FEED_PAT_ARG",Fake.Core.Environment.environVarOrDefault "FEED_PAT" ""],dockerFile),
       docker.Push projectName
    else
       Packing.Packaging.addTarget Packing.Packaging.Targets.Build
       Packing.Packaging.addTarget Packing.Packaging.Targets.PackageAndPush
       Packing.Packaging.Targets.Build :> ITargets,Packing.Packaging.Targets.PackageAndPush :> ITargets

create Targets.Build ignore
create Targets.Push ignore

build ==> Targets.Build
push ==> Targets.Push
build ==> push

Targets.Build
|> runOrDefaultWithArguments


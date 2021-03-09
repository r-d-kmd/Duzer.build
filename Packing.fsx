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
open Fake.Core
open Fake
open Fake.DotNet
open Fake.IO
open System.IO
open Tools.Tools
open Duzer.Operators.Operators

module Packaging = 
    [<RequireQualifiedAccess>]
    type Targets = 
       Build 
       | Package
       | PackageAndPush
       | Test
       | Release
       | InstallDependencies
       interface ITargets with
           member x.Name with get() = 
               match x with
               Targets.Build -> "build"
               | Targets.InstallDependencies -> "installdependencies"
               | Targets.Package -> "package"
               | Targets.PackageAndPush -> "packageandpush"
               | Targets.Test -> "test"
               | Targets.Release -> "release"

    let srcPath = "src/"
    let testsPath = "tests/"

    let getProjectFile folder = 
        if Directory.Exists folder then
            Directory.EnumerateFiles(folder,"*.?sproj")
            |> Seq.tryExactlyOne
        else
            None
 
    let private packageVersion() = 
        match Environment.environVarOrNone "PACKAGE_VERSION" with
        None -> 
            eprintfn "No package version supplied (env var PACKAGE_VERSION)"
            "local"
        | Some v ->
            v

    let rec addTarget target = 
        (match target with
        Targets.Release ->
            create Targets.Release ignore
        | Targets.InstallDependencies ->
            create Targets.InstallDependencies (fun _ ->
                paket srcPath "install"
            )
        | Targets.Build ->
            create Targets.Build (fun _ ->    
                let projectFile = 
                    srcPath
                    |> getProjectFile

                build buildConfiguration "./package" projectFile.Value
            )
        | Targets.Package ->
            create Targets.Package (fun _ ->
                let packages = Directory.EnumerateFiles(srcPath, "*.nupkg")
                
                File.deleteAll packages
                sprintf "pack --version %s ." <| packageVersion()
                |> paket srcPath 
            )
        | Targets.PackageAndPush ->
            create Targets.PackageAndPush (fun _ ->
                let apiKey = 
                    match Environment.environVarOrNone "API_KEY" with
                    None  -> "az"
                    | Some key -> key
                let args = 
                    let workDir = System.IO.Path.GetFullPath(".")
                    sprintf "run -e VERSION=%s -e API_KEY=%s -v %s:/source -t kmdrd/paket-publisher" <| packageVersion() <| apiKey <| workDir
                run "docker" "." args
            )
        | Targets.Test ->
            create Targets.Test (fun _ ->
                match testsPath |> getProjectFile with
                Some tests -> 
                    tests |> DotNet.test id
                | None -> printfn "Skipping tests because no tests was found. Create a project in the folder 'tests/' to have tests run"
            ))

        Targets.Build
            ==> Targets.Package
            |> ignore

        Targets.Build
            ==> Targets.PackageAndPush
            |> ignore

        Targets.Build
            ?=> Targets.Test
            ?=> Targets.Package
            ?=> Targets.PackageAndPush
            |> ignore


        Targets.InstallDependencies
            ?=> Targets.Build
            |> ignore
            
        Targets.Release
                <=== Targets.PackageAndPush
                <=== Targets.Test
                <=== Targets.InstallDependencies
                |> ignore
                
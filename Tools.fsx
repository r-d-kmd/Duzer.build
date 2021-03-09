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

module Tools =
    let mutable buildConfiguration = 
            DotNet.BuildConfiguration.Release
      
    let mutable verbosity = Quiet
        
    let run command workingDir (args : string) = 
        let arguments = 
            match args.Trim() |> String.split ' ' with
            [""] -> Arguments.Empty
            | args -> args |> Arguments.OfArgs
        RawCommand (command, arguments)
        |> CreateProcess.fromCommand
        |> CreateProcess.withWorkingDirectory workingDir
        |> CreateProcess.ensureExitCode
        |> Proc.run
        |> ignore

    let paket workDir args = 
        run "dotnet" workDir ("paket " + args) 

    let build conf outputDir projectFile =
        DotNet.publish (fun opts -> 
                            { opts with 
                                   OutputPath = Some outputDir
                                   Configuration = conf
                                   MSBuildParams = 
                                       { opts.MSBuildParams with
                                              Verbosity = Some verbosity
                                       }    
                            }
                       ) projectFile
    
    
        
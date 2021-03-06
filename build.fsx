#r @"packages/build/FAKE/tools/FakeLib.dll"

open System
open System.IO

open Fake
open Fake.Azure.Kudu

let serverDir = "./src/server" |> FullName
let uiDir = "./src/ui" |> FullName
let publishDir = "./publish" |> FullName

let dotnetcliVersion = DotNetCli.GetDotNetSDKVersionFromGlobalJson ()

let mutable dotnetExePath = "dotnet"

let run' timeout cmd args dir =
    if not (execProcess (fun info ->
        info.FileName <- cmd
        info.UseShellExecute <- cmd.Contains "yarn" && Console.OutputEncoding <> Text.Encoding.GetEncoding (850)
        if not (String.IsNullOrWhiteSpace dir) then info.WorkingDirectory <- dir
        info.Arguments <- args
    ) timeout) then failwithf "Error while running '%s' with args: %s" cmd args

let run = run' System.TimeSpan.MaxValue

let runDotnet workingDir args =
    let result = ExecProcess (fun info ->
        info.FileName <- dotnetExePath
        info.WorkingDirectory <- workingDir
        info.Arguments <- args) TimeSpan.MaxValue
    if result <> 0 then failwithf "dotnet %s failed" args

let platformTool tool winTool = (if isUnix then tool else winTool) |> ProcessHelper.tryFindFileOnPath |> function | Some t -> t | None -> failwithf "%s not found" tool

let nodeTool = platformTool "node" "node.exe"

let installUi yarnTool =
    printfn "Node version:"
    run nodeTool "--version" __SOURCE_DIRECTORY__
    match yarnTool with
    | Some yarnTool ->
        printfn "Yarn version:"
        run yarnTool "--version" __SOURCE_DIRECTORY__
        run yarnTool "install --frozen-lockfile" __SOURCE_DIRECTORY__
    | None -> ()
    runDotnet uiDir "restore"

let build webpackFlags =
    runDotnet serverDir "publish --configuration Release"
    runDotnet uiDir (sprintf "fable webpack -- %s" webpackFlags)

let publish () =
    CreateDir publishDir
    CopyFiles publishDir !! @".\src\server\bin\Release\netcoreapp2.0\*.*"
    let uiDir = publishDir </> "ui"
    CreateDir uiDir
    CopyFile uiDir @".\src\ui\index.html"
    let publicDir = uiDir </> "public"
    CreateDir publicDir
    let publicJsDir = publicDir </> "js"
    CreateDir publicJsDir
    CopyFiles publicJsDir !! @".\src\ui\public\js\*.js"
    let publicStyleDir = publicDir </> "style"
    CreateDir publicStyleDir
    CopyFiles publicStyleDir !! @".\src\ui\public\style\*.css" 
    let publicResourcesDir = publicDir </> "resources"
    CreateDir publicResourcesDir
    CopyFiles publicResourcesDir !! @".\src\ui\public\resources\*.*"

do if not isWindows then
    // We have to set the FrameworkPathOverride so that dotnet SDK invocations know where to look for full-framework base class libraries.
    let mono = platformTool "mono" "mono"
    let frameworkPath = IO.Path.GetDirectoryName (mono) </> ".." </> "lib" </> "mono" </> "4.5"
    setEnvironVar "FrameworkPathOverride" frameworkPath

Target "install-dot-net-core" (fun _ -> dotnetExePath <- DotNetCli.InstallDotNetSDK dotnetcliVersion)

Target "clean" (fun _ ->
    CleanDir (serverDir </> "bin")
    DeleteFiles !! @".\src\server\obj\*.nuspec"
    CleanDir (uiDir </> "bin")
    DeleteFiles !! @".\src\ui\obj\*.nuspec"
    CleanDir (uiDir </> "public"))

Target "copy-resources" (fun _ ->
    let publicResourcesDir = uiDir </> @"public\resources"
    CreateDir publicResourcesDir
    // TODO-NMB-LOW: Get "favicon" working in Edge (cf. .\src\ui\index.html)?... CopyFiles publicResourcesDir !! @".\src\resources\ico\*.*"
    CopyFiles publicResourcesDir !! @".\src\resources\images\*.*")

Target "install-server" (fun _ -> runDotnet serverDir "restore")

Target "install-ui-local" (fun _ -> installUi (Some (platformTool "yarn" "yarn.cmd")))

(* Note: Requires yarn to have been installed "globally" [via npm] to D:\home\tools as per https://github.com/projectkudu/kudu/issues/2176#issuecomment-328158475, e.g.:
            - Kudu Services (https://sweepstake-2018.scm.azurewebsites.net/)...
            - ...Debug console -> CMD...
            - ...D:\home> npm config set prefix "D:\home\tools"...
            - ...D:\home> npm i g yarn@1.5.1...
            - ...D:\home> D:\home\tools\yarn.cmd --version *)
// TEMP-NMB: Skip running yarn until resolve "Error while running 'D:\home\tools\yarn.cmd' with args: --version" exception (note: can always run manually via Kudu Services as required)...
Target "install-ui-azure" (fun _ -> installUi None)
// ...or not...
//Target "install-ui-azure" (fun _ -> installUi (Some @"D:\home\tools\yarn.cmd"))
// ...NMB-TEMP

Target "install-local" DoNothing
Target "install-azure" DoNothing

Target "build-local" (fun _ -> build "-p --verbose") // note: need something to distinguish between "local" and "azure" production builds in webpack.config.js
Target "build-azure" (fun _ -> build "-p")

Target "run" (fun _ ->
    let server = async { runDotnet serverDir "watch run" }
    let ui = async { runDotnet uiDir "fable webpack-dev-server" }
    let openBrowser = async {
        do! Async.Sleep 5000
        Diagnostics.Process.Start "http://localhost:8080" |> ignore }
    Async.Parallel [| server ; ui ; openBrowser |] |> Async.RunSynchronously |> ignore)

Target "clean-publish-local" (fun _ -> CleanDir publishDir)
Target "clean-publish-azure" (fun _ -> CleanDir publishDir)

Target "publish-local" (fun _ -> publish ())

Target "publish-and-stage-azure" (fun _ ->
    publish ()
    stageFolder (Path.GetFullPath publishDir) (fun _ -> true))

Target "publish-azure" kuduSync

Target "help" (fun _ ->
    printfn "\nThe following build targets are defined:"
    printfn "\n\tbuild ... builds [Release] server and ui [which writes output to .\\src\\ui\\public]"
    printfn "\tbuild run ... builds and runs [Debug] server and ui [using webpack dev-server]"
    printfn "\tbuild publish-local ... builds [Release] server and ui, then copies output to .\\publish\n"
    printfn "\tbuild publish-azure ... builds [Release] server and ui, then uses Kudu to deploy to Azure...\n")

"install-dot-net-core" ==> "install-server" ==> "install-local"
"install-server" ==> "install-azure"
"install-dot-net-core" ==> "install-ui-local" ==> "install-local"
"install-dot-net-core" ==> "install-ui-azure" ==> "install-azure"
"clean" ==> "install-server"
"clean" ==> "copy-resources" ==> "install-ui-local"
"copy-resources" ==> "install-ui-azure"
"install-local" ==> "run"
"install-local" ==> "build-local" ==> "publish-local"
"clean-publish-local" ==> "publish-local"
"install-azure" ==> "build-azure" ==> "publish-and-stage-azure" ==> "publish-azure"
"clean-publish-azure" ==> "publish-and-stage-azure"

RunTargetOrDefault "build-local"

﻿
#if INTERACTIVE
#load "../../packages/MBrace.Runtime.0.5.13-alpha/bootstrap.fsx" 
#r "../../bin/Streams.Core.dll"
#r "../../bin/Streams.Cloud.dll"
#else 
module Nessos.Streams.Cloud.Samples.WordCount
#endif

open System
open System.IO
open System.Text.RegularExpressions
open Nessos.MBrace
open Nessos.MBrace.Client
open Nessos.Streams
open Nessos.Streams.Cloud

/// words ignored by wordcount
let noiseWords = 
    set [
        "a"; "about"; "above"; "all"; "along"; "also"; "although"; "am"; "an"; "any"; "are"; "aren't"; "as"; "at";
        "be"; "because"; "been"; "but"; "by"; "can"; "cannot"; "could"; "couldn't"; "did"; "didn't"; "do"; "does"; 
        "doesn't"; "e.g."; "either"; "etc"; "etc."; "even"; "ever";"for"; "from"; "further"; "get"; "gets"; "got"; 
        "had"; "hardly"; "has"; "hasn't"; "having"; "he"; "hence"; "her"; "here"; "hereby"; "herein"; "hereof"; 
        "hereon"; "hereto"; "herewith"; "him"; "his"; "how"; "however"; "I"; "i.e."; "if"; "into"; "it"; "it's"; "its";
        "me"; "more"; "most"; "mr"; "my"; "near"; "nor"; "now"; "of"; "onto"; "other"; "our"; "out"; "over"; "really"; 
        "said"; "same"; "she"; "should"; "shouldn't"; "since"; "so"; "some"; "such"; "than"; "that"; "the"; "their"; 
        "them"; "then"; "there"; "thereby"; "therefore"; "therefrom"; "therein"; "thereof"; "thereon"; "thereto"; 
        "therewith"; "these"; "they"; "this"; "those"; "through"; "thus"; "to"; "too"; "under"; "until"; "unto"; "upon";
        "us"; "very"; "viz"; "was"; "wasn't"; "we"; "were"; "what"; "when"; "where"; "whereby"; "wherein"; "whether";
        "which"; "while"; "who"; "whom"; "whose"; "why"; "with"; "without"; "would"; "you"; "your" ; "have"; "thou"; "will"; 
        "shall"
    ]

// Splits a string into words
let splitWords =
    let regex = new Regex(@"[\W]+", RegexOptions.Compiled)
    fun text -> regex.Split(text)

let wordTransform (word : string) = word.Trim().ToLower()

let wordFilter (word : string) = word.Length > 3 && not <| noiseWords.Contains(word)

let files = Directory.GetFiles @"C:\Users\developer001\Source\Repos\StreamsPresentation2\data"

let runtime = MBrace.InitLocal(totalNodes = 4)
let storeClient = runtime.GetStoreClient()



//
// Option 1 : CloudArrays API
//

let lines = storeClient.CreateCloudArray("tmp", files |> Seq.collect(fun f -> File.ReadLines(f)))

[<Cloud>]
let getTop count =
    lines
    |> CloudStream.ofCloudArray
    |> CloudStream.collect (fun line -> splitWords line |> Stream.ofArray |> Stream.map wordTransform)
    |> CloudStream.filter wordFilter
    |> CloudStream.countBy id
    |> CloudStream.sortBy (fun (_,c) -> -c) count
    |> CloudStream.toCloudArray

             
let proc1 = runtime.CreateProcess <@ getTop 20 @>

proc1.AwaitResult() |> Seq.iter (printfn "%A")


//
// Option 2 : CloudFiles API
//

let cfiles = storeClient.UploadFiles(files)

[<Cloud>]
let getTop' count =
    cfiles
    |> CloudStream.ofCloudFiles CloudFile.ReadLines
    |> CloudStream.collect Stream.ofSeq 
    |> CloudStream.collect (fun line -> splitWords line |> Stream.ofArray |> Stream.map wordTransform)
    |> CloudStream.filter wordFilter
    |> CloudStream.countBy id
    |> CloudStream.sortBy (fun (_,c) -> -c) count
    |> CloudStream.toCloudArray

let proc2 = runtime.CreateProcess <@ getTop' 20 @>

proc2.AwaitResult() |> Seq.iter (printfn "%A")

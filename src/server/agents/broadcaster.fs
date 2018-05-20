module Aornota.Sweepstake2018.Server.Agents.Broadcaster

// Note: Broadcaster agent broadcasts nothing itself - and subscribes to nothing.

open Aornota.Sweepstake2018.Server.Agents.ConsoleLogger
open Aornota.Sweepstake2018.Server.Signal

open System
open System.Collections.Generic

type SubscriptionId = private | SubscriptionId of guid : Guid

type SignalFilter = Signal -> bool
type LogSignalFilter = string * SignalFilter

type private BroadcasterInput =
    | Start of logSignalFilter : LogSignalFilter * reply : AsyncReplyChannel<unit>
    | Broadcast of signal : Signal
    | Subscribe of onSignal : (Signal -> unit) * reply : AsyncReplyChannel<SubscriptionId>
    | Unsubscribe of subscriptionId : SubscriptionId
    | CurrentLogSignalFilter of reply : AsyncReplyChannel<LogSignalFilter>
    | ChangeLogSignalFilter of logSignalFilter : LogSignalFilter * reply : AsyncReplyChannel<unit>

let private log category = (Broadcaster, category) |> consoleLogger.Log

let private allSignals : SignalFilter = function | _ -> true
let private allExceptTick : SignalFilter = function | Tick _ -> false | _ -> true
let private noSignals : SignalFilter = function | _ -> false

let logAllSignals : LogSignalFilter = "all signals", allSignals
let logAllSignalsExceptTick : LogSignalFilter = "all signals except Tick", allExceptTick
let logNoSignals : LogSignalFilter = "no signals", noSignals

type Broadcaster () =
    let agent = MailboxProcessor.Start (fun inbox ->
        let rec awaitingStart () = async {
            let! input = inbox.Receive ()
            match input with
            | Start ((filterName, logSignalFilter), reply) ->
                sprintf "Start when awaitingStart -> broadcasting (log signal filter: '%s')" filterName |> Info |> log
                () |> reply.Reply
                return! broadcasting (new Dictionary<SubscriptionId, Signal -> unit> (), (filterName, logSignalFilter))
            | Broadcast _ -> "Broadcast when awaitingStart" |> IgnoredInput |> Agent |> log ; return! awaitingStart ()
            | Subscribe _ -> "Subscribe when awaitingStart" |> IgnoredInput |> Agent |> log ; return! awaitingStart ()
            | Unsubscribe _ -> "Unsubscribe when awaitingStart" |> IgnoredInput |> Agent |> log ; return! awaitingStart ()
            | CurrentLogSignalFilter _ -> "CurrentLogSignalFilter when awaitingStart" |> IgnoredInput |> Agent |> log ; return! awaitingStart ()
            | ChangeLogSignalFilter _ -> "ChangeLogSignalFilter when awaitingStart" |> IgnoredInput |> Agent |> log ; return! awaitingStart () }
        and broadcasting (subscriptions, (filterName, signalFilter)) = async {
            let! input = inbox.Receive ()
            match input with
            | Start _ -> "Start when broadcasting" |> IgnoredInput |> Agent |> log ; return! broadcasting (subscriptions, (filterName, signalFilter))
            | Broadcast signal ->
                if signalFilter signal then sprintf "Broadcast -> %i subscription/s -> %A" subscriptions.Count signal |> Info |> log
                subscriptions |> List.ofSeq |> List.iter (fun (KeyValue (_, onSignal)) -> onSignal signal)
                return! broadcasting (subscriptions, (filterName, signalFilter))
            | Subscribe (onSignal, reply) ->
                let subscriptionId = Guid.NewGuid () |> SubscriptionId
                (subscriptionId, onSignal) |> subscriptions.Add
                sprintf "Subscribe when broadcasting -> added %A -> %i subscription/s" subscriptionId subscriptions.Count |> Info |> log
                subscriptionId |> reply.Reply
                return! broadcasting (subscriptions, (filterName, signalFilter))
            | Unsubscribe subscriptionId ->
                let source = "Unsubscribe"
                if subscriptionId |> subscriptions.ContainsKey then
                    subscriptionId |> subscriptions.Remove |> ignore
                    sprintf "%s when broadcasting -> removed %A -> %i subscription/s" source subscriptionId subscriptions.Count |> Info |> log
                else sprintf "%s when broadcasting (%i subscription/s) -> unknown %A" source subscriptions.Count subscriptionId |> IgnoredInput |> Agent |> log
                return! broadcasting (subscriptions, (filterName, signalFilter))
            | CurrentLogSignalFilter reply ->
                (filterName, signalFilter) |> reply.Reply
                return! broadcasting (subscriptions, (filterName, signalFilter))
            | ChangeLogSignalFilter ((filterName, logSignalFilter), reply) ->
                sprintf "ChangeLogSignalFilter when broadcasting -> broadcasting (log signal filter: '%s')" filterName |> Info |> log
                () |> reply.Reply
                return! broadcasting (subscriptions, (filterName, signalFilter)) }
        "agent instantiated -> awaitingStart" |> Info |> log
        awaitingStart ())
    do Source.Broadcaster |> logAgentException |> agent.Error.Add // note: an unhandled exception will "kill" the agent - but at least we can log the exception
    member __.Start logSignalFilter = (fun reply -> (logSignalFilter, reply) |> Start) |> agent.PostAndReply // note: not async (since need to start agents deterministically)
    member __.Broadcast signal = signal |> Broadcast |> agent.Post
    member __.SubscribeAsync onSignal = (fun reply -> (onSignal, reply) |> Subscribe) |> agent.PostAndAsyncReply
    member __.Unsubscribe subscriptionId = subscriptionId |> Unsubscribe |> agent.Post
    member __.CurrentLogFilter () = CurrentLogSignalFilter |> agent.PostAndReply
    member __.ChangeLogSignalFilter logSignalFilter = (fun reply -> (logSignalFilter, reply) |> ChangeLogSignalFilter) |> agent.PostAndReply

let broadcaster = Broadcaster ()

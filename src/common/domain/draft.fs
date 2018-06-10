module Aornota.Sweepstake2018.Common.Domain.Draft

open Aornota.Common.Revision

open Aornota.Sweepstake2018.Common.Domain.Squad
open Aornota.Sweepstake2018.Common.Domain.User

open System

type DraftId = | DraftId of guid : Guid with
    static member Create () = Guid.NewGuid () |> DraftId

type DraftOrdinal = | DraftOrdinal of draftOrdinal : int

type DraftType =
    | Constrained of starts : DateTimeOffset * ends : DateTimeOffset
    | Unconstrained

type DraftStatus =
    | PendingOpen of starts : DateTimeOffset * ends : DateTimeOffset
    | Opened of ends : DateTimeOffset
    | PendingProcessing of processingStarted : bool
    | Processed
    | PendingFreeSelection
    | FreeSelection

type DraftPick =
    | TeamPicked of squadId : SquadId
    | PlayerPicked of squadId : SquadId * playerId : PlayerId

type DraftDto = { DraftId : DraftId ; Rvn : Rvn ; DraftOrdinal : DraftOrdinal ; DraftStatus : DraftStatus }

type UserDraftPick =
    | TeamPick of squadId : SquadId
    | PlayerPick of squadId : SquadId * playerId : PlayerId

type PriorityChange = | Increase | Decrease

type UserDraftKey = UserId * DraftId

type UserDraftPickDto = { UserDraftPick : UserDraftPick ; Rank : int }
type CurrentUserDraftDto = { UserDraftKey : UserDraftKey ; Rvn : Rvn ; UserDraftPickDtos : UserDraftPickDto list }

type UserDraftSummaryDto = { UserDraftKey : UserDraftKey ; PickCount : int }

let [<Literal>] MAX_TEAM_PICKS = 1
let [<Literal>] MAX_GOALKEEPER_PICKS = 1
let [<Literal>] MAX_OUTFIELD_PLAYER_PICKS = 10

let draftTextLower (DraftOrdinal draftOrdinal) =
    if draftOrdinal = 1 then "first draft"
    else if draftOrdinal = 2 then "second draft"
    else if draftOrdinal = 4 then "third draft"
    else sprintf "draft #%i" draftOrdinal

let defaultDraftStatus draftType = match draftType with | Constrained (starts, ends) -> (starts, ends) |> PendingOpen | Unconstrained -> PendingFreeSelection

let isActive draftStatus = match draftStatus with | Opened _ | PendingProcessing _ -> true | PendingOpen _ | Processed | PendingFreeSelection | FreeSelection -> false

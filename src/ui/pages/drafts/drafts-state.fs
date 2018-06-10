module Aornota.Sweepstake2018.UI.Pages.Drafts.State

open Aornota.Common.IfDebug
open Aornota.Common.Revision
open Aornota.Common.UnexpectedError

open Aornota.UI.Common.Notifications
open Aornota.UI.Common.ShouldNeverHappen
open Aornota.UI.Common.Toasts

open Aornota.Sweepstake2018.Common.Domain.Draft
open Aornota.Sweepstake2018.Common.Domain.User
open Aornota.Sweepstake2018.Common.WsApi.ServerMsg
open Aornota.Sweepstake2018.Common.WsApi.UiMsg
open Aornota.Sweepstake2018.UI.Pages.Drafts.Common
open Aornota.Sweepstake2018.UI.Shared

open Elmish

let initialize () = { RemovalPending = None ; ChangePriorityPending = None ; LastPriorityChanged = None }, Cmd.none

let private shouldNeverHappenCmd debugText = debugText |> shouldNeverHappenText |> debugDismissableMessage |> AddNotificationMessage |> Cmd.ofMsg

let private handleServerDraftsMsg serverDraftsMsg (squadDic:SquadDic) state : State * Cmd<Input> =
    match serverDraftsMsg with
    | ChangePriorityCmdResult (Ok _) -> // note: nothing to do here
        state, Cmd.none
    | ChangePriorityCmdResult (Error (userDraftPick, error)) ->
        match state.ChangePriorityPending with
        | Some (pendingPick, _, _) when pendingPick = userDraftPick ->
            let errorText = ifDebug (sprintf "ChangePriorityCmdResult error -> %A" error) (error |> cmdErrorText)
            let errorCmd = errorText |> dangerDismissableMessage |> AddNotificationMessage |> Cmd.ofMsg
            let errorToastCmd = UNEXPECTED_ERROR |> errorToastCmd
            { state with ChangePriorityPending = None }, Cmd.batch [ errorCmd ; errorToastCmd ]
        | Some _ | None -> state, Cmd.none
    | ServerDraftsMsg.RemoveFromDraftCmdResult (Ok userDraftPick) ->
        state, sprintf "<strong>%s</strong> has been removed from draft" (userDraftPick |> userDraftPickText squadDic) |> successToastCmd
    | ServerDraftsMsg.RemoveFromDraftCmdResult (Error (userDraftPick, error)) ->
        match state.RemovalPending with
        | Some (pendingPick, _) when pendingPick = userDraftPick ->
            let errorText = ifDebug (sprintf "ServerDraftsMsg.RemoveFromDraftCmdResult error -> %A" error) (error |> cmdErrorText)
            let errorCmd = errorText |> dangerDismissableMessage |> AddNotificationMessage |> Cmd.ofMsg
            let errorToastCmd = sprintf "Unable to remove <strong>%s</strong> from draft" (userDraftPick |> userDraftPickText squadDic) |> errorToastCmd
            { state with RemovalPending = None }, Cmd.batch [ errorCmd ; errorToastCmd ]
        | Some _ | None -> state, Cmd.none

let transition input (authUser:AuthUser option) (squadsProjection:Projection<_ * SquadDic>) (currentUserDraftDto:CurrentUserDraftDto option) state =
    let state, cmd, isUserNonApiActivity =
        match input, squadsProjection with
        | AddNotificationMessage _, _ -> // note: expected to be handled by Program.State.transition
            state, Cmd.none, false
        | SendUiAuthMsg _, Ready _ -> // note: expected to be handled by Program.State.transition
            state, Cmd.none, false
        | ReceiveServerDraftsMsg serverDraftsMsg, Ready (_, squadDic) ->
            let state, cmd = state |> handleServerDraftsMsg serverDraftsMsg squadDic
            state, cmd, false
        | ChangePriority (draftId, userDraftPick, priorityChange), Ready _ ->
            match authUser, state.RemovalPending, state.ChangePriorityPending with
            | Some authUser, None, None ->
                let isPickedRvn =
                    match currentUserDraftDto with
                    | Some currentUserDraftDto ->
                        if currentUserDraftDto.UserDraftPickDtos |> List.exists (fun userDraftPickDto -> userDraftPickDto.UserDraftPick = userDraftPick) then
                            currentUserDraftDto.Rvn |> Some
                        else None
                    | None -> None
                match isPickedRvn with
                | Some rvn ->
                    let cmd = (authUser.UserId, draftId, rvn, userDraftPick, priorityChange) |> UiAuthDraftsMsg.ChangePriorityCmd |> UiAuthDraftsMsg |> SendUiAuthMsg |> Cmd.ofMsg
                    let changePriorityPending = (userDraftPick, priorityChange, rvn |> incrementRvn)
                    { state with ChangePriorityPending = changePriorityPending |> Some }, cmd, true
                | None -> state, UNEXPECTED_ERROR |> errorToastCmd, true
            | _ -> // note: should never happen
                state, Cmd.none, false
        | RemoveFromDraft (draftId, userDraftPick), Ready _ ->
            match authUser, state.RemovalPending, state.ChangePriorityPending with
            | Some authUser, None, None ->
                let isPickedRvn =
                    match currentUserDraftDto with
                    | Some currentUserDraftDto ->
                        if currentUserDraftDto.UserDraftPickDtos |> List.exists (fun userDraftPickDto -> userDraftPickDto.UserDraftPick = userDraftPick) then
                            currentUserDraftDto.Rvn |> Some
                        else None
                    | None -> None
                match isPickedRvn with
                | Some rvn ->
                    let cmd = (authUser.UserId, draftId, rvn, userDraftPick) |> UiAuthDraftsMsg.RemoveFromDraftCmd |> UiAuthDraftsMsg |> SendUiAuthMsg |> Cmd.ofMsg
                    let removalPending = (userDraftPick, rvn |> incrementRvn)
                    { state with RemovalPending = removalPending |> Some }, cmd, true
                | _ -> state, UNEXPECTED_ERROR |> errorToastCmd, true
            | _ -> // note: should never happen
                state, Cmd.none, false
        | _, _ ->
            state, shouldNeverHappenCmd (sprintf "Unexpected Input when %A -> %A" squadsProjection input), false
    state, cmd, isUserNonApiActivity

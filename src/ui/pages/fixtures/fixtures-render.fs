module Aornota.Sweepstake2018.UI.Pages.Fixtures.Render

open Aornota.Common.IfDebug
open Aornota.Common.UnitsOfMeasure

open Aornota.UI.Common.LazyViewOrHMR
open Aornota.UI.Common.ShouldNeverHappen
open Aornota.UI.Common.TimestampHelper
open Aornota.UI.Render.Bulma
open Aornota.UI.Render.Common
open Aornota.UI.Theme.Common
open Aornota.UI.Theme.Render.Bulma
open Aornota.UI.Theme.Shared

open Aornota.Sweepstake2018.Common.Domain.Core
open Aornota.Sweepstake2018.Common.Domain.Squad
open Aornota.Sweepstake2018.Common.Domain.User
open Aornota.Sweepstake2018.Common.Domain.Fixture
open Aornota.Sweepstake2018.UI.Pages.Fixtures.Common

open System

module Rct = Fable.Helpers.React

let private filterTabs currentFixtureFilter dispatch =
    let isActive filter =
        match filter with
        | AllFixtures -> currentFixtureFilter = AllFixtures
        | GroupFixtures _ -> match currentFixtureFilter with | GroupFixtures _ -> true | AllFixtures | KnockoutFixtures -> false
        | KnockoutFixtures -> currentFixtureFilter = KnockoutFixtures
    let filterText filter = match filter with | AllFixtures -> "All" | GroupFixtures _ -> "Group" | KnockoutFixtures -> "Knockout"
    let onClick filter =
        match filter with
        | AllFixtures -> (fun _ -> ShowAllFixtures |> dispatch )
        | GroupFixtures _ -> (fun _ -> None |> ShowGroupFixtures |> dispatch )
        | KnockoutFixtures -> (fun _ -> ShowKnockoutFixtures |> dispatch )
    let filters = [ AllFixtures ; GroupA |> GroupFixtures ; KnockoutFixtures ]
    filters |> List.map (fun filter -> { IsActive = filter |> isActive ; TabText = filter |> filterText ; TabLinkType = ClickableLink (filter |> onClick) } )

let private groupTabs currentFixtureFilter dispatch =
    let groupTab currentGroup dispatch group =
        { IsActive = group = currentGroup ; TabText = group |> groupText ; TabLinkType = ClickableLink (fun _ -> group |> Some |> ShowGroupFixtures |> dispatch ) }
    match currentFixtureFilter with
    | GroupFixtures currentGroup -> groups |> List.map (groupTab currentGroup dispatch)
    | AllFixtures | KnockoutFixtures -> []

let private startsIn (_timestamp:DateTime) : Fable.Import.React.ReactElement option * bool =
#if TICK
    let startsIn, imminent = _timestamp |> startsIn
    (if imminent then bold startsIn else str startsIn) |> Some, imminent
#else
    None, false
#endif

let private renderFixtures (useDefaultTheme, currentFixtureFilter, fixtureDic:FixtureDic, authUser) dispatch = // TODO-SOON?: Enable ShowConfirmParticipantModal link in release builds...
    let theme = getTheme useDefaultTheme
    let matchesFilter fixture =
        match currentFixtureFilter with
        | AllFixtures -> true
        | GroupFixtures currentGroup -> match fixture.Stage with | Group group -> group = currentGroup | RoundOf16 _ | QuarterFinal _ | SemiFinal _ | ThirdPlacePlayOff | Final -> false
        | KnockoutFixtures -> match fixture.Stage with | RoundOf16 _ | QuarterFinal _ | SemiFinal _ | ThirdPlacePlayOff | Final -> true | Group _ -> false
    let canConfirmParticipant =
        match authUser with
        | Some authUser ->
            match authUser.Permissions.FixturePermissions with
            | Some fixturePermissions -> fixturePermissions.ConfirmFixturePermission
            | None -> false
        | None -> false
    let confirmParticipant role participantDto fixtureId =
        match participantDto with
        | ConfirmedDto _ -> None
        | UnconfirmedDto _ ->
            if canConfirmParticipant then
                let paraConfirm = match role with | Home -> { paraDefaultSmallest with ParaAlignment = RightAligned } | Away -> paraDefaultSmallest
                let onClick = (fun _ -> (fixtureId, role) |> ShowConfirmParticipantModal |> dispatch)
                let confirmParticipant = [ [ str "Confirm participant" ] |> para theme paraConfirm ] |> link theme (ClickableLink onClick)
                ifDebug (confirmParticipant |> Some) None
            else None
    let stageText stage =
        let stageText =
            match stage with
            | Group group -> match currentFixtureFilter with | GroupFixtures _ -> None | AllFixtures | KnockoutFixtures -> group |> groupText |> Some
            | RoundOf16 matchNumber -> sprintf "Match %i" matchNumber |> Some
            | QuarterFinal quarterFinalOrdinal -> sprintf "Quarter-final %i" quarterFinalOrdinal |> Some
            | SemiFinal semiFinalOrdinal -> sprintf "Semi-final %i" semiFinalOrdinal |> Some
            | ThirdPlacePlayOff -> "Third/fourth place play-off" |> Some
            | Final -> "Final" |> Some
        match stageText with | Some stageText -> [ str stageText ] |> para theme paraDefaultSmallest |> Some | None -> None
    let participantText participantDto =
        let unconfirmedText unconfirmed =
            match unconfirmed with
            | Winner (Group group) -> sprintf "%s winner" (group |> groupText)
            | Winner (RoundOf16 matchNumber) -> sprintf "Match %i winner" matchNumber
            | Winner (QuarterFinal quarterFinalOrdinal) -> sprintf "Quarter-final %i winner" quarterFinalOrdinal
            | Winner (SemiFinal semiFinalOrdinal) -> sprintf "Semi-final %i winner" semiFinalOrdinal
            | Winner (ThirdPlacePlayOff) | Winner (Final) -> SHOULD_NEVER_HAPPEN
            | RunnerUp group -> sprintf "%s runner-up" (group |> groupText)
            | Loser semiFinalOrdinal -> sprintf "Semi-final %i loser" semiFinalOrdinal
        match participantDto with | ConfirmedDto (_, SquadName squadName) -> squadName | UnconfirmedDto unconfirmed -> unconfirmed |> unconfirmedText
    let extra (local:DateTime) =
        let extra, imminent = if local < DateTime.Now then italic "Result pending" |> Some, true else local |> startsIn
        let paraExtra = { paraDefaultSmallest with ParaAlignment = RightAligned ; ParaColour = GreyscalePara Grey }
        let paraExtra = if imminent then { paraExtra with ParaColour = GreyscalePara GreyDarker } else paraExtra
        match extra with | Some extra -> [ extra ] |> para theme paraExtra |> Some | None -> None
    let fixtureRow (fixtureId, fixture) =
        tr false [
            td [ [ str (fixture.KickOff.LocalDateTime |> dateText) ] |> para theme paraDefaultSmallest ]
            td [ [ str (fixture.KickOff.LocalDateTime.ToString ("HH:mm")) ] |> para theme paraDefaultSmallest ]
            td [ Rct.ofOption (stageText fixture.Stage) ]
            td [ Rct.ofOption (confirmParticipant Home fixture.HomeParticipantDto fixtureId) ]
            td [ [ str (fixture.HomeParticipantDto |> participantText) ] |> para theme { paraDefaultSmallest with ParaAlignment = RightAligned } ]
            td [ [ str "vs." ] |> para theme paraDefaultSmallest ]
            td [ [ str (fixture.AwayParticipantDto |> participantText) ] |> para theme paraDefaultSmallest ]
            td [ Rct.ofOption (confirmParticipant Away fixture.AwayParticipantDto fixtureId) ]
            td [ Rct.ofOption (fixture.KickOff.LocalDateTime |> extra) ] ]   
    let fixtures =
        fixtureDic
        |> List.ofSeq
        |> List.map (fun (KeyValue (fixtureId, fixture)) -> (fixtureId, fixture))
        |> List.filter (fun (_, fixture) -> fixture |> matchesFilter)
        |> List.sortBy (fun (_, fixture) -> fixture.KickOff)
    let fixtureRows = fixtures |> List.map (fun (fixtureId, fixture) -> (fixtureId, fixture) |> fixtureRow)
    div divCentred [
        yield table theme false { tableDefault with IsNarrow = true } [
            thead [ 
                tr false [
                    th [ [ bold "Date" ] |> para theme paraDefaultSmallest ]
                    th [ [ bold "Time" ] |> para theme paraDefaultSmallest ]
                    th []
                    th []
                    th []
                    th []
                    th []
                    th []
                    th [] ] ]
            tbody [ yield! fixtureRows ] ] ]    

let render (useDefaultTheme, state, authUser:AuthUser option, _hasModal, _:int<tick>) dispatch =
    let theme = getTheme useDefaultTheme
    columnContent [
        yield [ bold "Fixtures" ] |> para theme paraCentredSmall
        yield hr theme false
        match state.ProjectionState with
        | Initializing _ ->
            yield div divCentred [ icon iconSpinnerPulseLarge ]
        | InitializationFailed -> // note: should never happen
            yield [ str "This functionality is not currently available" ] |> para theme { paraCentredSmallest with ParaColour = SemanticPara Danger ; Weight = Bold }
        | Active activeState ->
            let fixtureDic = activeState.FixturesProjection.FixtureDic
            let currentFixtureFilter = activeState.CurrentFixtureFilter
            let filterTabs = filterTabs currentFixtureFilter dispatch
            let groupTabs = groupTabs currentFixtureFilter dispatch
            yield div divCentred [ tabs theme { tabsDefault with TabsSize = Normal ; Tabs = filterTabs } ]
            match groupTabs with
            | _ :: _ ->
                yield div divCentred [ tabs theme { tabsDefault with Tabs = groupTabs } ]
            | [] -> ()
            yield br
            yield lazyViewOrHMR2 renderFixtures (useDefaultTheme, currentFixtureFilter, fixtureDic, authUser) dispatch ]

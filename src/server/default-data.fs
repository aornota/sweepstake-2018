module Aornota.Sweepstake2018.Server.DefaultData

open Aornota.Common.IfDebug
open Aornota.Common.Revision

open Aornota.Server.Common.Helpers

open Aornota.Sweepstake2018.Common.Domain.Core
open Aornota.Sweepstake2018.Common.Domain.Draft
open Aornota.Sweepstake2018.Common.Domain.Fixture
open Aornota.Sweepstake2018.Common.Domain.Squad
open Aornota.Sweepstake2018.Common.Domain.User
open Aornota.Sweepstake2018.Common.WsApi.ServerMsg
open Aornota.Sweepstake2018.Server.Agents.ConsoleLogger
open Aornota.Sweepstake2018.Server.Agents.Entities.Drafts
open Aornota.Sweepstake2018.Server.Agents.Entities.Fixtures
open Aornota.Sweepstake2018.Server.Agents.Entities.Squads
open Aornota.Sweepstake2018.Server.Agents.Entities.Users
open Aornota.Sweepstake2018.Server.Agents.Persistence
open Aornota.Sweepstake2018.Server.Authorization

open System
open System.IO

let private deleteExistingUsersEvents = ifDebug false false // note: should *not* generally set to true for Release (and only with caution for Debug!)
let private deleteExistingSquadsEvents = ifDebug false false // note: should *not* generally set to true for Release (and only with caution for Debug!)
let private deleteExistingFixturesEvents = ifDebug false false // note: should *not* generally set to true for Release (and only with caution for Debug!)
let private deleteExistingDraftsEvents = ifDebug false false // note: should *not* generally set to true for Release (and only with caution for Debug!)

let private log category = (Host, category) |> consoleLogger.Log

let private logResult shouldSucceed scenario result =
    match shouldSucceed, result with
    | true, Ok _ -> sprintf "%s -> succeeded (as expected)" scenario |> Verbose |> log
    | true, Error error -> sprintf "%s -> unexpectedly failed -> %A" scenario error |> Danger |> log
    | false, Ok _ -> sprintf "%s -> unexpectedly succeeded" scenario |> Danger |> log
    | false, Error error -> sprintf "%s -> failed (as expected) -> %A" scenario error |> Verbose |> log
let private logShouldSucceed scenario result = result |> logResult true scenario
let private logShouldFail scenario result = result |> logResult false scenario

let private delete dir =
    Directory.GetFiles dir |> Array.iter File.Delete
    Directory.Delete dir

let private ifToken fCmdAsync token = async { return! match token with | Some token -> token |> fCmdAsync | None -> NotAuthorized |> AuthCmdAuthznError |> Error |> thingAsync }

let private superUser = SuperUser
let private nephId = Guid.Empty |> UserId
let private nephTokens = permissions nephId superUser |> UserTokens

// #region SquadIds
let private egyptId = Guid "00000011-0000-0000-0000-000000000000" |> SquadId
let private russiaId = Guid "00000012-0000-0000-0000-000000000000" |> SquadId
let private saudiArabiaId = Guid "00000013-0000-0000-0000-000000000000" |> SquadId
let private uruguayId = Guid "00000014-0000-0000-0000-000000000000" |> SquadId
let private iranId = Guid "00000021-0000-0000-0000-000000000000" |> SquadId
let private moroccoId = Guid "00000022-0000-0000-0000-000000000000" |> SquadId
let private portugalId = Guid "00000023-0000-0000-0000-000000000000" |> SquadId
let private spainId = Guid "00000024-0000-0000-0000-000000000000" |> SquadId
let private australiaId = Guid "00000031-0000-0000-0000-000000000000" |> SquadId
let private denmarkId = Guid "00000032-0000-0000-0000-000000000000" |> SquadId
let private franceId = Guid "00000033-0000-0000-0000-000000000000" |> SquadId
let private peruId = Guid "00000034-0000-0000-0000-000000000000" |> SquadId
let private argentinaId = Guid "00000041-0000-0000-0000-000000000000" |> SquadId
let private croatiaId = Guid "00000042-0000-0000-0000-000000000000" |> SquadId
let private icelandId = Guid "00000043-0000-0000-0000-000000000000" |> SquadId
let private nigeriaId = Guid "00000044-0000-0000-0000-000000000000" |> SquadId
let private brazilId = Guid "00000051-0000-0000-0000-000000000000" |> SquadId
let private costaRicaId = Guid "00000052-0000-0000-0000-000000000000" |> SquadId
let private serbiaId = Guid "00000053-0000-0000-0000-000000000000" |> SquadId
let private switzerlandId = Guid "00000054-0000-0000-0000-000000000000" |> SquadId
let private germanyId = Guid "00000061-0000-0000-0000-000000000000" |> SquadId
let private mexicoId = Guid "00000062-0000-0000-0000-000000000000" |> SquadId
let private southKoreaId = Guid "00000063-0000-0000-0000-000000000000" |> SquadId
let private swedenId = Guid "00000064-0000-0000-0000-000000000000" |> SquadId
let private belgiumId = Guid "00000071-0000-0000-0000-000000000000" |> SquadId
let private englandId = Guid "00000072-0000-0000-0000-000000000000" |> SquadId
let private panamaId = Guid "00000073-0000-0000-0000-000000000000" |> SquadId
let private tunisiaId = Guid "00000074-0000-0000-0000-000000000000" |> SquadId
let private colombiaId = Guid "00000081-0000-0000-0000-000000000000" |> SquadId
let private japanId = Guid "00000082-0000-0000-0000-000000000000" |> SquadId
let private polandId = Guid "00000083-0000-0000-0000-000000000000" |> SquadId
let private senegalId = Guid "00000084-0000-0000-0000-000000000000" |> SquadId
// #endregion

let private createInitialUsersEventsIfNecessary = async {
    let usersDir = directory EntityType.Users

    // #region: Force re-creation of initial User/s events if directory already exists (if requested)
    if deleteExistingUsersEvents && Directory.Exists usersDir then
        sprintf "deleting existing User/s events -> %s" usersDir |> Info |> log
        delete usersDir
    // #endregion

    if Directory.Exists usersDir then sprintf "preserving existing User/s events -> %s" usersDir |> Info |> log
    else
        sprintf "creating initial User/s events -> %s" usersDir |> Info |> log
        "starting Users agent" |> Info |> log
        () |> users.Start
        // Note: Send dummy OnUsersEventsRead to Users agent to ensure that it transitions [from pendingOnUsersEventsRead] to managingUsers; otherwise HandleCreateUserCmdAsync (&c.) would be ignored (and block).
        "sending dummy OnUsersEventsRead to Users agent" |> Info |> log
        [] |> users.OnUsersEventsRead

        // #region: Create initial SuperUser | Administators - and a couple of Plebs
        let neph = UserName "neph"
        let dummyPassword = Password "password"
        let! result = nephTokens.CreateUserToken |> ifToken (fun token -> (token, nephId, nephId, neph, dummyPassword, superUser) |> users.HandleCreateUserCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateUserCmdAsync (%A)" neph)
        let administrator = Administrator
        let rosieId, rosie = Guid "ffffffff-0001-0000-0000-000000000000" |> UserId, UserName "rosie"
        let! result = nephTokens.CreateUserToken |> ifToken (fun token -> (token, nephId, rosieId, rosie, dummyPassword, administrator) |> users.HandleCreateUserCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateUserCmdAsync (%A)" rosie)
        let hughId, hugh = Guid "ffffffff-0002-0000-0000-000000000000" |> UserId, UserName "hugh"
        let! result = nephTokens.CreateUserToken |> ifToken (fun token -> (token, nephId, hughId, hugh, dummyPassword, administrator) |> users.HandleCreateUserCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateUserCmdAsync (%A)" hugh)
        let pleb = Pleb
        let robId, rob = Guid "ffffffff-ffff-0001-0000-000000000000" |> UserId, UserName "rob"
        let! result = nephTokens.CreateUserToken |> ifToken (fun token -> (token, nephId, robId, rob, dummyPassword, pleb) |> users.HandleCreateUserCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateUserCmdAsync (%A)" rob)
        let joshId, josh = Guid "ffffffff-ffff-0002-0000-000000000000" |> UserId, UserName "josh"
        let! result = nephTokens.CreateUserToken |> ifToken (fun token -> (token, nephId, joshId, josh, dummyPassword, pleb) |> users.HandleCreateUserCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateUserCmdAsync (%A)" josh)
        // #endregion

        // #region: TEMP-NMB: Test various scenarios (note: expects initial SuperUser | Administrators | Plebs to have been created)...
        (*let newDummyPassword = Password "drowssap"
        let rosieTokens = permissions rosieId adminType |> UserTokens
        let willId, will = Guid "ffffffff-ffff-0003-0000-000000000000" |> UserId, UserName "will"
        let personaNonGrataId, personaNonGrata = Guid "ffffffff-ffff-ffff-0001-000000000000" |> UserId, UserName "persona non grata"
        let unknownUserId, unknownUser = Guid.NewGuid () |> UserId, UserName "unknown"
        let personaNonGrataTokens = permissions personaNonGrataId PersonaNonGrata |> UserTokens
        let unknownUserTokens = permissions unknownUserId Pleb |> UserTokens
        // Test HandleSignInCmdAsync:
        let! result = (neph, dummyPassword) |> users.HandleSignInCmdAsync
        result |> logShouldSucceed (sprintf "HandleSignInCmdAsync (%A)" neph)
        let! result = (UserName String.Empty, dummyPassword) |> users.HandleSignInCmdAsync
        result |> logShouldFail "HandleSignInCmdAsync (invalid userName: blank)"
        let! result = (UserName "bob", dummyPassword) |> users.HandleSignInCmdAsync
        result |> logShouldFail "HandleSignInCmdAsync (invalid userName: too short)"
        let! result = (neph, Password String.Empty) |> users.HandleSignInCmdAsync
        result |> logShouldFail "HandleSignInCmdAsync (invalid password: blank)"
        let! result = (neph, Password "1234") |> users.HandleSignInCmdAsync
        result |> logShouldFail "HandleSignInCmdAsync (invalid password: too short)"
        let! result = (neph, Password "PASSWORD") |> users.HandleSignInCmdAsync
        result |> logShouldFail "HandleSignInCmdAsync (incorrect password: case-sensitive)"
        let! result = (unknownUser, dummyPassword) |> users.HandleSignInCmdAsync
        result |> logShouldFail "HandleSignInCmdAsync (unknown userName)"
        let! result = nephTokens.CreateUserToken |> ifToken (fun token -> (token, nephId, personaNonGrataId, personaNonGrata, dummyPassword, PersonaNonGrata) |> users.HandleCreateUserCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateUserCmdAsync (%A)" personaNonGrata)
        let! result = (personaNonGrata, dummyPassword) |> users.HandleSignInCmdAsync
        result |> logShouldFail "HandleSignInCmdAsync (PersonaNonGrata)"
        // Test HandleChangePasswordCmdAsync:
        let! result = nephTokens.ChangePasswordToken |> ifToken (fun token -> (token, nephId, initialRvn, newDummyPassword) |> users.HandleChangePasswordCmdAsync)
        result |> logShouldSucceed (sprintf "HandleChangePasswordCmdAsync (%A)" neph)
        let! result = rosieTokens.ChangePasswordToken |> ifToken (fun token -> (token, hughId, Rvn 2, newDummyPassword) |> users.HandleChangePasswordCmdAsync)
        result |> logShouldFail "HandleChangePasswordCmdAsync (invalid ChangePasswordToken: userId differs from auditUserId)"
        let! result = personaNonGrataTokens.ChangePasswordToken |> ifToken (fun token -> (token, personaNonGrataId, Rvn 2, newDummyPassword) |> users.HandleChangePasswordCmdAsync)
        result |> logShouldFail "HandleChangePasswordCmdAsync (no ChangePasswordToken)"
        let! result = unknownUserTokens.ChangePasswordToken |> ifToken (fun token -> (token, unknownUserId, Rvn 2, newDummyPassword) |> users.HandleChangePasswordCmdAsync)
        result |> logShouldFail "HandleChangePasswordCmdAsync (unknown auditUserId)"
        let! result = nephTokens.ChangePasswordToken |> ifToken (fun token -> (token, nephId, Rvn 2, Password String.Empty) |> users.HandleChangePasswordCmdAsync)
        result |> logShouldFail "HandleChangePasswordCmdAsync (invalid password: blank)"
        let! result = nephTokens.ChangePasswordToken |> ifToken (fun token -> (token, nephId, Rvn 2, Password "1234") |> users.HandleChangePasswordCmdAsync)
        result |> logShouldFail "HandleChangePasswordCmdAsync (invalid password: too short)"
        let! result = nephTokens.ChangePasswordToken |> ifToken (fun token -> (token, nephId, Rvn 2, newDummyPassword) |> users.HandleChangePasswordCmdAsync)
        result |> logShouldFail "HandleChangePasswordCmdAsync (invalid password: same as current)"
        let! result = nephTokens.ChangePasswordToken |> ifToken (fun token -> (token, nephId, Rvn 3, Password "pa$$word") |> users.HandleChangePasswordCmdAsync)
        result |> logShouldFail "HandleChangePasswordCmdAsync (invalid current Rvn)"
        // Test HandleCreateUserCmdAsync:
        let! result = rosieTokens.CreateUserToken |> ifToken (fun token -> (token, rosieId, willId, will, dummyPassword, Pleb) |> users.HandleCreateUserCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateUserCmdAsync (%A)" will)
        let! result = rosieTokens.CreateUserToken |> ifToken (fun token -> (token, rosieId, unknownUserId, unknownUser, dummyPassword, Administrator) |> users.HandleCreateUserCmdAsync)
        result |> logShouldFail "HandleCreateUserCmdAsync (invalid CreateUserToken: UserType not allowed)"
        let! result = personaNonGrataTokens.CreateUserToken |> ifToken (fun token -> (token, personaNonGrataId, unknownUserId, unknownUser, dummyPassword, Administrator) |> users.HandleCreateUserCmdAsync)
        result |> logShouldFail "HandleCreateUserCmdAsync (no CreateUserToken)"
        let! result = nephTokens.CreateUserToken |> ifToken (fun token -> (token, nephId, unknownUserId, UserName String.Empty, dummyPassword, Administrator) |> users.HandleCreateUserCmdAsync)
        result |> logShouldFail "HandleCreateUserCmdAsync (invalid userName: blank)"
        let! result = nephTokens.CreateUserToken |> ifToken (fun token -> (token, nephId, unknownUserId, UserName "bob", dummyPassword, Administrator) |> users.HandleCreateUserCmdAsync)
        result |> logShouldFail "HandleCreateUserCmdAsync (invalid userName: too short)"
        let! result = nephTokens.CreateUserToken |> ifToken (fun token -> (token, nephId, unknownUserId, unknownUser, Password String.Empty, Administrator) |> users.HandleCreateUserCmdAsync)
        result |> logShouldFail "HandleCreateUserCmdAsync (invalid password: blank)"
        let! result = nephTokens.CreateUserToken |> ifToken (fun token -> (token, nephId, unknownUserId, unknownUser, Password "1234", Administrator) |> users.HandleCreateUserCmdAsync)
        result |> logShouldFail "HandleCreateUserCmdAsync (invalid password: too short)"
        let! result = nephTokens.CreateUserToken |> ifToken (fun token -> (token, nephId, rosieId, rosie, dummyPassword, Administrator) |> users.HandleCreateUserCmdAsync)
        result |> logShouldFail "HandleCreateUserCmdAsync (userId already exists)"
        let! result = nephTokens.CreateUserToken |> ifToken (fun token -> (token, nephId, unknownUserId, rosie, dummyPassword, Administrator) |> users.HandleCreateUserCmdAsync)
        result |> logShouldFail "HandleCreateUserCmdAsync (userName already exists)"
        // Test HandleResetPasswordCmdAsync:
        let! result = nephTokens.ResetPasswordToken |> ifToken (fun token -> (token, nephId, rosieId, initialRvn, newDummyPassword) |> users.HandleResetPasswordCmdAsync)
        result |> logShouldSucceed (sprintf "HandleResetPasswordCmdAsync (%A)" will)
        let! result = rosieTokens.ResetPasswordToken |> ifToken (fun token -> (token, rosieId, willId, initialRvn, newDummyPassword) |> users.HandleResetPasswordCmdAsync)
        result |> logShouldSucceed (sprintf "HandleResetPasswordCmdAsync (%A)" will)
        let! result = nephTokens.ResetPasswordToken |> ifToken (fun token -> (token, nephId, nephId, Rvn 2, dummyPassword) |> users.HandleResetPasswordCmdAsync)
        result |> logShouldFail "HandleResetPasswordCmdAsync (invalid ResetPasswordToken: valid UserTarget is NotSelf)"
        let! result = rosieTokens.ResetPasswordToken |> ifToken (fun token -> (token, rosieId, nephId, Rvn 2, dummyPassword) |> users.HandleResetPasswordCmdAsync)
        result |> logShouldFail "HandleResetPasswordCmdAsync (invalid ResetPasswordToken: UserType for UserTarget not allowed)"
        let! result = personaNonGrataTokens.ResetPasswordToken |> ifToken (fun token -> (token, personaNonGrataId, nephId, Rvn 2, dummyPassword) |> users.HandleResetPasswordCmdAsync)
        result |> logShouldFail "HandleResetPasswordCmdAsync (no ResetPasswordToken)"
        let! result = nephTokens.ResetPasswordToken |> ifToken (fun token -> (token, nephId, unknownUserId, initialRvn, newDummyPassword) |> users.HandleResetPasswordCmdAsync)
        result |> logShouldFail "HandleResetPasswordCmdAsync (unknown userId)"
        let! result = nephTokens.ResetPasswordToken |> ifToken (fun token -> (token, nephId, willId, Rvn 2, Password String.Empty) |> users.HandleResetPasswordCmdAsync)
        result |> logShouldFail "HandleResetPasswordCmdAsync (invalid password; blank)"
        let! result = nephTokens.ResetPasswordToken |> ifToken (fun token -> (token, nephId, willId, Rvn 2, Password "1234") |> users.HandleResetPasswordCmdAsync)
        result |> logShouldFail "HandleResetPasswordCmdAsync (invalid password; too short)"
        let! result = nephTokens.ResetPasswordToken |> ifToken (fun token -> (token, nephId, willId, Rvn 0, Password "pa$$word") |> users.HandleResetPasswordCmdAsync)
        result |> logShouldFail "HandleResetPasswordCmdAsync (invalid current Rvn)"
        // Test HandleChangeUserTypeCmdAsync:
        let! result = nephTokens.ChangeUserTypeToken |> ifToken (fun token -> (token, nephId, hughId, initialRvn, Pleb) |> users.HandleChangeUserTypeCmdAsync)
        result |> logShouldSucceed (sprintf "HandleChangeUserTypeCmdAsync (%A %A)" hugh Pleb)
        let! result = nephTokens.ChangeUserTypeToken |> ifToken (fun token -> (token, nephId, nephId, Rvn 2, Administrator) |> users.HandleChangeUserTypeCmdAsync)
        result |> logShouldFail "HandleChangeUserTypeCmdAsync (invalid ChangeUserTypeToken: valid UserTarget is NotSelf)"
        // Note: Cannot test "UserType for UserTarget not allowed" or "UserType not allowed" as only SuperUsers have ChangeUserTypePermission - and they have it for all UserTypes.
        let! result = rosieTokens.ChangeUserTypeToken |> ifToken (fun token -> (token, rosieId, nephId, Rvn 2, Administrator) |> users.HandleChangeUserTypeCmdAsync)
        result |> logShouldFail "HandleChangeUserTypeCmdAsync (no ChangeUserTypeToken)"
        let! result = nephTokens.ChangeUserTypeToken |> ifToken (fun token -> (token, nephId, unknownUserId, initialRvn, Administrator) |> users.HandleChangeUserTypeCmdAsync)
        result |> logShouldFail "HandleChangeUserTypeCmdAsync (unknown userId)"
        let! result = nephTokens.ChangeUserTypeToken |> ifToken (fun token -> (token, nephId, hughId, Rvn 2, Pleb) |> users.HandleChangeUserTypeCmdAsync)
        result |> logShouldFail "HandleChangeUserTypeCmdAsync (invalid userType: same as current)"
        let! result = nephTokens.ChangeUserTypeToken |> ifToken (fun token -> (token, nephId, hughId, initialRvn, SuperUser) |> users.HandleChangeUserTypeCmdAsync)
        result |> logShouldFail "HandleChangeUserTypeCmdAsync (invalid current Rvn)"*)
        // #endregion

        // Note: Reset Users agent [to pendingOnUsersEventsRead] so that it handles subsequent UsersEventsRead event appropriately (i.e. from readPersistedEvents).
        "resetting Users agent" |> Info |> log
        () |> users.Reset
    return () }

let private createInitialSquadsEventsIfNecessary = async {
    let squadsDir = directory EntityType.Squads

    // #region: Force re-creation of initial Squads/s events if directory already exists (if requested)
    if deleteExistingSquadsEvents && Directory.Exists squadsDir then
        sprintf "deleting existing Squad/s events -> %s" squadsDir |> Info |> log
        delete squadsDir
    // #endregion

    if Directory.Exists squadsDir then sprintf "preserving existing Squads/s events -> %s" squadsDir |> Info |> log
    else
        sprintf "creating initial Squads/s events -> %s" squadsDir |> Info |> log
        "starting Squads agent" |> Info |> log
        () |> squads.Start
        // Note: Send dummy OnSquadsEventsRead to Squads agent to ensure that it transitions [from pendingOnSquadsEventsRead] to managingSquads; otherwise HandleCreateSquadCmdAsync (&c.) would be ignored (and block).
        "sending dummy OnSquadsEventsRead to Squads agent" |> Info |> log
        [] |> squads.OnSquadsEventsRead

        // #region: Create initial Squads - and subset of Players.

        // #region: Group A
        let egypt = SquadName "Egypt"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, egyptId, egypt, GroupA, Seeding 22, CoachName "Héctor Cúper") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" egypt)
        let russia = SquadName "Russia"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, russiaId, russia, GroupA, Seeding 1, CoachName "Stanislav Cherchesov") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" russia)
        let saudiArabia = SquadName "Saudi Arabia"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, saudiArabiaId, saudiArabia, GroupA, Seeding 32, CoachName "Juan Antonio Pizzi") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" saudiArabia)
        let uruguay = SquadName "Uruguay"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, uruguayId, uruguay, GroupA, Seeding 15, CoachName "Óscar Tabárez") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" uruguay)
        // #endregion
        // #region: Group B
        let iran = SquadName "Iran"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, iranId, iran, GroupB, Seeding 24, CoachName "Carlos Queiroz") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" iran)
        let morocco = SquadName "Morocco"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, moroccoId, morocco, GroupB, Seeding 29, CoachName "Hervé Renard") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" morocco)
        let portugal = SquadName "Portugal"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, portugalId, portugal, GroupB, Seeding 4, CoachName "Fernando Santos") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" portugal)
        let spain = SquadName "Spain"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, spainId, spain, GroupB, Seeding 9, CoachName "Julen Lopetegui") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" spain)
        // #endregion
        // #region: Group C
        let australia = SquadName "Australia"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, australiaId, australia, GroupC, Seeding 27, CoachName "Bert van Marwijk") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" australia)
        let denmark = SquadName "Denmark"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, denmarkId, denmark, GroupC, Seeding 17, CoachName "Åge Hareide") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" denmark)
        let france = SquadName "France"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, franceId, france, GroupC, Seeding 8, CoachName "Didier Deschamps") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" france)
        let peru = SquadName "Peru"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, peruId, peru, GroupC, Seeding 10, CoachName "Ricardo Gareca") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateUseHandleCreateSquadCmdAsyncrCmdAsync (%A)" peru)
        // #endregion
        // #region: Group D
        let argentina = SquadName "Argentina"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, argentinaId, argentina, GroupD, Seeding 5, CoachName "Jorge Sampaoli") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" argentina)
        let croatia = SquadName "Croatia"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, croatiaId, croatia, GroupD, Seeding 16, CoachName "Zlatko Dalić") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" croatia)
        let iceland = SquadName "Iceland"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, icelandId, iceland, GroupD, Seeding 18, CoachName "Heimir Hallgrímsson") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" iceland)
        let nigeria = SquadName "Nigeria"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, nigeriaId, nigeria, GroupD, Seeding 26, CoachName "Gernot Rohr") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" nigeria)
        // #endregion
        // #region: Group E
        let brazil = SquadName "Brazil"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, brazilId, brazil, GroupE, Seeding 3, CoachName "Tite") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" brazil)
        let costaRica = SquadName "Costa Rica"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, costaRicaId, costaRica, GroupE, Seeding 19, CoachName "Óscar Ramírez") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" costaRica)
        let serbia = SquadName "Serbia"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, serbiaId, serbia, GroupE, Seeding 25, CoachName "Mladen Krstajić") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" serbia)
        let switzerland = SquadName "Switzerland"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, switzerlandId, switzerland, GroupE, Seeding 11, CoachName "Vladimir Petković") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" switzerland)
        // #endregion
        // #region: Group F
        let germany = SquadName "Germany"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, germanyId, germany, GroupF, Seeding 2, CoachName "Joachim Löw") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" germany)
        let mexico = SquadName "Mexico"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, mexicoId, mexico, GroupF, Seeding 14, CoachName "Juan Carlos Osorio") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" mexico)
        let southKorea = SquadName "South Korea"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, southKoreaId, southKorea, GroupF, Seeding 31, CoachName "Shin Tae-yong") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" southKorea)
        let sweden = SquadName "Sweden"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, swedenId, sweden, GroupF, Seeding 20, CoachName "Janne Andersson") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" sweden)
        // #endregion
        // #region: Group G
        let belgium = SquadName "Belgium"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, belgiumId, belgium, GroupG, Seeding 6, CoachName "Roberto Martínez") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" belgium)
        let england = SquadName "England"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, englandId, england, GroupG, Seeding 12, CoachName "Gareth Southgate") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" england)
        // #region: England Players
        let jackButlandId, jackButland = Guid "00000072-0001-0000-0000-000000000000" |> PlayerId, PlayerName "Jack Butland"
        let! result = nephTokens.AddOrEditPlayerToken |> ifToken (fun token -> (token, nephId, englandId, initialRvn, jackButlandId, jackButland, Goalkeeper) |> squads.HandleAddPlayerCmdAsync)
        result |> logShouldSucceed (sprintf "HandleAddPlayerCmdAsync (%A %A)" england jackButland)
        let jordanPickfordId, jordanPickford = Guid "00000072-0002-0000-0000-000000000000" |> PlayerId, PlayerName "Jordan Pickford"
        let! result = nephTokens.AddOrEditPlayerToken |> ifToken (fun token -> (token, nephId, englandId, Rvn 2, jordanPickfordId, jordanPickford, Goalkeeper) |> squads.HandleAddPlayerCmdAsync)
        result |> logShouldSucceed (sprintf "HandleAddPlayerCmdAsync (%A %A)" england jordanPickford)
        let nickPopeId, nickPope = Guid "00000072-0003-0000-0000-000000000000" |> PlayerId, PlayerName "Nick Pope"
        let! result = nephTokens.AddOrEditPlayerToken |> ifToken (fun token -> (token, nephId, englandId, Rvn 3, nickPopeId, nickPope, Goalkeeper) |> squads.HandleAddPlayerCmdAsync)
        result |> logShouldSucceed (sprintf "HandleAddPlayerCmdAsync (%A %A)" england nickPope)
        let trentAlexanderArnoldId, trentAlexanderArnold = Guid "00000072-0000-0001-0000-000000000000" |> PlayerId, PlayerName "Trent Alexander-Arnold"
        let! result = nephTokens.AddOrEditPlayerToken |> ifToken (fun token -> (token, nephId, englandId, Rvn 4, trentAlexanderArnoldId, trentAlexanderArnold, Defender) |> squads.HandleAddPlayerCmdAsync)
        result |> logShouldSucceed (sprintf "HandleAddPlayerCmdAsync (%A %A)" england trentAlexanderArnold)
        let garyCahillId, garyCahill = Guid "00000072-0000-0002-0000-000000000000" |> PlayerId, PlayerName "Gary Cahill"
        let! result = nephTokens.AddOrEditPlayerToken |> ifToken (fun token -> (token, nephId, englandId, Rvn 5, garyCahillId, garyCahill, Defender) |> squads.HandleAddPlayerCmdAsync)
        result |> logShouldSucceed (sprintf "HandleAddPlayerCmdAsync (%A %A)" england garyCahill)
        let fabianDelphId, fabianDelph = Guid "00000072-0000-0003-0000-000000000000" |> PlayerId, PlayerName "Fabian Delph"
        let! result = nephTokens.AddOrEditPlayerToken |> ifToken (fun token -> (token, nephId, englandId, Rvn 6, fabianDelphId, fabianDelph, Defender) |> squads.HandleAddPlayerCmdAsync)
        result |> logShouldSucceed (sprintf "HandleAddPlayerCmdAsync (%A %A)" england fabianDelph)
        let philJonesId, philJones = Guid "00000072-0000-0004-0000-000000000000" |> PlayerId, PlayerName "Phil Jones"
        let! result = nephTokens.AddOrEditPlayerToken |> ifToken (fun token -> (token, nephId, englandId, Rvn 7, philJonesId, philJones, Defender) |> squads.HandleAddPlayerCmdAsync)
        result |> logShouldSucceed (sprintf "HandleAddPlayerCmdAsync (%A %A)" england philJones)
        let harryMaguireId, harryMaguire = Guid "00000072-0000-0005-0000-000000000000" |> PlayerId, PlayerName "Harry Maguire"
        let! result = nephTokens.AddOrEditPlayerToken |> ifToken (fun token -> (token, nephId, englandId, Rvn 8, harryMaguireId, harryMaguire, Defender) |> squads.HandleAddPlayerCmdAsync)
        result |> logShouldSucceed (sprintf "HandleAddPlayerCmdAsync (%A %A)" england harryMaguire)
        let dannyRoseId, dannyRose = Guid "00000072-0000-0006-0000-000000000000" |> PlayerId, PlayerName "Danny Rose"
        let! result = nephTokens.AddOrEditPlayerToken |> ifToken (fun token -> (token, nephId, englandId, Rvn 9, dannyRoseId, dannyRose, Defender) |> squads.HandleAddPlayerCmdAsync)
        result |> logShouldSucceed (sprintf "HandleAddPlayerCmdAsync (%A %A)" england dannyRose)
        let johnStonesId, johnStones = Guid "00000072-0000-0007-0000-000000000000" |> PlayerId, PlayerName "John Stones"
        let! result = nephTokens.AddOrEditPlayerToken |> ifToken (fun token -> (token, nephId, englandId, Rvn 10, johnStonesId, johnStones, Defender) |> squads.HandleAddPlayerCmdAsync)
        result |> logShouldSucceed (sprintf "HandleAddPlayerCmdAsync (%A %A)" england johnStones)
        let kieranTrippierId, kieranTrippier = Guid "00000072-0000-0008-0000-000000000000" |> PlayerId, PlayerName "Kieran Trippier"
        let! result = nephTokens.AddOrEditPlayerToken |> ifToken (fun token -> (token, nephId, englandId, Rvn 11, kieranTrippierId, kieranTrippier, Defender) |> squads.HandleAddPlayerCmdAsync)
        result |> logShouldSucceed (sprintf "HandleAddPlayerCmdAsync (%A %A)" england kieranTrippier)
        let kyleWalkerId, kyleWalker = Guid "00000072-0000-0009-0000-000000000000" |> PlayerId, PlayerName "Kyle Walker"
        let! result = nephTokens.AddOrEditPlayerToken |> ifToken (fun token -> (token, nephId, englandId, Rvn 12, kyleWalkerId, kyleWalker, Defender) |> squads.HandleAddPlayerCmdAsync)
        result |> logShouldSucceed (sprintf "HandleAddPlayerCmdAsync (%A %A)" england kyleWalker)
        let ashleyYoungId, ashleyYoung = Guid "00000072-0000-0010-0000-000000000000" |> PlayerId, PlayerName "Ashley Young"
        let! result = nephTokens.AddOrEditPlayerToken |> ifToken (fun token -> (token, nephId, englandId, Rvn 13, ashleyYoungId, ashleyYoung, Defender) |> squads.HandleAddPlayerCmdAsync)
        result |> logShouldSucceed (sprintf "HandleAddPlayerCmdAsync (%A %A)" england ashleyYoung)
        let deleAlliId, deleAlli = Guid "00000072-0000-0000-0001-000000000000" |> PlayerId, PlayerName "Dele Alli"
        let! result = nephTokens.AddOrEditPlayerToken |> ifToken (fun token -> (token, nephId, englandId, Rvn 14, deleAlliId, deleAlli, Midfielder) |> squads.HandleAddPlayerCmdAsync)
        result |> logShouldSucceed (sprintf "HandleAddPlayerCmdAsync (%A %A)" england deleAlli)
        let ericDierId, ericDier = Guid "00000072-0000-0000-0002-000000000000" |> PlayerId, PlayerName "Eric Dier"
        let! result = nephTokens.AddOrEditPlayerToken |> ifToken (fun token -> (token, nephId, englandId, Rvn 15, ericDierId, ericDier, Midfielder) |> squads.HandleAddPlayerCmdAsync)
        result |> logShouldSucceed (sprintf "HandleAddPlayerCmdAsync (%A %A)" england ericDier)
        let jordanHendersonId, jordanHenderson = Guid "00000072-0000-0000-0003-000000000000" |> PlayerId, PlayerName "Jordan Henderson"
        let! result = nephTokens.AddOrEditPlayerToken |> ifToken (fun token -> (token, nephId, englandId, Rvn 16, jordanHendersonId, jordanHenderson, Midfielder) |> squads.HandleAddPlayerCmdAsync)
        result |> logShouldSucceed (sprintf "HandleAddPlayerCmdAsync (%A %A)" england jordanHenderson)
        let jesseLindgardId, jesseLindgard = Guid "00000072-0000-0000-0004-000000000000" |> PlayerId, PlayerName "Jesse Lindgard"
        let! result = nephTokens.AddOrEditPlayerToken |> ifToken (fun token -> (token, nephId, englandId, Rvn 17, jesseLindgardId, jesseLindgard, Midfielder) |> squads.HandleAddPlayerCmdAsync)
        result |> logShouldSucceed (sprintf "HandleAddPlayerCmdAsync (%A %A)" england jesseLindgard)
        let rubenLoftusCheekId, rubenLoftusCheek = Guid "00000072-0000-0000-0005-000000000000" |> PlayerId, PlayerName "Ruben Loftus-Cheek"
        let! result = nephTokens.AddOrEditPlayerToken |> ifToken (fun token -> (token, nephId, englandId, Rvn 18, rubenLoftusCheekId, rubenLoftusCheek, Midfielder) |> squads.HandleAddPlayerCmdAsync)
        result |> logShouldSucceed (sprintf "HandleAddPlayerCmdAsync (%A %A)" england rubenLoftusCheek)
        let harryKaneId, harryKane = Guid "00000072-0000-0000-0000-000000000001" |> PlayerId, PlayerName "Harry Kane"
        let! result = nephTokens.AddOrEditPlayerToken |> ifToken (fun token -> (token, nephId, englandId, Rvn 19, harryKaneId, harryKane, Forward) |> squads.HandleAddPlayerCmdAsync)
        result |> logShouldSucceed (sprintf "HandleAddPlayerCmdAsync (%A %A)" england harryKane)
        let marcusRashfordId, marcusRashford = Guid "00000072-0000-0000-0000-000000000002" |> PlayerId, PlayerName "Marcus Rashford"
        let! result = nephTokens.AddOrEditPlayerToken |> ifToken (fun token -> (token, nephId, englandId, Rvn 20, marcusRashfordId, marcusRashford, Forward) |> squads.HandleAddPlayerCmdAsync)
        result |> logShouldSucceed (sprintf "HandleAddPlayerCmdAsync (%A %A)" england marcusRashford)
        let raheemSterlingId, raheemSterling = Guid "00000072-0000-0000-0000-000000000003" |> PlayerId, PlayerName "Raheem Sterling"
        let! result = nephTokens.AddOrEditPlayerToken |> ifToken (fun token -> (token, nephId, englandId, Rvn 21, raheemSterlingId, raheemSterling, Forward) |> squads.HandleAddPlayerCmdAsync)
        result |> logShouldSucceed (sprintf "HandleAddPlayerCmdAsync (%A %A)" england raheemSterling)
        let jamieVardyId, jamieVardy = Guid "00000072-0000-0000-0000-000000000004" |> PlayerId, PlayerName "Jamie Vardy"
        let! result = nephTokens.AddOrEditPlayerToken |> ifToken (fun token -> (token, nephId, englandId, Rvn 22, jamieVardyId, jamieVardy, Forward) |> squads.HandleAddPlayerCmdAsync)
        result |> logShouldSucceed (sprintf "HandleAddPlayerCmdAsync (%A %A)" england jamieVardy)
        let dannyWelbeckId, dannyWelbeck = Guid "00000072-0000-0000-0000-000000000005" |> PlayerId, PlayerName "Danny Welbeck"
        let! result = nephTokens.AddOrEditPlayerToken |> ifToken (fun token -> (token, nephId, englandId, Rvn 23, dannyWelbeckId, dannyWelbeck, Forward) |> squads.HandleAddPlayerCmdAsync)
        result |> logShouldSucceed (sprintf "HandleAddPlayerCmdAsync (%A %A)" england dannyWelbeck)
        // #endregion
        let panama = SquadName "Panama"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, panamaId, panama, GroupG, Seeding 30, CoachName "Hernán Darío Gómez") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" panama)
        let tunisia = SquadName "Tunisia"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, tunisiaId, tunisia, GroupG, Seeding 21, CoachName "Nabil Maâloul") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateSquadCmdAsync (%A)" tunisia)
        // #endregion
        // #region: Group H
        let colombia = SquadName "Colombia"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, colombiaId, colombia, GroupH, Seeding 13, CoachName "José Pékerman") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateUserCmdAsync (%A)" colombia)
        let japan = SquadName "Japan"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, japanId, japan, GroupH, Seeding 28, CoachName "Akira Nishino") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateUserCmdAsync (%A)" japan)
        let poland = SquadName "Poland"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, polandId, poland, GroupH, Seeding 7, CoachName "Adam Nawałka") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateUserCmdAsync (%A)" poland)
        let senegal = SquadName "Senegal"
        let! result = nephTokens.CreateSquadToken |> ifToken (fun token -> (token, nephId, senegalId, senegal, GroupH, Seeding 23, CoachName "Aliou Cissé") |> squads.HandleCreateSquadCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateUserCmdAsync (%A)" senegal)
        // #endregion
        // #endregion

        // Note: Reset Squads agent [to pendingOnSquadsEventsRead] so that it handles subsequent SquadsEventsRead event appropriately (i.e. from readPersistedEvents).
        "resetting Squads agent" |> Info |> log
        () |> squads.Reset
    return () }

let private createInitialFixturesEventsIfNecessary = async {
    let fixtureId matchNumber =
        if matchNumber < 10u then sprintf "00000000-0000-0000-0000-00000000000%i" matchNumber |> Guid |> FixtureId
        else if matchNumber < 100u then sprintf "00000000-0000-0000-0000-0000000000%i" matchNumber |> Guid |> FixtureId
        else FixtureId.Create ()

    let fixturesDir = directory EntityType.Fixtures

    // #region: Force re-creation of initial Fixture/s events if directory already exists (if requested)
    if deleteExistingFixturesEvents && Directory.Exists fixturesDir then
        sprintf "deleting existing Fixture/s events -> %s" fixturesDir |> Info |> log
        delete fixturesDir
    // #endregion

    if Directory.Exists fixturesDir then sprintf "preserving existing Fixture/s events -> %s" fixturesDir |> Info |> log
    else
        sprintf "creating initial Fixture/s events -> %s" fixturesDir |> Info |> log
        "starting Fixtures agent" |> Info |> log
        () |> fixtures.Start
        // Note: Send dummy OnFixturesEventsRead to Users agent to ensure that it transitions [from pendingOnFixturesEventsRead] to managingFixtures; otherwise HandleCreateFixtureCmdAsync would be ignored (and block).
        "sending dummy OnFixturesEventsRead to Fixtures agent" |> Info |> log
        [] |> fixtures.OnFixturesEventsRead

        // #region: Group A
        let russiaVsSaudiArabiaKO = (2018, 06, 14, 15, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 1u, Group GroupA, Confirmed russiaId, Confirmed saudiArabiaId, russiaVsSaudiArabiaKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 1u)
        let egyptVsUruguayKO = (2018, 06, 15, 12, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 2u, Group GroupA, Confirmed egyptId, Confirmed uruguayId, egyptVsUruguayKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 2u)
        let russiaVsEgyptKO = (2018, 06, 19, 18, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 17u, Group GroupA, Confirmed russiaId, Confirmed egyptId, russiaVsEgyptKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 17u)
        let uruguayVsSaudiArabiaKO = (2018, 06, 20, 15, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 18u, Group GroupA, Confirmed uruguayId, Confirmed saudiArabiaId, uruguayVsSaudiArabiaKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 18u)
        let uruguayVsRussiaKO = (2018, 06, 25, 14, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 33u, Group GroupA, Confirmed uruguayId, Confirmed russiaId, uruguayVsRussiaKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 33u)
        let saudiArabiaVsEgyptKO = (2018, 06, 25, 14, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 34u, Group GroupA, Confirmed saudiArabiaId, Confirmed egyptId, saudiArabiaVsEgyptKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 34u)
        // #endregion
        // #region: Group B
        let moroccoVsIranKO = (2018, 06, 15, 15, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 4u, Group GroupB, Confirmed moroccoId, Confirmed iranId, moroccoVsIranKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 4u)
        let portugalVsSpainKO = (2018, 06, 15, 18, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 3u, Group GroupB, Confirmed portugalId, Confirmed spainId, portugalVsSpainKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 3u)
        let portugalVsMoroccoKO = (2018, 06, 20, 12, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 19u, Group GroupB, Confirmed portugalId, Confirmed moroccoId, portugalVsMoroccoKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 19u)
        let iranVsSpainKO = (2018, 06, 20, 18, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 20u, Group GroupB, Confirmed iranId, Confirmed spainId, iranVsSpainKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 20u)
        let iranVsPortugalKO = (2018, 06, 25, 18, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 35u, Group GroupB, Confirmed iranId, Confirmed portugalId, iranVsPortugalKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 35u)
        let spainVsMoroccoKO = (2018, 06, 25, 18, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 36u, Group GroupB, Confirmed spainId, Confirmed moroccoId, spainVsMoroccoKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 36u)
        // #endregion
        // #region: Group C
        let franceVsAustraliaKO = (2018, 06, 16, 10, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 5u, Group GroupC, Confirmed franceId, Confirmed australiaId, franceVsAustraliaKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 5u)
        let peruVsDenmarkKO = (2018, 06, 16, 16, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 6u, Group GroupC, Confirmed peruId, Confirmed denmarkId, peruVsDenmarkKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 6u)
        let denmarkVsAustraliaKO = (2018, 06, 21, 12, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 22u, Group GroupC, Confirmed denmarkId, Confirmed australiaId, denmarkVsAustraliaKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 22u)
        let franceVsPeruKO = (2018, 06, 21, 15, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 21u, Group GroupC, Confirmed franceId, Confirmed peruId, franceVsPeruKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 21u)
        let denmarkVsFranceKO = (2018, 06, 26, 14, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 37u, Group GroupC, Confirmed denmarkId, Confirmed franceId, denmarkVsFranceKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 37u)
        let australiaVsPeruKO = (2018, 06, 26, 14, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 38u, Group GroupC, Confirmed australiaId, Confirmed peruId, australiaVsPeruKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 38u)
        // #endregion
        // #region: Group D
        let argentinaVsIcelandKO = (2018, 06, 16, 13, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 7u, Group GroupD, Confirmed argentinaId, Confirmed icelandId, argentinaVsIcelandKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 7u)
        let croatiaVsNigeriaKO = (2018, 06, 16, 19, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 8u, Group GroupD, Confirmed croatiaId, Confirmed nigeriaId, croatiaVsNigeriaKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 8u)
        let argentinaVsCroatiaKO = (2018, 06, 21, 18, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 23u, Group GroupD, Confirmed argentinaId, Confirmed croatiaId, argentinaVsCroatiaKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 23u)
        let nigeriaVsIcelandKO = (2018, 06, 22, 15, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 24u, Group GroupD, Confirmed nigeriaId, Confirmed icelandId, nigeriaVsIcelandKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 24u)
        let nigeriaVsArgentinaKO = (2018, 06, 26, 18, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 39u, Group GroupD, Confirmed nigeriaId, Confirmed argentinaId, nigeriaVsArgentinaKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 39u)
        let icelandVsCroatiaKO = (2018, 06, 26, 18, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 40u, Group GroupD, Confirmed icelandId, Confirmed croatiaId, icelandVsCroatiaKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 40u)
        // #endregion
        // #region: Group E
        let costaRicaVsSerbiaKO = (2018, 06, 17, 12, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 10u, Group GroupE, Confirmed costaRicaId, Confirmed serbiaId, costaRicaVsSerbiaKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 10u)
        let brazilVsSwitzerlandKO = (2018, 06, 17, 18, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 9u, Group GroupE, Confirmed brazilId, Confirmed switzerlandId, brazilVsSwitzerlandKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 9u)
        let brazilVsCostRicaKO = (2018, 06, 22, 12, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 25u, Group GroupE, Confirmed brazilId, Confirmed costaRicaId, brazilVsCostRicaKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 25u)
        let serbiaVsSwitzerlandKO = (2018, 06, 22, 18, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 26u, Group GroupE, Confirmed serbiaId, Confirmed switzerlandId, serbiaVsSwitzerlandKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 26u)
        let serbiaVsBrazilKO = (2018, 06, 27, 18, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 41u, Group GroupE, Confirmed serbiaId, Confirmed brazilId, serbiaVsBrazilKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 41u)
        let switzerlandVsCostaRicaKO = (2018, 06, 27, 18, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 42u, Group GroupE, Confirmed switzerlandId, Confirmed costaRicaId, switzerlandVsCostaRicaKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 42u)
        // #endregion
        // #region: Group F
        let germanyVsMexicoKO = (2018, 06, 17, 15, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 11u, Group GroupF, Confirmed germanyId, Confirmed mexicoId, germanyVsMexicoKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 11u)
        let swedenVsSouthKoreaKO = (2018, 06, 18, 12, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 12u, Group GroupF, Confirmed swedenId, Confirmed southKoreaId, swedenVsSouthKoreaKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 12u)
        let southKoreaVsMexicoKO = (2018, 06, 23, 15, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 28u, Group GroupF, Confirmed southKoreaId, Confirmed mexicoId, southKoreaVsMexicoKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 28u)
        let germanyVsSwedenKO = (2018, 06, 23, 18, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 27u, Group GroupF, Confirmed germanyId, Confirmed swedenId, germanyVsSwedenKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 27u)
        let southKoreaVsGermanyKO = (2018, 06, 27, 14, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 43u, Group GroupF, Confirmed southKoreaId, Confirmed germanyId, southKoreaVsGermanyKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 43u)
        let mexicoVsSwedenKO = (2018, 06, 27, 14, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 44u, Group GroupF, Confirmed mexicoId, Confirmed swedenId, mexicoVsSwedenKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 44u)
        // #endregion
        // #region: Group G
        let belgiumVsPanamaKO = (2018, 06, 18, 15, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 13u, Group GroupG, Confirmed belgiumId, Confirmed panamaId, belgiumVsPanamaKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 13u)
        let tunisiaVsEnglandKO = (2018, 06, 18, 18, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 14u, Group GroupG, Confirmed tunisiaId, Confirmed englandId, tunisiaVsEnglandKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 14u)
        let belgiumVsTunisiaKO = (2018, 06, 23, 12, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 29u, Group GroupG, Confirmed belgiumId, Confirmed tunisiaId, belgiumVsTunisiaKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 29u)
        let englandVsPanamaKO = (2018, 06, 24, 12, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 30u, Group GroupG, Confirmed englandId, Confirmed panamaId, englandVsPanamaKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 30u)
        let englandVsBelgiumKO = (2018, 06, 28, 18, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 45u, Group GroupG, Confirmed englandId, Confirmed belgiumId, englandVsBelgiumKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 45u)
        let panamaVsTunisiaKO = (2018, 06, 28, 18, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 46u, Group GroupG, Confirmed panamaId, Confirmed tunisiaId, panamaVsTunisiaKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 46u)
        // #endregion
        // #region: Group H
        let colombiaVsJapanKO = (2018, 06, 19, 12, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 16u, Group GroupH, Confirmed colombiaId, Confirmed japanId, colombiaVsJapanKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 16u)
        let polandVsSenegalKO = (2018, 06, 19, 15, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 15u, Group GroupH, Confirmed polandId, Confirmed senegalId, polandVsSenegalKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 15u)
        let japanVsSenegalKO = (2018, 06, 24, 15, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 32u, Group GroupH, Confirmed japanId, Confirmed senegalId, japanVsSenegalKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 32u)
        let polandVsColombiaKO = (2018, 06, 24, 18, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 31u, Group GroupH, Confirmed polandId, Confirmed colombiaId, polandVsColombiaKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 31u)
        let japanVsPolandKO = (2018, 06, 28, 14, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 47u, Group GroupH, Confirmed japanId, Confirmed polandId, japanVsPolandKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 47u)
        let senegalVsColombiaKO = (2018, 06, 28, 14, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 48u, Group GroupH, Confirmed senegalId, Confirmed colombiaId, senegalVsColombiaKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 48u)
        // #endregion
        // #region: Round-of-16
        let winnerCVsRunnerUpDKO = (2018, 06, 30, 14, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 50u, RoundOf16 50u, Unconfirmed (Winner (Group GroupC)), Unconfirmed (RunnerUp GroupD), winnerCVsRunnerUpDKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 50u)
        let winnerAVsRunnerUpBKO = (2018, 06, 30, 18, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 49u, RoundOf16 49u, Unconfirmed (Winner (Group GroupA)), Unconfirmed (RunnerUp GroupB), winnerAVsRunnerUpBKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 49u)
        let winnerBVsRunnerUpAKO = (2018, 07, 01, 14, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 51u, RoundOf16 51u, Unconfirmed (Winner (Group GroupB)), Unconfirmed (RunnerUp GroupA), winnerBVsRunnerUpAKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 51u)
        let winnerDVsRunnerUpCKO = (2018, 07, 01, 18, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 52u, RoundOf16 52u, Unconfirmed (Winner (Group GroupD)), Unconfirmed (RunnerUp GroupC), winnerDVsRunnerUpCKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 52u)
        let winnerEVsRunnerUpFKO = (2018, 07, 02, 14, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 53u, RoundOf16 53u, Unconfirmed (Winner (Group GroupE)), Unconfirmed (RunnerUp GroupF), winnerEVsRunnerUpFKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 53u)
        let winnerGVsRunnerUpHKO = (2018, 07, 02, 18, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 54u, RoundOf16 54u, Unconfirmed (Winner (Group GroupG)), Unconfirmed (RunnerUp GroupH), winnerGVsRunnerUpHKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 54u)
        let winnerFVsRunnerUpEKO = (2018, 07, 03, 14, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 55u, RoundOf16 55u, Unconfirmed (Winner (Group GroupF)), Unconfirmed (RunnerUp GroupE), winnerFVsRunnerUpEKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 55u)
        let winnerHVsRunnerUpGKO = (2018, 07, 03, 18, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 56u, RoundOf16 56u, Unconfirmed (Winner (Group GroupH)), Unconfirmed (RunnerUp GroupG), winnerHVsRunnerUpGKO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 56u)
        // #endregion
        // #region: Quarter-finals
        let winner49VsWinner50KO = (2018, 07, 06, 14, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 57u, QuarterFinal 1u, Unconfirmed (Winner (RoundOf16 49u)), Unconfirmed (Winner (RoundOf16 50u)), winner49VsWinner50KO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 57u)
        let winner53VsWinner54KO = (2018, 07, 06, 18, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 58u, QuarterFinal 2u, Unconfirmed (Winner (RoundOf16 53u)), Unconfirmed (Winner (RoundOf16 54u)), winner53VsWinner54KO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 58u)
        let winner55VsWinner56KO = (2018, 07, 07, 14, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 59u, QuarterFinal 3u, Unconfirmed (Winner (RoundOf16 55u)), Unconfirmed (Winner (RoundOf16 56u)), winner55VsWinner56KO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 59u)
        let winner51VsWinner52KO = (2018, 07, 07, 18, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 60u, QuarterFinal 4u, Unconfirmed (Winner (RoundOf16 51u)), Unconfirmed (Winner (RoundOf16 52u)), winner51VsWinner52KO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 60u)
        // #endregion
        // #region: Semi-finals
        let winnerQF1VsWinnerQF2KO = (2018, 07, 10, 18, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 61u, SemiFinal 1u, Unconfirmed (Winner (QuarterFinal 1u)), Unconfirmed (Winner (QuarterFinal 2u)), winnerQF1VsWinnerQF2KO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 61u)
        let winnerQF3VsWinnerQF4KO = (2018, 07, 11, 18, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 62u, SemiFinal 2u, Unconfirmed (Winner (QuarterFinal 3u)), Unconfirmed (Winner (QuarterFinal 4u)), winnerQF3VsWinnerQF4KO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 62u)
        // #endregion
        // #region: Third/fourth place play-off
        let loserSF1VsLoserSF2KO = (2018, 07, 14, 14, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 63u, ThirdPlacePlayOff, Unconfirmed (Loser 1u), Unconfirmed (Loser 2u), loserSF1VsLoserSF2KO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 63u)
        // #endregion
        // #region: Final
        let winnerSF1VsWinnerSF2KO = (2018, 07, 15, 15, 00) |> dateTimeOffsetUtc
        let! result = nephTokens.CreateFixtureToken |> ifToken (fun token -> (token, nephId, fixtureId 64u, Final, Unconfirmed (Winner (SemiFinal 1u)), Unconfirmed (Winner (SemiFinal 2u)), winnerSF1VsWinnerSF2KO) |> fixtures.HandleCreateFixtureCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateFixtureCmdAsync (match %i)" 64u)
        // #endregion

        // Note: Reset Fixtures agent [to pendingOnFixturesEventsRead] so that it handles subsequent FixturesEventsRead event appropriately (i.e. from readPersistedEvents).
        "resetting Fixtures agent" |> Info |> log
        () |> fixtures.Reset
    return () }

let private createInitialDraftsEventsIfNecessary = async {
    let draftsDir = directory EntityType.Drafts

    // #region: Force re-creation of initial Draft/s events if directory already exists (if requested)
    if deleteExistingDraftsEvents && Directory.Exists draftsDir then
        sprintf "deleting existing Draft/s events -> %s" draftsDir |> Info |> log
        delete draftsDir
    // #endregion

    if Directory.Exists draftsDir then sprintf "preserving existing Draft/s events -> %s" draftsDir |> Info |> log
    else
        sprintf "creating initial Draft/s events -> %s" draftsDir |> Info |> log
        "starting Drafts agent" |> Info |> log
        () |> drafts.Start
        // Note: Send dummy OnSquadsRead | OnDraftsEventsRead | OnUserDraftsEventsRead to Drafts agent to ensure that it transitions [from pendingAllRead] to managingDrafts; otherwise HandleCreateDraftCmdAsync would be ignored (and block).
        "sending dummy OnSquadsRead | OnDraftsEventsRead | OnUserDraftsEventsRead to Drafts agent" |> Info |> log
        [] |> drafts.OnSquadsRead
        [] |> drafts.OnDraftsEventsRead
        [] |> drafts.OnUserDraftsEventsRead

        let draft1Id, draft1Ordinal = Guid "00000000-0000-0000-0000-000000000001" |> DraftId, DraftOrdinal 1
        let draft1Starts, draft1Ends = (2018, 06, 04, 22, 59) |> dateTimeOffsetUtc, (2018, 06, 09, 17, 00) |> dateTimeOffsetUtc
        let draft1Type = (draft1Starts, draft1Ends) |> Constrained
        let! result = nephTokens.DraftAdminToken |> ifToken (fun token -> (token, nephId, draft1Id, draft1Ordinal, draft1Type) |> drafts.HandleCreateDraftCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateDraftCmdAsync (%A %A)" draft1Id draft1Ordinal)
        let draft2Id, draft2Ordinal = Guid "00000000-0000-0000-0000-000000000002" |> DraftId, DraftOrdinal 2
        let draft2Starts, draft2Ends = (2018, 06, 09, 22, 59) |> dateTimeOffsetUtc, (2018, 06, 12, 17, 00) |> dateTimeOffsetUtc
        let draft2Type = (draft2Starts, draft2Ends) |> Constrained
        let! result = nephTokens.DraftAdminToken |> ifToken (fun token -> (token, nephId, draft2Id, draft2Ordinal, draft2Type) |> drafts.HandleCreateDraftCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateDraftCmdAsync (%A %A)" draft2Id draft2Ordinal)
        let draft3Id, draft3Ordinal = Guid "00000000-0000-0000-0000-000000000003" |> DraftId, DraftOrdinal 3
        let draft3Starts, draft3Ends = (2018, 06, 12, 22, 59) |> dateTimeOffsetUtc, (2018, 06, 14, 11, 00) |> dateTimeOffsetUtc
        let draft3Type = (draft3Starts, draft3Ends) |> Constrained
        let! result = nephTokens.DraftAdminToken |> ifToken (fun token -> (token, nephId, draft3Id, draft3Ordinal, draft3Type) |> drafts.HandleCreateDraftCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateDraftCmdAsync (%A %A)" draft3Id draft3Ordinal)
        let draft4Id, draft4Ordinal = Guid "00000000-0000-0000-0000-000000000004" |> DraftId, DraftOrdinal 4
        let draft4Type = Unconstrained
        let! result = nephTokens.DraftAdminToken |> ifToken (fun token -> (token, nephId, draft4Id, draft4Ordinal, draft4Type) |> drafts.HandleCreateDraftCmdAsync)
        result |> logShouldSucceed (sprintf "HandleCreateDraftCmdAsync (%A %A)" draft4Id draft4Ordinal)

        // Note: Reset Drafts agent [to pendingAllRead] so that it handles subsequent DraftsEventsRead event (&c.) appropriately (i.e. from readPersistedEvents).
        "resetting Drafts agent" |> Info |> log
        () |> drafts.Reset
    return () }

let createInitialPersistedEventsIfNecessary = async {
    "creating initial persisted events (if necessary)" |> Info |> log
    let previousLogFilter = () |> consoleLogger.CurrentLogFilter
    let customLogFilter = "createInitialPersistedEventsIfNecessary", function | Host -> allCategories | Entity _ -> allExceptVerbose | _ -> onlyWarningsAndWorse
    customLogFilter |> consoleLogger.ChangeLogFilter
    do! createInitialUsersEventsIfNecessary // note: although this can cause various events to be broadcast (UsersRead | UserEventWritten | &c.), no agents should yet be subscribed to these
    do! createInitialSquadsEventsIfNecessary // note: although this can cause various events to be broadcast (SquadsRead | SquadEventWritten | &c.), no agents should yet be subscribed to these
    do! createInitialFixturesEventsIfNecessary // note: although this can cause various events to be broadcast (FixturesRead | FixtureEventWritten | &c.), no agents should yet be subscribed to these
    // TEMP-NMB... do! createInitialDraftsEventsIfNecessary // note: although this can cause various events to be broadcast (DraftsRead | DraftEventWritten | &c.), no agents should yet be subscribed to these
    previousLogFilter |> consoleLogger.ChangeLogFilter }

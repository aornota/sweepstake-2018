module Aornota.Sweepstake2018.UI.Common

open System

open Aornota.Sweepstake2018.Shared

open Aornota.UI.Common.DebugMessages
open Aornota.UI.Theme.Dark
open Aornota.UI.Theme.Default

type Preferences = { UseDefaultTheme : bool }

type MessageType =
    | Sent of message : Message * timestamp : DateTime
    | SendFailed of message : Message * timestamp : DateTime
    | Received of message : Message * timestamp : DateTime

type Input =
    | DismissDebugMessage of debugId : DebugId
    | ToggleTheme
    | ToggleNavbarBurger
    | ReadPreferencesResult of result : Result<Preferences option, exn>
    | WritePreferencesResult of result : Result<unit, exn>
    | InitializeWsResult of result : Result<UiWs -> Async<Input>, exn>
    | WsNotInitialized of uiWs : UiWs
    | NicknameTextChanged of nicknameText : string
    | Connect
    | ConnectResult of result : Result<Connection, exn>
    | DismissMessage of messageId : MessageId
    | MessageTextChanged of messageText : string
    | SendMessage
    | SendMessageResult of Result<Message, MessageId * exn>
    | Disconnect
    | DisconnectResult of result : Result<Connection, exn>
    | SendMessageOther of message : Message
    | UserConnectedOther of nickname : string
    | UserDisconnectedOther of nickname : string

type Status =
    | ReadingPreferences
    | InitializingWS
    | ServiceUnavailable
    | NotConnected of connectionId : ConnectionId * nicknameText : string * validationErrorText : string option * connectResultErrorText : string option
    | Connecting of connection : Connection
    | Connected of connection : Connection * messages : MessageType list * messageId : MessageId * messageText : string
    | Disconnecting of connection : Connection

type State = {
    DebugMessages : DebugMessage list
    UseDefaultTheme : bool
    NavbarBurgerIsActive : bool
    Status : Status
    SendWsCmdAsync : UiWs -> Async<Input> }

let [<Literal>] SWEEPSTAKE_2018 = "sweepstake 2018 (pre-α)"

let errorText text = sprintf "ERROR -> %s" text
let shouldNeverHappenText text = sprintf "SHOULD NEVER HAPPEN -> %s" text

let getTheme useDefaultTheme = if useDefaultTheme then themeDefault else themeDark

let validateNicknameText nicknameText = if String.IsNullOrWhiteSpace nicknameText then Some "Nickname must not be blank" else None
let validateMessageText messageText = if String.IsNullOrWhiteSpace messageText then Some "Message must not be blank" else None

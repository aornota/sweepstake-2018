module Aornota.Sweepstake2018.UI.Program.State

open Aornota.Common.IfDebug
open Aornota.Common.Json
open Aornota.Common.UnexpectedError
open Aornota.Common.UnitsOfMeasure

open Aornota.UI.Common.LocalStorage
open Aornota.UI.Common.Notifications
open Aornota.UI.Common.Toasts
open Aornota.UI.Theme.Common
open Aornota.UI.Theme.Shared

open Aornota.Sweepstake2018.Common.Domain.Core
open Aornota.Sweepstake2018.Common.Literals
open Aornota.Sweepstake2018.Common.WsApi.ServerMsg
open Aornota.Sweepstake2018.Common.WsApi.UiMsg
open Aornota.Sweepstake2018.UI.Pages
open Aornota.Sweepstake2018.UI.Pages.Chat.Common
open Aornota.Sweepstake2018.UI.Program.Common

open System

open Elmish

open Fable.Core.JsInterop
open Fable.Import
module Brw = Fable.Import.Browser

let [<Literal>] private APP_PREFERENCES_KEY = "sweepstake-2018-ui-app-preferences"

let private setBodyClass useDefaultTheme = Browser.document.body.className <- getThemeClass (getTheme useDefaultTheme).ThemeClass

let private readPreferencesCmd =
    let readPreferences () = async {
        do! ifDebugSleepAsync 20 100
        return readJson (Key APP_PREFERENCES_KEY) |> Option.map (fun (Json json) -> json |> ofJson<Preferences>) }
    Cmd.ofAsync readPreferences () (Ok >> ReadingPreferencesInput >> AppInput) (Error >> ReadingPreferencesInput >> AppInput)

let private writePreferencesCmd state =
    let writePreferences uiState = async {
        let lastPage =
            match uiState.AppState with
            | Unauth unauthState -> Some (UnauthPage unauthState.CurrentUnauthPage)
            | Auth authState -> Some authState.CurrentPage
            | ReadingPreferences | Connecting _ | ServiceUnavailable | AutomaticallySigningIn _ -> None
        let jwt =
            match uiState.AppState with
            | Auth authState -> Some (Jwt (authState.AuthUser))
            | ReadingPreferences | Connecting _ | ServiceUnavailable | AutomaticallySigningIn _ | Unauth _ -> None
        let preferences = { UseDefaultTheme = uiState.UseDefaultTheme ; SessionId = uiState.SessionId ; LastPage = lastPage ; Jwt = jwt }
        do writeJson (Key APP_PREFERENCES_KEY) (Json (preferences |> toJson)) }
    Cmd.ofAsync writePreferences state (Ok >> WritePreferencesResult) (Error >> WritePreferencesResult)

let private initializeWsSub dispatch =
    let receiveServerMsg (wsMessage:Brw.MessageEvent) : unit =
        try // note: expect wsMessage.data to be deserializable to ServerMsg
            let serverMsg = unbox wsMessage.data |> ofJson<ServerMsg>
            ifDebugFakeErrorFailWith (sprintf "Fake error deserializing %A" serverMsg)
            HandleServerMsg serverMsg |> dispatch
        with exn -> WsError (DeserializeServerMsgError exn.Message) |> dispatch
    let wsUrl = ifDebug (sprintf "ws://localhost:%i" WS_PORT) "wss://sweepstake-2018.azurewebsites.net:443" // note: WS_PORT irrelevant for Azure (since effectively "internal")
    let wsApiUrl = sprintf "%s%s" wsUrl WS_API_PATH
    try
        let ws = Brw.WebSocket.Create wsApiUrl
        ws.onopen <- (fun _ -> ConnectingInput ws |> AppInput |> dispatch)
        ws.onerror <- (fun _ -> WsError (WsOnError wsApiUrl) |> dispatch)
        ws.onmessage <- receiveServerMsg
        ()
    with _ -> WsError (WsOnError wsApiUrl) |> dispatch

let private sendMsg (ws:Brw.WebSocket) (uiMsg:UiMsg) =
    if ws.readyState <> ws.OPEN then WsError (SendMsgWsNotOpenError uiMsg) |> Cmd.ofMsg
    else
        try
            ifDebugFakeErrorFailWith "Fake sendMsg error"
            ws.send (uiMsg |> toJson)
            Cmd.none
        with exn -> WsError (SendMsgOtherError (uiMsg, exn.Message)) |> Cmd.ofMsg

let private shouldNeverHappenText text = sprintf "SHOULD NEVER HAPPEN -> %s" text

let private sendUnauthMsgCmd (ws:Brw.WebSocket option) uiUnauthMsg =
    match ws with
    | Some ws -> sendMsg ws (UiUnauthMsg uiUnauthMsg)
    | None -> AddNotificationMessage (debugDismissableMessage (shouldNeverHappenText "sendUnauthMsgCmd called when WebSocket is None")) |> Cmd.ofMsg

let private sendMsgCmd (ws:Brw.WebSocket option) uiMsg =
    match ws with
    | Some ws -> sendMsg ws uiMsg
    | None -> AddNotificationMessage (debugDismissableMessage (shouldNeverHappenText "sendMsgCmd called when WebSocket is None")) |> Cmd.ofMsg

let private addNotificationMessage notificationMessage state = { state with NotificationMessages = notificationMessage :: state.NotificationMessages }

let private addDebugMessage debugText state = addNotificationMessage (debugDismissableMessage debugText) state
let private addInfoMessage infoText state = addNotificationMessage (infoDismissableMessage infoText) state
let private addWarningMessage warningText state = addNotificationMessage (warningDismissableMessage warningText) state
let private addDangerMessage dangerText state = addNotificationMessage (dangerDismissableMessage dangerText) state

let private shouldNeverHappen debugText state : State * Cmd<Input> = addDebugMessage (shouldNeverHappenText debugText) state, Cmd.none

let private addDebugError debugText toastText state : State * Cmd<Input> =
    addDebugMessage (sprintf "ERROR -> %s" debugText) state, match toastText with | Some toastText -> errorToastCmd toastText | None -> Cmd.none

let private addError errorText state = addDangerMessage errorText state

let private appStateText appState =
    match appState with
    | ReadingPreferences -> "ReadingPreferences" | Connecting _ -> "Connecting" | ServiceUnavailable -> "ServiceUnavailable" | AutomaticallySigningIn _ -> "AutomaticallySigningIn"
    | Unauth _ -> "Unauth" | Auth _ -> "Auth"

let defaultSignInState userName signInStatus = {
    UserNameKey = Guid.NewGuid ()
    UserNameText = match userName with | Some userName -> userName | None -> String.Empty
    UserNameErrorText = None
    PasswordKey = Guid.NewGuid ()
    PasswordText = String.Empty
    PasswordErrorText = None
    FocusPassword = match userName with | Some _ -> true | None -> false
    SignInStatus = signInStatus }

let private defaultUnauthState currentPage signInState state =
    let unauthState = {
        CurrentUnauthPage = match currentPage with | Some currentPage -> currentPage | None -> News
        UnauthPageStates = { NewsState = () ; SquadsState = () }
        SignInState = signInState }
    { state with AppState = Unauth unauthState }, Cmd.none

let private defaultAuthState authUser currentPage (unauthState:UnauthState option) state =
    let currentPage = match currentPage with | Some currentPage -> currentPage | None -> AuthPage ChatPage
    // Note: No actual need to call Chat.State.initialize here as will be initialized on demand - i.e. by ShowPage (AuthPage ChatPage) - but no harm in being pre-emptive.
    let chatState, chatCmd = Chat.State.initialize authUser (currentPage = AuthPage ChatPage)
    let authState = {
        AuthUser = authUser
        CurrentPage = currentPage
        UnauthPageStates = match unauthState with | Some unauthState -> unauthState.UnauthPageStates | None -> { NewsState = () ; SquadsState = () }
        AuthPageStates = { DraftsState = () ; ChatState = Some chatState }
        SigningOut = false }
    { state with AppState = Auth authState }, chatCmd |> Cmd.map (ChatInput >> APageInput >> PageInput >> AuthInput >> AppInput)

let initialize () =
    let state = {
        Ticks = 0<tick>
        NotificationMessages = []
        UseDefaultTheme = true
        SessionId = SessionId.Create ()
        NavbarBurgerIsActive = false
        StaticModal = None
        Ws = None
        AppState = ReadingPreferences }
    setBodyClass state.UseDefaultTheme
    state, readPreferencesCmd

let private handleWsError wsError state : State * Cmd<Input> =
    match wsError, state.AppState with
    | WsOnError wsApiUrl, Connecting _ ->
        let uiState = { state with AppState = ServiceUnavailable }
        addDebugError (sprintf "WsOnError when Connecting -> %s" wsApiUrl) (Some "Unable to create a connection to the web server<br><br>Please try again later") uiState
    | WsOnError wsApiUrl, _ -> addDebugError (sprintf "WsOnError not when Connecting -> %s" wsApiUrl) (Some UNEXPECTED_ERROR) state
    | SendMsgWsNotOpenError uiMsg, _ -> addDebugError (sprintf "SendMsgWsNotOpenError -> %A" uiMsg) (Some "The connection to the web server has been closed<br><br>Please try refreshing the page") state
    | SendMsgOtherError (uiMsg, errorText), _ -> addDebugError (sprintf "SendMsgOtherError -> %s -> %A" errorText uiMsg) (Some (unexpectedErrorWhen "sending a message")) state
    | DeserializeServerMsgError errorText, _ -> addDebugError (sprintf "DeserializeServerMsgError -> %s" errorText) (Some (unexpectedErrorWhen "processing a received message")) state

let private handleServerUiMsgError serverUiMsgError state =
    match serverUiMsgError with
    | ReceiveUiMsgError errorText -> addDebugError (sprintf "Server ReceiveUiMsgError -> %s" errorText) (Some "The web server was unable to receive a message<br><br>Please try refreshing the page") state
    | DeserializeUiMsgError errorText -> addDebugError (sprintf "Server DeserializeUiMsgError -> %s" errorText) (Some"The web server was unable to process a message<br><br>Please try refreshing the page") state

let private handleConnected (otherConnections, signedIn) jwt lastPage state =
    let toastCmd =
#if DEBUG
        // TEMP-NMB: Show [ other-web-socket-connection | signed-in-user ] counts (as toast)...
        let otherConnections = if otherConnections > 0 then sprintf "<strong>%i</strong>" otherConnections else sprintf "%i" otherConnections
        let signedIn = if signedIn > 0 then sprintf "<strong>%i</strong>" signedIn else sprintf "%i" signedIn
        infoToastCmd (sprintf "Other web socket connections: %s<br>Signed-in users: %s" otherConnections signedIn)
        // ...or not...
        //Cmd.none
        // ...NMB-TEMP
#else
        Cmd.none
#endif
    let state, cmd =
        match jwt with
        | Some jwt -> { state with AppState = AutomaticallySigningIn (jwt, lastPage) }, AutoSignInMsgOLD jwt |> sendUnauthMsgCmd state.Ws
        | None ->
            let lastPage = match lastPage with | Some (UnauthPage unauthPage) -> Some unauthPage | Some (AuthPage _) | None -> None
            let showPageCmd = match lastPage with | Some lastPage -> ShowUnauthPage lastPage |> UnauthInput |> AppInput |> Cmd.ofMsg | None -> Cmd.none
            // TEMP-NMB: ShowSignInModal once connected...
            let showSignInCmd =
                ShowSignInModal |> UnauthInput |> AppInput |> Cmd.ofMsg
            // ...or not...
                //Cmd.none
            // ...NMB-TEMP
            let state, cmd = defaultUnauthState None None state
            state, Cmd.batch [ cmd ; showPageCmd ; showSignInCmd ]
    state, Cmd.batch [ cmd ; toastCmd ]

let private handleSignInResult result unauthState state =
    match unauthState.SignInState, result with
    | Some _, Ok authUser ->
        let currentPage = Some (UnauthPage unauthState.CurrentUnauthPage)
        let state, cmd = defaultAuthState authUser currentPage (Some unauthState) state
        state, Cmd.batch [ cmd ; writePreferencesCmd state ; successToastCmd (sprintf "You have signed in as <strong>%s</strong>" authUser.UserName) ]
    | Some signInState, Error errorText ->
        let toastCmd = errorToastCmd (sprintf "Unable to sign in as <strong>%s</strong>" signInState.UserNameText)
        let errorText = ifDebug (sprintf "SignInResultMsg error -> %s" errorText) (unexpectedErrorWhen "signing in")
        let signInState = { signInState with SignInStatus = Some (Failed errorText) }
        { state with AppState = Unauth { unauthState with SignInState = Some signInState } }, toastCmd
    | None, _ -> shouldNeverHappen (sprintf "Unexpected SignInResultMsg when SignInState is None -> %A" result) state

let private handleAutoSignInResult result (jwt:AuthUser) lastPage state =
    match result with
    | Ok authUser -> // TODO-NMB-LOW: Check authUser vs. _jwt?...
        let showPageCmd = match lastPage with | Some lastPage -> ShowPage lastPage |> AuthInput |> AppInput |> Cmd.ofMsg | None -> Cmd.none
        let state, cmd = defaultAuthState authUser None None state
        state, Cmd.batch [ showPageCmd ; cmd ; successToastCmd (sprintf "You have been automatically signed in as <strong>%s</strong>" authUser.UserName) ]
    | Error errorText ->
        let toastCmd = errorToastCmd (sprintf "Unable to automatically sign in as <strong>%s</strong>" jwt.UserName)
        let errorText = ifDebug (sprintf "AutoSignInResultMsg error -> %s" errorText) (unexpectedErrorWhen "automatically signing in")
        let lastPage = match lastPage with | Some (UnauthPage unauthPage) -> Some unauthPage | Some (AuthPage _) | None -> None
        let signInState = defaultSignInState (Some jwt.UserName) (Some (Failed errorText))
        let showPageCmd = match lastPage with | Some lastPage -> ShowUnauthPage lastPage |> UnauthInput |> AppInput |> Cmd.ofMsg | None -> Cmd.none
        let state, cmd = defaultUnauthState None (Some signInState) state
        state, Cmd.batch [ cmd ; showPageCmd ; toastCmd ]

let private handleSignOutResult result authState state =
    let toastCmd = successToastCmd "You have signed out"
    match authState.SigningOut, result with
    | true, Ok _sessionId -> // TODO-NMB-LOW: Check _sessionId vs. authState.AuthUser.SessionId?...
        let currentPage = match authState.CurrentPage with | UnauthPage unauthPage -> Some unauthPage | AuthPage _ -> None
        let state, cmd = defaultUnauthState currentPage None state
        state, Cmd.batch [ cmd ; writePreferencesCmd state ; toastCmd ]
    | true, Error errorText ->
        let state, _ = ifDebug (addDebugError (sprintf "SignOutResultMsg error -> %s" errorText) None state) (addError (unexpectedErrorWhen "signing out") state, Cmd.none)
        let currentPage = match authState.CurrentPage with | UnauthPage unauthPage -> Some unauthPage | AuthPage _ -> None
        let state, cmd = defaultUnauthState currentPage None state
        state, Cmd.batch [ cmd ; writePreferencesCmd state ; toastCmd ]
    | false, _ -> shouldNeverHappen (sprintf "Unexpected SignOutResultMsg when not SigningOut -> %A" result) state

let private handleAutoSignOut _sessionId (authState:AuthState) state = // TODO-NMB-LOW: Check _sessionId vs. authState.AuthUser.SessionId?...
    let currentPage = match authState.CurrentPage with | UnauthPage unauthPage -> Some unauthPage | AuthPage _ -> None
    let state, cmd = defaultUnauthState currentPage None state
    state, Cmd.batch [ cmd ; writePreferencesCmd state ; warningToastCmd "You have been automatically signed out" ]

let private handleServerAppMsg serverAppMsg state =
    match serverAppMsg, state.AppState with
    | ServerUiMsgErrorMsg serverUiMsgError, _ -> handleServerUiMsgError serverUiMsgError state
    | ConnectedMsg (otherConnections, signedIn), Connecting (jwt, lastPage) -> handleConnected (otherConnections, signedIn) jwt lastPage state
    | SignInResultMsg result, Unauth unauthState -> handleSignInResult result unauthState state
    | AutoSignInResultMsg result, AutomaticallySigningIn (Jwt jwt, lastPage) -> handleAutoSignInResult result jwt lastPage state
    | SignOutResultMsg result, Auth authState -> handleSignOutResult result authState state
    | AutoSignOutMsgOLD sessionId, Auth authState -> handleAutoSignOut sessionId authState state
    | OtherUserSignedInMsgOLD userName, Auth _ -> state, infoToastCmd (sprintf "<strong>%s</strong> has signed in" userName)
    | OtherUserSignedOutMsgOLD userName, Auth _ -> state, infoToastCmd (sprintf "<strong>%s</strong> has signed out" userName)
    | _, appState -> shouldNeverHappen (sprintf "Unexpected ServerAppMsg when %s -> %A" (appStateText appState) serverAppMsg) state

let private handleServerMsg serverMsg state =
    match serverMsg, state.AppState with
    | ServerAppMsg serverAppMsg, _ -> handleServerAppMsg serverAppMsg state
    | ServerChatMsg serverChatMsg, Auth _ -> state, ReceiveServerChatMsg serverChatMsg |> ChatInput |> APageInput |> PageInput |> AuthInput |> AppInput |> Cmd.ofMsg
    | _, appState -> shouldNeverHappen (sprintf "Unexpected ServerMsg when %s -> %A" (appStateText appState) serverMsg) state

let private handleReadingPreferencesInput (result:Result<Preferences option, exn>) (state:State) =
    match result with
    | Ok (Some preferences) ->
        let state = { state with UseDefaultTheme = preferences.UseDefaultTheme ; SessionId = preferences.SessionId }
        setBodyClass state.UseDefaultTheme
        { state with AppState = Connecting (preferences.Jwt, preferences.LastPage) }, Cmd.ofSub initializeWsSub
    | Ok None -> { state with AppState = Connecting (None, None) }, Cmd.ofSub initializeWsSub
    | Error exn ->
        let state, _ = addDebugError (sprintf "ReadPreferencesResult -> %s" exn.Message) None state // note: no need for toast
        state, ReadingPreferencesInput (Ok None) |> AppInput |> Cmd.ofMsg

let private handleConnectingInput ws state : State * Cmd<Input> = { state with Ws = Some ws }, Cmd.none

let private handleUnauthInput unauthInput unauthState state =
    match unauthInput, unauthState.SignInState with
    | ShowUnauthPage unauthPage, _ ->
        if unauthState.CurrentUnauthPage <> unauthPage then
            // TODO-NMB-MEDIUM: Initialize "optional" pages (if required) and toggle "IsCurrent" for relevant pages...
            let unauthState = { unauthState with CurrentUnauthPage = unauthPage }
            let state = { state with AppState = Unauth unauthState }
            state, writePreferencesCmd state
        else state, Cmd.none
    | UnauthPageInput NewsInput, None -> shouldNeverHappen "Unexpected NewsInput -> NYI" state
    | UnauthPageInput SquadsInput, None -> shouldNeverHappen "Unexpected SquadsInput -> NYI" state
    | ShowSignInModal, None ->
        let unauthState = { unauthState with SignInState = Some (defaultSignInState None None) }
        { state with AppState = Unauth unauthState }, Cmd.none
    | SignInInput (UserNameTextChanged userNameText), Some signInState ->
        let signInState = { signInState with UserNameText = userNameText ; UserNameErrorText = validateUserNameText userNameText }
        let unauthState = { unauthState with SignInState = Some signInState }
        { state with AppState = Unauth unauthState }, Cmd.none
    | SignInInput (PasswordTextChanged passwordText), Some signInState ->
        let signInState = { signInState with PasswordText = passwordText ; PasswordErrorText = validatePasswordText passwordText }
        let unauthState = { unauthState with SignInState = Some signInState }
        { state with AppState = Unauth unauthState }, Cmd.none
    | SignInInput SignIn, Some signInState -> // note: assume no need to validate unauthState.UserNameText or unauthState.PasswordText (i.e. because App.Render.renderUnauth will ensure that SignIn can only be dispatched when valid)
        let signInState = { signInState with SignInStatus = Some Pending }
        let unauthState = { unauthState with SignInState = Some signInState }
        let cmd = SignInMsgOLD (state.SessionId, signInState.UserNameText, signInState.PasswordText) |> sendUnauthMsgCmd state.Ws
        { state with AppState = Unauth unauthState }, cmd
    | SignInInput CancelSignIn, Some _ ->
        let unauthState = { unauthState with SignInState = None }
        { state with AppState = Unauth unauthState }, Cmd.none
    | _, _ -> shouldNeverHappen (sprintf "Unexpected UnauthInput when SignIsState is %A -> %A" unauthState.SignInState unauthInput) state

let private handleAuthInput authInput authState state =
    match authInput, authState.SigningOut with
    | ShowPage page, false ->
        if authState.CurrentPage <> page then
            let chatState, chatCmd =
                match page, authState.AuthPageStates.ChatState with
                | AuthPage ChatPage, None ->
                    let chatState, chatCmd = Chat.State.initialize authState.AuthUser true
                    Some chatState, chatCmd
                | _, Some chatState -> Some chatState, ToggleChatIsCurrentPage (page = AuthPage ChatPage) |> Cmd.ofMsg
                | _, None -> None, Cmd.none
            // TODO-NMB-MEDIUM: Initialize other "optional" pages (if required) and toggle "IsCurrent" for other relevant pages...
            let authPageStates = { authState.AuthPageStates with ChatState = chatState }
            let authState = { authState with CurrentPage = page ; AuthPageStates = authPageStates }
            let chatCmd = chatCmd |> Cmd.map (ChatInput >> APageInput >> PageInput >> AuthInput >> AppInput)
            let state = { state with AppState = Auth authState }
            state, Cmd.batch [ chatCmd ; writePreferencesCmd state ]
        else state, Cmd.none
    | PageInput (UPageInput NewsInput), false -> shouldNeverHappen "Unexpected NewsInput -> NYI" state
    | PageInput (UPageInput SquadsInput), false -> shouldNeverHappen "Unexpected SquadsInput -> NYI" state
    | PageInput (APageInput DraftsInput), false -> shouldNeverHappen "Unexpected DraftsInput -> NYI" state
    | PageInput (APageInput (ChatInput ShowMarkdownSyntaxModal)), false -> { state with StaticModal = Some MarkdownSyntax }, Cmd.none
    | PageInput (APageInput (ChatInput (SendUiAuthMsg (_authUser, uiAuthMsg)))), false -> // TODO-NMB-LOW: Check _authUser vs. authState.AuthUser?...
        state, UiAuthMsg (Jwt authState.AuthUser, uiAuthMsg) |> sendMsgCmd state.Ws
    | PageInput (APageInput (ChatInput chatInput)), false ->
        match authState.AuthPageStates.ChatState with
        | Some chatState ->
            let chatState, chatCmd = Chat.State.transition chatInput chatState
            let authPageStates = { authState.AuthPageStates with ChatState = Some chatState }
            { state with AppState = Auth { authState with AuthPageStates = authPageStates } }, chatCmd |> Cmd.map (ChatInput >> APageInput >> PageInput >> AuthInput >> AppInput)
        | None -> shouldNeverHappen "Unexpected ChatInput when ChatState is None" state
    | ChangePassword, false -> state, warningToastCmd "Change password functionality is coming soon"
    | SignOut, false ->
        let cmd = UiAuthMsg (Jwt authState.AuthUser, SignOutMsgOLD) |> sendMsgCmd state.Ws
        { state with AppState = Auth { authState with SigningOut = true } }, cmd
    | UserAdministration, false -> // TODO-NMB-LOW: Check that authState.AuthUser has appropriate permissions?...
        state, warningToastCmd "User administration functionality is coming soon"
    | _, true -> shouldNeverHappen (sprintf "Unexpected AuthInput when SigningOut -> %A" authInput) state

let private handleAppInput appInput state =
    match appInput, state.AppState with
    | ReadingPreferencesInput result, ReadingPreferences -> handleReadingPreferencesInput result state
    | ConnectingInput ws, Connecting _ -> handleConnectingInput ws state
    | UnauthInput unauthInput, Unauth unauthState -> handleUnauthInput unauthInput unauthState state
    | AuthInput authInput, Auth authState -> handleAuthInput authInput authState state
    | _, appState -> shouldNeverHappen (sprintf "Unexpected AppInput when %s -> %A" (appStateText appState) appInput) state

let transition input state =
    match input with
#if TICK
    | Tick -> { state with Ticks = state.Ticks + 1<tick> }, Cmd.none
#endif
    | AddNotificationMessage notificationMessage -> addNotificationMessage notificationMessage state, Cmd.none
    | DismissNotificationMessage notificationId -> { state with NotificationMessages = state.NotificationMessages |> removeNotificationMessage notificationId }, Cmd.none // note: silently ignore unknown notificationId
    | ToggleTheme ->
        let state = { state with UseDefaultTheme = not state.UseDefaultTheme }
        setBodyClass state.UseDefaultTheme
        state, writePreferencesCmd state
    | ToggleNavbarBurger -> { state with NavbarBurgerIsActive = not state.NavbarBurgerIsActive }, Cmd.none
    | ShowStaticModal staticModal -> { state with StaticModal = Some staticModal }, Cmd.none
    | HideStaticModal -> { state with StaticModal = None }, Cmd.none
    | WritePreferencesResult (Ok _) -> state, Cmd.none
    | WritePreferencesResult (Error exn) -> addDebugError (sprintf "WritePreferencesResult -> %s" exn.Message) None state // note: no need for toast
    | WsError wsError -> handleWsError wsError state
    | HandleServerMsg serverMsg -> handleServerMsg serverMsg state
    | AppInput appInput -> handleAppInput appInput state

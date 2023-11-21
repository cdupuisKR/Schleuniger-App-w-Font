Imports System.Configuration
Imports System.IO
Imports WinCutSchl.KnowDotNet.KDNGrid
Imports System.Threading
Imports System.Windows.Forms
Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.Reflection
Imports Trilogy.LibTools
Imports Trilogy.KR.Wiretrac.LibPermissions.modPermissions
Imports Trilogy.KR.Wiretrac.LibRouting

Public Class frmCutSchl

#Region " Notes for Kyle "
  ' 1) I have not removed the Scan In and employee ID stuff at the top of the form yet - while user interaction is not expected to be the primary mode of operation, 
  '   We still should support the Retry button at least to jump start things if the operator thinks there is a file pending that the app is not looking at.
  ' 2) The "Cut Interceptor" (WinCutInterceptor) works on a slightly different model for reading DDS and SDC files. Even if the last DDS message passed on has not been acted and responded to yet,
  '   The Cut Interceptor will still read and pass along another message if it appears, appending it to the last one at the Cut Machine. It operates asynchronously with regard to 
  '   messages from either the Cell Controller (in *.DDS) or from the Cut Machine (Job.SDC). In contrast, this app reads one message from *.DDS, instructs the machine about what to do,
  '   then WAITS until it gets a response from the Cut Machine. It never reads another DDS request until after it has written out some kind of Job.SDC reponse.
  ' 3) So sequence is:
  '   a) Job.DDS changed event fires
  '   b) App reads one message from Job.DDS (even if there are multiuple requests pending)
  '   c) App parses request and tells Cut Machine what to do
  '   d) App does nothing until Cut Machine fires a response event 
  '   e) App parses the response event and converts to a WPCS response string, which it appends to Job.SDC (Job.SDC could still have another response in it but not likely)
  '   f) As soon as Job.SDC is appended, App manually checks Job.DDS for another message. If thre is one, continue with step (b); if not wait for step (a)

  ' TimerScanIn is somewhat misnamed perhaps, but it does 2 things:
  ' 1) It allows me to update the timer control showing how long the operator has been scanned in (we may not need this)
  ' 2) The windows events which fire when the files have changed, are running in a different thread, which causes VS to throw errors when resulting logic involves any changes to 
  '   windows form objects (this is a documented problem with windows form objects). There are several solutions but I went with a pretty simple one that involves setting form
  '   properties in the windows file changed events. Then the form's own timer, running in the form's thread, responds to a change in the form properties. Since the timer is not doing anything 
  '   else that is resource intensive, I can afford to fire it often enough to not be a performance issue for the process.

  ' ProcessDDSReadRequest gets called when the app has parsed a Job.DSS request. Note that all data from the request is in a dataset there for you to work with.
  ' You will want to write your logic to instruct the machine here. You can handle certain kinds of error conditions right then, and the result will be that the request never
  ' even gets removed from JOB.DDS. If the call to the cut machine succeeds, then returning success will cause JOB.DDS etc to be written back with that request removed from it.
  ' All the time you are in this routine, Job.DDS etc are still LOCKED so no other process can write to them. You will not do the modUtil.DDSAppendFiles call.

  ' You will never call ProcessSDCReadRequest, but your response event fired by the Cut machine will give you info about the response, and you will make your own call to 
  ' modUtil.SDCAppendFiles to write the results to the SDC file. This is where I woudl think you might want to load your data into the SDC dataset, the write a routine that constructs the 
  ' SDC append string from the dataset content, before calling modUtil.SDCAppendFiles with SDC append string.

  ' This design entails a state variable telling you if a Cut Machine request is pending ... note my comment in ProcessDDSReadRequest about not actually processing the DDS read if
  ' you are still waiting for the Cut Machine. You do want to support the user deciding the machie is never going to respond, and pressing the btnRetry button to check for any more 
  ' Job.DDS requests.

  ' 1/8/08: After a discussion with Scott, I trimmed a LOT more out of this app that was irrelevant, including all the 
  ' CUT machine side of the WPCS file stuff and logic to pass through (unchanged and unprocessed) certain kinds of files.
  ' I also eliminated everything related to Employee badge Scanin.

  ' Things to do to get this app running for your first test:
  ' 1) tblCutMachine contains the paths CutMachineDataFolder and CutMachineFeedbackFolder - set them for your setup to wherever you want those folders.
  ' 2) Start the app
  ' 3) Drop a Job.DDS and Article.DDS into the CutMachineDataFolder.
  ' 4) Set a breakpoint in ProcessDDSReadRequest
  ' 5) The app should spot it and you should find yourself at the breakpoint


#End Region

#Region " Form To Top "

  Private Function RunningInstance() As Process
    Dim CurrentProcess As Process = Process.GetCurrentProcess()
    'Loop through the running processes in with the same name 
    For Each oProcess As Process In Process.GetProcessesByName(CurrentProcess.ProcessName)
      'Ignore the current process 
      If oProcess.Id <> CurrentProcess.Id Then
        'Make sure that the process is running from the exe file. 
        If [Assembly].GetExecutingAssembly().Location.Replace("/", "\") = CurrentProcess.MainModule.FileName Then
          'Return the other process instance. 
          Return oProcess
        End If
      End If
    Next oProcess
    'No other instance was found, return null. 
    Return Nothing
  End Function 'RunningInstance 


  'Declarations of Windows API functions.
  Declare Function OpenIcon Lib "user32" (ByVal hwnd As Long) As Long
  Declare Function SetForegroundWindow Lib "user32" (ByVal hwnd As Long) As Long
  Declare Function ShowWindow Lib "user32" (ByVal hwnd As Long, ByVal swCommand As Integer) As Long

  <DllImport("User32.dll")>
  Public Shared Function ShowWindowAsync(ByVal hWnd As IntPtr, ByVal swCommand As Integer) As Integer
  End Function

  Private Enum ShowWindowConstants
    SW_HIDE = 0
    SW_SHOWNORMAL = 1
    SW_NORMAL = 1
    SW_SHOWMINIMIZED = 2
    SW_SHOWMAXIMIZED = 3
    SW_MAXIMIZE = 3
    SW_SHOWNOACTIVATE = 4
    SW_SHOW = 5
    SW_MINIMIZE = 6
    SW_SHOWMINNOACTIVE = 7
    SW_SHOWNA = 8
    SW_RESTORE = 9
    SW_SHOWDEFAULT = 10
    SW_FORCEMINIMIZE = 11
    SW_MAX = 11
  End Enum

  '<STAThread()> _
  Private Sub ForceToTop()
    Dim sProcess As String = Process.GetCurrentProcess.ProcessName
    'For Each oP As Process In Process.GetProcesses
    '    Dim s As String = oP.ProcessName
    '    s = s
    'Next
    Dim RunningProcesses As Process() = Process.GetProcessesByName(sProcess)
    'ShowWindow(RunningProcesses(0).MainWindowHandle, ShowWindowConstants.SW_SHOWMINIMIZED)
    'ShowWindow(RunningProcesses(0).MainWindowHandle, ShowWindowConstants.SW_RESTORE)

    'ShowWindowAsync(RunningProcesses(0).MainWindowHandle, ShowWindowConstants.SW_SHOWMINIMIZED)
    'ShowWindowAsync(RunningProcesses(0).MainWindowHandle, ShowWindowConstants.SW_RESTORE)
    ''Thread.Sleep(5000)
    Dim Hndl As Long = Process.GetCurrentProcess.MainWindowHandle.ToInt32
    Dim result As Long = SetForegroundWindow(Hndl) 'Activate the application.
  End Sub

#End Region

#Region " Form Load "

  ' DRB 9/9/10 Now this says "WinCutSchl" instead of "WinCutInterceptor"
  Private msAppName As String = "WinCutSchl"
  Private miCutMachineID As Integer = 0
  Private msCutMachineName As String = ""
  Private msCutMachineType As String = ""
  'Private miWorkCenterID As Integer = 0
  'Private msWorkCenterName As String = ""
  Private msConnectionString As String = ""
  'Private msBoxPrinter As String = ""
  'Private msConnPrinter As String = ""
  Private miSQLTimeOutSeconds As Integer = 0
  Private miSQLRetryUntilSeconds As Integer = 0
  Private miSQLRetrySleepMilliseconds As Integer = 0
  'Private miCTRangeFrom As Integer
  'Private miCTRangeTo As Integer

  Private moPrincipal As PersonPrincipal
  Private moIdentity As PersonIdentity = Nothing
  Private miUserID As Integer = 0
  Private msLoginName As String = ""
  Private msLoginOverride As String = ""

  ' DRB 2/16/12 added this to conform with winapp pattern of always calling PersonAuthenticate.AuthenticateWinApp
  Private msOverrideLoginID As String = ""

  Private mbSkipAuthentication As Boolean
  Private mbLogEvents As Boolean = True
  Private msEventLogTextFile As String
  Private mbRespondToMachineEvents As Boolean

  Private mbIsSecondCopy As Boolean = False

  Private Sub frmInterceptor_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing

    ' DRB 9/9/10 now I log when CutSchl Closed.
    If mbIsSecondCopy = False Then
      LogEvent(msAppName & " Closed For " & msLoginName, True)
    End If

  End Sub

  Private Sub frmInterceptor_LostFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.LostFocus
    Debug.WriteLine("frmInterceptor_LostFocus")
  End Sub

  Private Sub frmInterceptor_GotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.GotFocus
    Debug.WriteLine("frmInterceptor_GotFocus")
  End Sub

  Private Sub frmInterceptor_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

    Dim sError As String = ""

    Try


      ''''''''''''''''''''''' See if app should Authenticate Login User '''''''''''''''''''''''''''
      If ConfigurationManager.AppSettings("SkipAuthentication") Is Nothing Then
        mbSkipAuthentication = True
      Else
        Dim sSkipAuthentication As String = ConfigurationManager.AppSettings("SkipAuthentication")
        mbSkipAuthentication = (sSkipAuthentication.ToUpper = "Y")
      End If
      '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''


      ''''''''''''''''''''''' See if app should Authenticate Login User '''''''''''''''''''''''''''
      If ConfigurationManager.AppSettings("RespondToMachineEvents") Is Nothing Then
        mbRespondToMachineEvents = False
      Else
        Dim sRespondToMachineEvents As String = ConfigurationManager.AppSettings("RespondToMachineEvents")
        mbRespondToMachineEvents = (sRespondToMachineEvents.ToUpper = "Y")
      End If
      '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''


      ''''''''''''''''''''''''''''''''' See if app should Log Events ''''''''''''''''''''''''''''''
      If ConfigurationManager.AppSettings("LogEvents") Is Nothing Then
        mbLogEvents = False
      Else
        Dim sLogEvents As String = ConfigurationManager.AppSettings("LogEvents")
        mbLogEvents = (sLogEvents.ToUpper = "Y")
      End If
      If ConfigurationManager.AppSettings("EventLogTextFile") Is Nothing Then
        msEventLogTextFile = ""
      Else
        msEventLogTextFile = ConfigurationManager.AppSettings("EventLogTextFile")
      End If
      If mbSkipAuthentication AndAlso mbLogEvents AndAlso msEventLogTextFile = "" Then
        Throw New Exception("If you are skipping authentication and logging events, then the EventLogTextFile must be a valid file name.")
      End If
      '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''



      ''''''''''''''''''''''' Authenticate Login User or Override User '''''''''''''''''''''''''''
      If mbSkipAuthentication = False Then

        Try
          msConnectionString = GetCNFConnectionString()
          'msConnectionString = "database=localhost;initial catalog=SC;uid=WireTrac;pwd=Wt112358"
        Catch ex As Exception
          Throw New Exception("Server Not Available.")
        End Try

        ' DRB 2/16/12 Now all winapps call PersonAuthenticate.AuthenticateWinApp
        'Dim sOverrideLoginUser As String = ""
        'If ConfigurationManager.AppSettings("OverrideLoginUser") IsNot Nothing Then
        '  sOverrideLoginUser = ConfigurationManager.AppSettings("OverrideLoginUser")
        '  If MsgBox("Config file contains OverrideLoginUser=" & sOverrideLoginUser & "." & vbCrLf & vbCrLf & "Use this override login?", MsgBoxStyle.Exclamation Or MsgBoxStyle.YesNo Or MsgBoxStyle.DefaultButton1 Or MsgBoxStyle.SystemModal, "Cut Machine Interceptor") = MsgBoxResult.No Then
        '    sOverrideLoginUser = ""
        '  Else
        '    msLoginOverride = "Login overridden with " & sOverrideLoginUser
        '  End If
        'End If
        'sError = UserAuthenticate.LoginToWinapp(moPrincipal, msConnectionString, sOverrideLoginUser)
        'If sError = "" Then
        '  If Not UserHasPermission(moPrincipal, Permissions.CutInterceptor) Then
        '    sError = "Login [" & CType(moPrincipal.Identity, radTools.PersonIdentity).LoginId & "] does not have permissions for this application."
        '  End If
        'End If
        'If sError > "" Then
        '  Throw New Exception(sError)
        'End If
        'moIdentity = CType(moPrincipal.Identity, radTools.PersonIdentity)
        PersonAuthenticate.AuthenticateWinApp(PersonPrincipal:=moPrincipal, ConnectionString:=msConnectionString, OverrideLoginID:=msOverrideLoginID, Context:="Cut Machine Interceptor")
        moIdentity = CType(moPrincipal.Identity, PersonIdentity)
        If Not UserHasPermission(moPrincipal, Permissions.CutInterceptor) Then
          Throw New Exception(IIf(Application.ProductName > "", Application.ProductName & " : ", "") & "Login [" & moIdentity.LoginId & "] does not have permissions for this application.")
        End If

        msLoginName = moIdentity.LoginId
        msCutMachineName = msLoginName      ' This is reset for real in UpdateConfig, but this insures that a log event now sets a StationID
        If msCutMachineName.Length > 10 Then
          msCutMachineName = msCutMachineName.Substring(0, 10)
        End If
        miUserID = moIdentity.PersonID
      Else
        moIdentity = Nothing
        msLoginName = ""
        miUserID = 0
      End If
      ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

      ' DRB 9/9/10 Moved this to where I can get StationID and rqAddedbyDate
      If RunningInstance() IsNot Nothing Then
        Me.mbIsSecondCopy = True
        Throw New Exception(msAppName & " This application is already running on this machine.")
      End If

      ' DRB 9/9/10 Now this says starting instead of started.
      LogEvent(msAppName & " Starting for " & msCutMachineName)

      Dim CloseNow As Boolean   ' Since no badgenumber is passed in CloseNow is always returned false because the msgbox is never displayed.
      If mbSkipAuthentication Then
        CloseNow = False

        If ConfigurationManager.AppSettings("CutMachineID") Is Nothing Then
          Throw New Exception("When config <SkipAuthentication> = Y then <CutMachineID> must be provided in the config file.")
        Else
          Dim sCutMachineID As String = ConfigurationManager.AppSettings("CutMachineID")
          If IsNumeric(sCutMachineID) Then
            miCutMachineID = CInt(sCutMachineID)
          Else
            Throw New Exception("Invalid <CutMachineID> in the config file (must be an integer).")
          End If
        End If

        If ConfigurationManager.AppSettings("CutMachineName") Is Nothing Then
          Throw New Exception("When config <SkipAuthentication> = Y then <CutMachineName> must be provided in the config file.")
        Else
          msCutMachineName = ConfigurationManager.AppSettings("CutMachineName")
        End If

        If ConfigurationManager.AppSettings("CutMachineType") Is Nothing Then
          Throw New Exception("When config <SkipAuthentication> = Y then <CutMachineType> must be provided in the config file.")
        Else
          msCutMachineType = ConfigurationManager.AppSettings("CutMachineType")
        End If

        If ConfigurationManager.AppSettings("DataFolderWhenSkippingAuthentication") Is Nothing Then
          msCellRequestFolder = ""
        Else
          msCellRequestFolder = ConfigurationManager.AppSettings("DataFolderWhenSkippingAuthentication")
        End If
        If msCellRequestFolder = "" Then
          Throw New Exception(msAppName & " config <DataFolderWhenSkippingAuthentication> must contain CutMachine Data Folder.")
        End If

        If ConfigurationManager.AppSettings("FeedbackFolderWhenSkippingAuthentication") Is Nothing Then
          msCellResponseFolder = ""
        Else
          msCellResponseFolder = ConfigurationManager.AppSettings("FeedbackFolderWhenSkippingAuthentication")
        End If
        If msCellResponseFolder = "" Then
          Throw New Exception(msAppName & " config <FeedbackFolderWhenSkippingAuthentication> must contain CutMachine Feedback Folder.")
        End If

      Else
        AuthenticateLogin(CloseNow)
      End If

      If Not Directory.Exists(msCellRequestFolder) Then
        Throw New Exception(msAppName & ": CutMachine Data Folder [" & msCellRequestFolder & "] for Cut Machine " & CStr(miCutMachineID) & " does not exist.")
      End If
      mdiCellRequestFolder = New DirectoryInfo(msCellRequestFolder)

      If Not Directory.Exists(msCellResponseFolder) Then
        Throw New Exception(msAppName & ": CutMachine Feedback Folder [" & msCellResponseFolder & "] for Cut Machine " & CStr(miCutMachineID) & " does not exist.")
      End If
      mdiCellResponseFolder = New DirectoryInfo(msCellResponseFolder)

      Me.Text = "WireTrac Schleuniger Controller v" & MyAssembly.Version & "  (" & msCutMachineName & " : " & msCutMachineType & ")"
      If msLoginOverride > "" Then    ' Won't happen when skipping authentication
        Me.Text &= " [" & msLoginOverride & "]"
      End If

      StartWatchers()

      'If ConfigurationManager.AppSettings("SQLListener_WebSvc_URL") Is Nothing Then
      '    Throw New Exception("WinCutInterceptor app.config file must contain appSetting key=[SQLListener_WebSvc_URL].")
      'End If
      'msSQLListener_WebSvc_URL = ConfigurationManager.AppSettings("SQLListener_WebSvc_URL")
      'moSQLListener_WebSvc = New SQLListener_WebSvc.SQLListenerWeb
      'moSQLListener_WebSvc.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials
      'moSQLListener_WebSvc.Url = ConfigurationManager.AppSettings("SQLListener_WebSvc_URL")

      ' DRB I neeeded to disable this - the filewatchers were already started in AuthenticateLogin.
      'moCellRequestWatcher = New clsFSW()
      'moCutResponseWatcher = New clsFSW()
      'moCellResponseWatcher = New clsFSW()
      'moCutRequestWatcher = New clsFSW()

      Dim sTemp As String
      If ConfigurationManager.AppSettings("WPCSFirstTryMS") Is Nothing Then
        Throw New Exception("WinCutInterceptor app.config file must contain appSetting key=[WPCSFirstTryMS].")
      End If
      sTemp = ConfigurationManager.AppSettings("WPCSFirstTryMS")
      If Not IsNumeric(sTemp) Then
        Throw New Exception("WinCutInterceptor app.config file contains invalid appSetting key=[WPCSFirstTryMS] Value=[" & sTemp & "]. Value must be an integer.")
      End If
      miWPCSFirstTryMS = CInt(sTemp)


      If ConfigurationManager.AppSettings("WPCSRetryTimes") Is Nothing Then
        Throw New Exception("WinCutInterceptor app.config file must contain appSetting key=[WPCSRetryTimes].")
      End If
      sTemp = ConfigurationManager.AppSettings("WPCSRetryTimes")
      If Not IsNumeric(sTemp) Then
        Throw New Exception("WinCutInterceptor app.config file contains invalid appSetting key=[WPCSRetryTimes] Value=[" & sTemp & "]. Value must be an integer.")
      End If
      miWPCSRetryTimes = CInt(sTemp)

      If ConfigurationManager.AppSettings("WPCSRetrySleepMilliSeconds") Is Nothing Then
        Throw New Exception("WinCutInterceptor app.config file must contain appSetting key=[WPCSRetrySleepMilliSeconds].")
      End If
      sTemp = ConfigurationManager.AppSettings("WPCSRetrySleepMilliSeconds")
      If Not IsNumeric(sTemp) Then
        Throw New Exception("WinCutInterceptor app.config file contains invalid appSetting key=[WPCSRetrySleepMilliSeconds] Value=[" & sTemp & "]. Value must be an integer.")
      End If
      miWPCSRetrySleepMilliSeconds = CInt(sTemp)

      If ConfigurationManager.AppSettings("WPCSDoPromptForRetry") Is Nothing Then
        Throw New Exception("WinCutInterceptor app.config file must contain appSetting key=[WPCSDoPromptForRetry].")
      End If
      sTemp = ConfigurationManager.AppSettings("WPCSDoPromptForRetry")
      If sTemp <> "Y" AndAlso sTemp <> "N" Then
        Throw New Exception("WinCutInterceptor app.config file contains invalid appSetting key=[WPCSDoPromptForRetry] Value=[" & sTemp & "]. Valid values are Y and N.")
      End If
      mbWPCSDoPromptForRetry = (sTemp = "Y")

      'Dim sSecs As String
      'If ConfigurationManager.AppSettings("SQLRetryUntilSeconds") Is Nothing Then
      '    Throw New Exception(msAppName & " config file must have SQLRetryUntilSeconds set.")
      'End If
      'sSecs = ConfigurationManager.AppSettings("SQLRetryUntilSeconds")
      'If Not IsNumeric(sSecs) Then
      '    Throw New Exception(msAppName & " config file must have a numeric value in SQLRetryUntilSeconds.")
      'End If
      'miSQLRetryUntilSeconds = CInt(sSecs)

      'If ConfigurationManager.AppSettings("SQLRetrySleepMilliseconds") Is Nothing Then
      '    Throw New Exception(msAppName & " config file must have SQLRetrySleepMilliseconds set.")
      'End If
      'sSecs = ConfigurationManager.AppSettings("SQLRetrySleepMilliseconds")
      'If Not IsNumeric(sSecs) Then
      '    Throw New Exception(msAppName & " config file must have a numeric value in SQLRetrySleepMilliseconds.")
      'End If
      'miSQLRetrySleepMilliseconds = CInt(sSecs)

      'If ConfigurationManager.AppSettings("SQLTimeOutSeconds") Is Nothing Then
      '    Throw New Exception(msAppName & " config file must have SQLTimeOutSeconds set.")
      'End If
      'sSecs = ConfigurationManager.AppSettings("SQLTimeOutSeconds")
      'If Not IsNumeric(sSecs) Then
      '    Throw New Exception(msAppName & " config file must have a numeric value in SQLTimeOutSeconds.")
      'End If
      'miSQLTimeOutSeconds = CInt(sSecs)
      'If Not msConnectionString.EndsWith(";") Then
      '    msConnectionString &= ";"
      'End If
      'If Not msConnectionString.Contains("Connection Timeout=") Then
      '    msConnectionString &= ";Connection Timeout=" & CStr(miSQLTimeOutSeconds) & ";"
      'End If

      ' DRB 12/14/07 I will not call the validations at all from Interceptor - let Web Service do that.
      'Dim sRange As String
      'If ConfigurationManager.AppSettings("CTRangeFrom") Is Nothing Then
      '    Throw New Exception(msAppName & " config file must have CTRangeFrom set.")
      'End If
      'sRange = ConfigurationManager.AppSettings("CTRangeFrom")
      'If Not IsNumeric(sRange) Then
      '    Throw New Exception(msAppName & " config file must have a numeric value in CTRangeFrom.")
      'End If
      'miCTRangeFrom = CInt(sRange)

      'If ConfigurationManager.AppSettings("CTRangeTo") Is Nothing Then
      '    Throw New Exception(msAppName & " config file must have CTRangeTo set.")
      'End If
      'sRange = ConfigurationManager.AppSettings("CTRangeTo")
      'If Not IsNumeric(sRange) Then
      '    Throw New Exception(msAppName & " config file must have a numeric value in CTRangeTo.")
      'End If
      'miCTRangeTo = CInt(sRange)

      mtblMessage = New DataTable("Message")
      With mtblMessage
        .Columns.Add(New DataColumn("MessageID", System.Type.GetType("System.Int32")))
        .Columns.Add(New DataColumn("MessageType", System.Type.GetType("System.String")))
        .Columns.Add(New DataColumn("MessageTime", System.Type.GetType("System.String")))
        .Columns.Add(New DataColumn("MessageString", System.Type.GetType("System.String")))
        .Constraints.Add(New System.Data.UniqueConstraint("MessageKey", New System.Data.DataColumn() { .Columns("MessageID")}, True))
        With .Columns("MessageID")
          .AutoIncrement = True
          .AutoIncrementSeed = -1
          .AutoIncrementStep = -1
          .AllowDBNull = False
          .ReadOnly = True
          .Unique = True
        End With
      End With
      mtblMessagesReversed = mtblMessage.Clone()

      Me.Visible = True

      'mdvMessage = New DataView(mtblMessage, "", "MessageID ASC", DataViewRowState.CurrentRows)

      RefreshLog()

      ReadPending()

      Me.TimerScanIn.Start()

    Catch ex As Exception

      MsgBox(ex.Message, MsgBoxStyle.SystemModal Or MsgBoxStyle.Exclamation)
      LogEvent(ex.Message, True)
      Close()

    End Try

  End Sub

  Private Sub AuthenticateLogin(ByRef CloseNow As Boolean)

    ' NOTE: In CutInterceptor, we reload lots of settings each time the user scans his badge again, so loading those settings are done in here.
    ' This also gets called once in for load, with BadgeNumber=""

    Dim sError As String = ""
    Dim sFolder As String = ""
    Dim dsCut As New BatchSet
    CloseNow = False

    ' DRB 12/17/07 Now I load the entire tblCutMachine record because I need a lot of fields from it.
    modUtil.FillCutMachineByName(dsCut, msLoginName, Me.msConnectionString)
    If dsCut.CutMachines.Count = 0 Then
      Throw New Exception(msAppName & " Name=[" & msLoginName & "] not found in tblCutMachine table.")
    End If
    msCutMachineType = dsCut.CutMachines(0).MachineType
    miCutMachineID = dsCut.CutMachines(0).CutMachineID
    msCutMachineName = msLoginName

    'miWorkCenterID = miCutMachineID
    'msWorkCenterName = "Cut " & Format(miCutMachineID, "00")


    ' DRB 3/20/12 per Scott, CutSchl does not need the printer names at all.
    ' DRB 3/20/12 Now modUtil.GetPrinterNamesForUserID just returns BLANKS for printer names that are missing or blank; parent program decides what to do.
    'modUtil.GetPrinterNamesForUserID(moIdentity.PersonID, msBoxPrinter, msConnPrinter, msConnectionString)
    'Dim iUserID As Integer  ' I really don't need this.
    'Try

    '  'modUtil.GetUserIDandPrinterNamesForLoginID(msLoginName, iUserID, msBoxPrinter, msConnPrinter, msConnectionString)
    '  ''Dim sResult As String = radData.GetResultStringFromQuery(msConnectString, "Select isnull([User].Primary_Key,0) from [User] WHERE [User].Login_ID='" & msStationName & "'", False)

    '  modUtil.GetPrinterNamesForUserID(moIdentity.PersonID, msBoxPrinter, msConnPrinter, msConnectionString)

    'Catch ex As Exception
    '  If iUserID = 0 OrElse ex.Message.Contains("Invalid Login Id") Then
    '    Throw New Exception(msAppName & "No WireTrac user for " & msLoginName)
    '  Else
    '    Throw New Exception(msAppName & " Error getting User table record for LoginID=[" & msLoginName & "]: [" & ex.Message & "]")
    '  End If
    'End Try




    ' DRB 9/29/10 Added this to replace the stuff below it just like I had already done to Interceptor so that the Machine paths can now be relative.
    SetWatcherPaths(dsCut.CutMachines(0))

    '' DRB 12/17/07 These four folders are now obtained from tblCutMachine.
    ''sFolder = ConfigurationManager.AppSettings("CellControllerRequestFolder")
    '' DRB 1/8/08 The INPUT to this app is the OUTPUT of the Interceptor app - A little confusing but there it is !!!!
    'msCellRequestFolder = dsCut.CutMachines(0).CutMachineDataFolder
    'If msCellRequestFolder = "" Then
    '  Throw New Exception(msAppName & " tblCutMachine table must contain CutMachineDataFolder value for Cut Machine " & CStr(miCutMachineID) & ".")
    'End If
    ''moCellRequestWatcher.FolderToMonitor = sFolder
    ''moCellRequestWatcher.StartWatch()

    '' DRB 12/17/07 These four folders are now obtained from tblCutMachine.
    ''sFolder = ConfigurationManager.AppSettings("CellControllerResponseFolder")
    '' DRB 1/8/08 The INPUT to this app is the OUTPUT of the Interceptor app - A little confusing but there it is !!!!
    'msCellResponseFolder = dsCut.CutMachines(0).CutMachineFeedbackFolder
    'If msCellResponseFolder = "" Then
    '  Throw New Exception(msAppName & " tblCutMachine table must contain CutMachineFeedbackFolder value for Cut Machine " & CStr(miCutMachineID) & ".")
    'End If
    ''moCellResponseWatcher.FolderToMonitor = sFolder
    ''moCellResponseWatcher.StartWatch()

  End Sub

  Private Sub SetWatcherPaths(ByVal oCutMachine As BatchSet.CutMachineRow)
    ' DRB 9/29/10 Added this just like I had already done to Interceptor so that the Machine paths can now be relative.
    ' Rediscovered that this was already in place and ready to test 9/17/12
    Dim sPrefix As String = ""
    Dim sSuffix As String = ""
    Dim sFolder As String = ""

    ' DRB 9/24/10 New logic: Use path prefix in front of tblCutMachine data folder paths.
    If ConfigurationManager.AppSettings("DataAndFeedbackFolderPathPrefix") Is Nothing Then
      sPrefix = ""
    Else
      sPrefix = ConfigurationManager.AppSettings("DataAndFeedbackFolderPathPrefix")
      sPrefix = sPrefix.Trim.Replace("/", "\")
      If sPrefix > "" AndAlso sPrefix.EndsWith("\") = False Then
        sPrefix &= "\"
      End If
    End If

    sSuffix = oCutMachine.CutMachineDataFolder
    sSuffix = sSuffix.Trim.Replace("/", "\")
    If sSuffix > "" AndAlso sSuffix.EndsWith("\") = False Then
      sSuffix &= "\"
    End If

    If sSuffix = "" Then
      Throw New Exception(msAppName & " tblCutMachine table must contain CutMachineDataFolder value for Cut Machine " & CStr(miCutMachineID) & ".")
    End If
    If sSuffix.StartsWith("\") AndAlso sPrefix > "" AndAlso sPrefix.EndsWith("\") Then
      sFolder = sPrefix.Substring(0, sPrefix.Length - 1) & sSuffix
    Else
      sFolder = sPrefix & sSuffix
    End If

    If Not Directory.Exists(sFolder) Then
      Throw New Exception(msAppName & " tblCutMachine table (plus config file prefix) contain Cut Machine Data Folder value [" & sFolder & "] for Cut Machine " & CStr(miCutMachineID) & ", which does not exist.")
    End If
    'mdiCutRequestFolder = New DirectoryInfo(sFolder)
    msCellRequestFolder = sFolder


    sSuffix = oCutMachine.CutMachineFeedbackFolder
    sSuffix = sSuffix.Trim.Replace("/", "\")
    If sSuffix > "" AndAlso sSuffix.EndsWith("\") = False Then
      sSuffix &= "\"
    End If
    If sSuffix = "" Then
      Throw New Exception(msAppName & " tblCutMachine table must contain CutMachineFeedbackFolder value for Cut Machine " & CStr(miCutMachineID) & ".")
    End If
    If sSuffix.StartsWith("\") AndAlso sPrefix > "" AndAlso sPrefix.EndsWith("\") Then
      sFolder = sPrefix.Substring(0, sPrefix.Length - 1) & sSuffix
    Else
      sFolder = sPrefix & sSuffix
    End If

    If Not Directory.Exists(sFolder) Then
      Throw New Exception(msAppName & " tblCutMachine table (plus config file prefix) contain Cut Machine Feedback Folder value [" & sFolder & "] for Cut Machine " & CStr(miCutMachineID) & ", which does not exist.")
    End If
    'mdiCutResponseFolder = New DirectoryInfo(sFolder)
    msCellResponseFolder = sFolder
  End Sub


  Private Sub StartWatchers()

    If moCellRequestWatcher IsNot Nothing Then
      moCellRequestWatcher.StopWatch()
      moCellRequestWatcher = Nothing
    End If
    moCellRequestWatcher = New Trilogy.LibTools.FileSystemWatcher
    moCellRequestWatcher.FolderToMonitor = mdiCellRequestFolder.FullName
    moCellRequestWatcher.IncludeSubfolders = False
    moCellRequestWatcher.StartWatch()

    If moCellResponseWatcher IsNot Nothing Then
      moCellResponseWatcher.StopWatch()
      moCellResponseWatcher = Nothing
    End If
    moCellResponseWatcher = New Trilogy.LibTools.FileSystemWatcher
    moCellResponseWatcher.FolderToMonitor = mdiCellResponseFolder.FullName
    moCellResponseWatcher.IncludeSubfolders = False
    moCellResponseWatcher.StartWatch()

  End Sub


#End Region

#Region " SystemFileWatchers "


  Private WithEvents moCellRequestWatcher As New Trilogy.LibTools.FileSystemWatcher()
  'Private WithEvents moCutResponseWatcher As New clsFSW()
  Private WithEvents moCellResponseWatcher As New Trilogy.LibTools.FileSystemWatcher()
  'Private WithEvents moCutRequestWatcher As New clsFSW()

  Private msCellRequestFolder As String
  Private mdiCellRequestFolder As DirectoryInfo

  Private msCellResponseFolder As String
  Private mdiCellResponseFolder As DirectoryInfo
  'Private mdiCutRequestFolder As DirectoryInfo
  'Private mdiCutResponseFolder As DirectoryInfo

  'Private mbFileEventFired As Boolean = False
  Private mqFilesChanged As New Queue(Of String)

  Private Sub moCellRequestWatcher_FileChanged(ByVal FullPath As String) Handles moCellRequestWatcher.FileChanged
    'Debug.WriteLine("moCellRequestWatcher_FileChanged:" & FullPath)
    Dim sFile As String = "CLRQ" & FullPath
    If Not mqFilesChanged.Contains(sFile) Then
      mqFilesChanged.Enqueue(sFile)
    End If
    'Me.mbFileEventFired = True
    'mbCellRequestPending = True
  End Sub
  Private Sub moCellRequestWatcher_FileCreated(ByVal FullPath As String) Handles moCellRequestWatcher.FileCreated
    Debug.WriteLine("moCellRequestWatcher_FileCreated:" & FullPath)
    Dim sFile As String = "CLRQ" & FullPath
    If Not mqFilesChanged.Contains(sFile) Then
      mqFilesChanged.Enqueue(sFile)
    End If
    'Me.mbFileEventFired = True
    'mbCellRequestPending = True
  End Sub
  'Private Sub moCellRequestWatcher_FileDeleted(ByVal FullPath As String) Handles moCellRequestWatcher.FileDeleted
  '    Debug.WriteLine("moCellRequestWatcher_FileDeleted:" & FullPath)
  '    Me.mbFileEventFired = True
  '    'mbCellRequestPending = True
  'End Sub
  Private Sub moCellRequestWatcher_FileRenamed(ByVal FullPath As String, ByVal NewPath As String) Handles moCellRequestWatcher.FileRenamed
    Debug.WriteLine("moCellRequestWatcher_FileRenamed:" & FullPath & " to " & NewPath)
    Dim sFile As String = "CLRQ" & FullPath
    If Not mqFilesChanged.Contains(sFile) Then
      mqFilesChanged.Enqueue(sFile)
    End If
    'Me.mbFileEventFired = True
    'mbCellRequestPending = True
  End Sub



  Private Sub moCellResponseWatcher_FileChanged(ByVal FullPath As String) Handles moCellResponseWatcher.FileChanged
    'Debug.WriteLine("moCellResponseWatcher_FileChanged:" & FullPath)
    Dim sFile As String = "CLRS" & FullPath
    If Not mqFilesChanged.Contains(sFile) Then
      mqFilesChanged.Enqueue(sFile)
    End If
    'Me.mbFileEventFired = True
    'mbCellResponsePending = True
  End Sub
  Private Sub moCellResponseWatcher_FileCreated(ByVal FullPath As String) Handles moCellResponseWatcher.FileCreated
    Debug.WriteLine("moCellResponseWatcher_FileCreated:" & FullPath)
    Dim sFile As String = "CLRS" & FullPath
    If Not mqFilesChanged.Contains(sFile) Then
      mqFilesChanged.Enqueue(sFile)
    End If
    'Me.mbFileEventFired = True
    'mbCellResponsePending = True
  End Sub
  'Private Sub moCellResponseWatcher_FileDeleted(ByVal FullPath As String) Handles moCellResponseWatcher.FileDeleted
  '    Debug.WriteLine("moCellResponseWatcher_FileDeleted:" & FullPath)
  '    Me.mbFileEventFired = True
  '    'mbCellResponsePending = True
  'End Sub
  Private Sub moCellResponseWatcher_FileRenamed(ByVal FullPath As String, ByVal NewPath As String) Handles moCellResponseWatcher.FileRenamed
    Debug.WriteLine("moCellResponseWatcher_FileRenamed:" & FullPath & " to " & NewPath)
    Dim sFile As String = "CLRS" & FullPath
    If Not mqFilesChanged.Contains(sFile) Then
      mqFilesChanged.Enqueue(sFile)
    End If
    'Me.mbFileEventFired = True
    'mbCellResponsePending = True
  End Sub



  'Private Sub moCutRequestWatcher_FileChanged(ByVal FullPath As String) Handles moCutRequestWatcher.FileChanged
  '    Debug.WriteLine("moCutRequestWatcher_FileChanged:" & FullPath)
  '    Dim sFile As String = "CTRQ" & FullPath
  '    If Not mqFilesChanged.Contains(sFile) Then
  '        mqFilesChanged.Enqueue(sFile)
  '    End If
  '    'Me.mbFileEventFired = True
  '    'mbCutRequestPending = True
  'End Sub
  'Private Sub moCutRequestWatcher_FileCreated(ByVal FullPath As String) Handles moCutRequestWatcher.FileCreated
  '    Debug.WriteLine("moCutRequestWatcher_FileCreated:" & FullPath)
  '    Dim sFile As String = "CTRQ" & FullPath
  '    If Not mqFilesChanged.Contains(sFile) Then
  '        mqFilesChanged.Enqueue(sFile)
  '    End If
  '    'Me.mbFileEventFired = True
  '    'mbCutRequestPending = True
  'End Sub
  'Private Sub moCutRequestWatcher_FileDeleted(ByVal FullPath As String) Handles moCutRequestWatcher.FileDeleted
  '    Debug.WriteLine("moCutRequestWatcher_FileDeleted:" & FullPath)
  '    Me.mbFileEventFired = True
  '    'mbCutRequestPending = True
  'End Sub
  'Private Sub moCutRequestWatcher_FileRenamed(ByVal FullPath As String, ByVal NewPath As String) Handles moCutRequestWatcher.FileRenamed
  '    Debug.WriteLine("moCutRequestWatcher_FileRenamed:" & FullPath & " to " & NewPath)
  '    Dim sFile As String = "CTRQ" & FullPath
  '    If Not mqFilesChanged.Contains(sFile) Then
  '        mqFilesChanged.Enqueue(sFile)
  '    End If
  '    'Me.mbFileEventFired = True
  '    'mbCutRequestPending = True
  'End Sub



  'Private Sub moCutResponseWatcher_FileChanged(ByVal FullPath As String) Handles moCutResponseWatcher.FileChanged
  '    Debug.WriteLine("moCutResponseWatcher_FileChanged:" & FullPath)
  '    Dim sFile As String = "CTRS" & FullPath
  '    If Not mqFilesChanged.Contains(sFile) Then
  '        mqFilesChanged.Enqueue(sFile)
  '    End If
  '    'Me.mbFileEventFired = True
  '    'mbCutResponsePending = True
  'End Sub
  'Private Sub moCutResponseWatcher_FileCreated(ByVal FullPath As String) Handles moCutResponseWatcher.FileCreated
  '    Debug.WriteLine("moCutResponseWatcher_FileCreated:" & FullPath)
  '    Dim sFile As String = "CTRS" & FullPath
  '    If Not mqFilesChanged.Contains(sFile) Then
  '        mqFilesChanged.Enqueue(sFile)
  '    End If
  '    'Me.mbFileEventFired = True
  '    'mbCutResponsePending = True
  'End Sub
  ''Private Sub moCutResponseWatcher_FileDeleted(ByVal FullPath As String) Handles moCutResponseWatcher.FileDeleted
  ''    Debug.WriteLine("moCutResponseWatcher_FileDeleted:" & FullPath)
  ''    Me.mbFileEventFired = True
  ''    'mbCutResponsePending = True
  ''End Sub
  'Private Sub moCutResponseWatcher_FileRenamed(ByVal FullPath As String, ByVal NewPath As String) Handles moCutResponseWatcher.FileRenamed
  '    Debug.WriteLine("moCutResponseWatcher_FileRenamed:" & FullPath & " to " & NewPath)
  '    Dim sFile As String = "CTRS" & FullPath
  '    If Not mqFilesChanged.Contains(sFile) Then
  '        mqFilesChanged.Enqueue(sFile)
  '    End If
  '    'Me.mbFileEventFired = True
  '    'mbCutResponsePending = True
  'End Sub



#End Region

#Region " Read Files "

  Private miWPCSFirstTryMS As Integer
  Private miWPCSRetryTimes As Integer
  Private miWPCSRetrySleepMilliSeconds As Integer
  Private mbWPCSDoPromptForRetry As Boolean
  Private msWPCSRetryPromptTitle As String

  'Private mbCellRequestIsFailing As Boolean = False
  'Private mbCutResponseIsFailing As Boolean = False

  Private Sub ReadPending()

    'mbCellRequestIsFailing = False   ' Set true if there is a read failure, stopping the loop.
    'mbCutResponseIsFailing = False   ' Set true if there is a read failure, stopping the loop.

    'txtScan.Enabled = False
    'lblScan.Enabled = False

    Me.lblCellRequestPending.Visible = False
    Me.lblCellResponsePending.Visible = False

    Debug.WriteLine("Reading")
    'ReadAllFiles(Me.mdiCutResponseFolder, "CTRS")   ' DRB 1/7/08 No such file to read in Schl app.
    'ReadAllFiles(Me.mdiCutRequestFolder, "CTRQ")    ' DRB 1/7/08 No such file to read in Schl app.
    ReadAllFiles(Me.mdiCellResponseFolder, "CLRS")
    ReadAllFiles(Me.mdiCellRequestFolder, "CLRQ")

    'ReadCutResponse()
    'ReadCutRequest()
    'ReadCellRequest()
    'ReadCellResponse()

  End Sub

  Private Sub ReadAllFiles(ByVal di As DirectoryInfo, ByVal Prefix As String)
    For Each oFile As FileInfo In di.GetFiles()
      ReadFilePending(Prefix & oFile.FullName)
    Next
  End Sub

  Private Function ReadFilePending(ByVal FullPath As String) As Boolean
    FullPath = FullPath.ToUpper.Trim
    Dim sFolderPrefix As String = FullPath.Substring(0, 4)
    FullPath = FullPath.Substring(4)
    Dim sFileName As String = URLNamePart(FullPath)
    Select Case sFolderPrefix
      Case "CLRQ"
        Select Case sFileName
          Case "JOB.DDS"
            ReadCellRequest()   ' ReadCellRequest specifically does Job.DDS and Artiucle.DDS as a pair.
          Case "ARTICLE.DDS"
            ' Do nothing - covererd by JOB.DDS above.
            'Case "FONT.DDS"
            '    bDoPassThrough = True
            'Case "WIRE.DDS"
            '    bDoPassThrough = True
            'Case "SEAL.DDS"
            '    bDoPassThrough = True
            'Case "TERMINAL.DDS"
            '    bDoPassThrough = True
            'Case "SLEEVE.DDS"
            '    bDoPassThrough = True
            'Case "HOUSING.DDS"
            '    bDoPassThrough = True
        End Select
      Case "CLRS"
        If sFileName = "JOB.SDC" Then
          ' This does nothing but determine if there are things pending for the display.
          ReadCellResponse()
        End If
    End Select
  End Function

  Private msLastCellRQErrInfo As String = ""    ' Prevents the same error from being posted to the error list or log twice in a row.
  Private Sub ReadCellRequest()
    ' Here you read from JOB.DDS and ARTICLE.DDS, extracting just one record from each.

    ' ErrInfo explains why in non-success cases (DDSReadFiles returns false)
    ' ErrInfo also contains extra information in some success cases (DDSReadFiles returns true), mainly is succeeding required text read retries or SQL updates done 
    ' as part of processing required SQL call retries that we want to log.
    Dim ErrInfo As String = ""

    ' Generally 0 in success cases
    ' Can be used to return other values if you need branching error handling logic at this level later.
    Dim ErrCode As Integer = 0

    ' A status string constructed for later display if the read & processing is successful. 
    ' Some of what I want to put in the string may come from the file read details, so this is passed for lower level routines to construct.
    Dim UpdateSuccessInfo As String = ""

    ' This is set true inside DDSReadFiles if there turns out to be an ADDITIONAL request pending besides that one found and processed by this call.
    Dim CellRequestPending As Boolean = False

    ' Returns true if a record was found and successfully processed.
    ' DRB 12/20/07: NOTE - One "success" case also returns false - the case where the record was processed fine but
    ' the Box Label or Connector Report was not printed because no printer was specified for the current login user.
    If Not modUtil.DDSReadFiles( _
        ErrorPrefix:="Schleuniger Controller", _
        FolderPath:=mdiCellRequestFolder.FullName, _
        FirstTryMS:=miWPCSFirstTryMS, _
        AutoRetryTimes:=miWPCSRetryTimes, _
        AutoRetryMs:=miWPCSRetrySleepMilliSeconds, _
        DoRetryQuestion:=mbWPCSDoPromptForRetry, _
        RetryQuestionTitle:="Reading Interceptor Cut Instructions for " & Me.msCutMachineName, _
        IsExtra:=CellRequestPending, _
        ErrInfo:=ErrInfo, _
        ErrCode:=ErrCode, _
        UpdateSuccessInfo:=UpdateSuccessInfo, _
        RemoveJobWithMissingArticle:=False, _
        SDCFolderPath:=mdiCellResponseFolder.FullName, _
        oDDSReadEventCallback:=AddressOf ProcessDDSReadRequest) Then

      ' Do special handling of different failure cases
      Select Case True
        Case ErrInfo.Contains("Do Not Process")
          ' A record was found but was not processed because no processing of files is done until the employee scans in.
          ' Override CellRequestPending because even if there were no "more" records after the one read, thre is still
          ' at least the one found pending.
          ' This is a common case which is separated out here because it does NOT call for a display message, etc.
          CellRequestPending = True
        Case ErrInfo.Contains("No record pending")
          ' This is a common case which is separated out here because it does NOT call for a display message, etc.
        Case Else
          ' This covers all the other failure cases. Not sure the ErrInfo>"" test is necessary but ...
          If ErrInfo > "" Then
            Dim sStatus As String = ErrorFilterForUsers(ErrInfo)
            Me.DisplayMessage("Error Processing Interceptor Instructions", sStatus, True)
            ErrInfo = "Error Processing Interceptor Instructions: " & ErrInfo
            sStatus = "Error Processing Interceptor Instructions: " & sStatus
            'MsgBox(ErrInfo, MsgBoxStyle.ApplicationModal Or MsgBoxStyle.Exclamation)
            If msLastCellRQErrInfo <> ErrInfo Then              ' This prevents the same error condition from popping up over and over if it has already been displayed.
              UpdateStatus(sStatus, True, True, ErrInfo)
              msLastCellRQErrInfo = ErrInfo
            End If
          End If
      End Select
    Else
      ' Success cases. 
      If ErrInfo > "" Then
        ' ErrInfo might still contain text, for example if reading the text file required multiple tries.
        If msLastCellRQErrInfo <> ErrInfo Then      ' This prevents the same message from cluttering the messaage list several times
          UpdateStatus(ErrInfo, False, False)
          msLastCellRQErrInfo = ErrInfo
        End If
      End If
      If UpdateSuccessInfo > "" Then
        ' There should always be something in UpdateSuccessInfo which may contain details from what was in the record read from the text file.
        ' This string is constructed specifically top be the Eventlog success message and the success message to appear in the running on screen event list.
        UpdateStatus(UpdateSuccessInfo, False, False)
      End If
    End If

    ' Indicate to the user whether there is still (after the above call) more pending in this text file to process.
    Me.lblCellRequestPending.Visible = CellRequestPending

  End Sub

  Private msLastCellRSErrInfo As String = ""   ' Prevents the same error from being posted to the error list or log twice in a row.
  Private Sub ReadCellResponse()
    ' Here you read from JOB.DDS and ARTICLE.DDS, extracting just one record from each.
    ' mbCellPending is set true if anything was observed in there that was not processed.
    ' Not processed could happen because there was something there but miOpEmployeeID=0.
    ' Not processed could happen because there was still more there besides the one being processed.

    Dim ErrInfo As String = ""
    Dim CellResponsePending As Boolean = False

    ' One of the things that can happen here is "Do Not Process" because no Operator is scanned in.
    If Not modUtil.SDCCheckFiles( _
        FolderPath:=mdiCellResponseFolder.FullName, _
        FirstTryMS:=miWPCSFirstTryMS, _
        AutoRetryTimes:=miWPCSRetryTimes, _
        AutoRetryMs:=miWPCSRetrySleepMilliSeconds, _
        DoRetryQuestion:=False, _
        RetryQuestionTitle:="Reading Cell Controller Response Files for " & Me.msCutMachineName, _
        IsPending:=CellResponsePending, _
        ErrInfo:=ErrInfo) Then
      Me.lblCellResponsePending.Visible = True
      If ErrInfo > "" Then
        If msLastCellRSErrInfo <> ErrInfo Then
          UpdateStatus(ErrInfo, True, True)
          msLastCellRSErrInfo = ErrInfo
        End If
      End If
    Else
      If ErrInfo > "" Then
        If msLastCellRSErrInfo <> ErrInfo Then
          UpdateStatus(ErrInfo, False, False)
          msLastCellRSErrInfo = ErrInfo
        End If
      End If
    End If

    Me.lblCellResponsePending.Visible = CellResponsePending

  End Sub

  Private Sub btnRetry_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnRetry.Click
    ' DRB 4/18/08 Now we reset oCayman to Nothing so that it recreates the object - this solves the problem whehn Cayman has to be restarted.
    oCayman = Nothing
    TimerScanIn.Stop()
    msLastCellRQErrInfo = ""
    msLastCellRSErrInfo = ""
    UpdateStatus("Retrying ...", False, False)
    StartWatchers()
    ReadPending()
    TimerScanIn.Start()
  End Sub

#End Region

#Region " Process Cell Requests "


  Private Function ProcessDDSReadRequest(ByVal sJob As String, ByVal sArticle As String, ByVal dsWPCS As WpcsSet, ByRef Errinfo As String, ByRef ErrCode As Integer, ByRef UpdateSuccessInfo As String) As Boolean
    ' This is called any time a cell controller request record is found.
    ' The job of this routine is to:
    ' 1) Do nothing if no operator is logged in
    ' 2) Pass the request through as is to the Cut Machine request folder

    ' DRB 1/7/08 In the CutInterceptor, no files are processed unless an operator is scanned in (though the app still does reads so that it can display the status info at the bottom)
    ' Here I comment this out because we will not normally expect an operator to be scanned in unless we decide to require it before pressing the retry button (not likely???)
    'If Me.miOpEmployeeID = 0 Then
    '    ' No operator is scanned in.
    '    Errinfo = "Do Not Process" ' This special case tells the caller to 
    '    Return False
    'End If

    ' DRB 1/7/08 Kyle, insert a test here that prevents processing of this request if you are still waiting on a response from the machine.
    'If (not waiting for Cut Machine) Then
    '    ' No operator is scanned in.
    '    Errinfo = "Do Not Process" ' Search on "Do Not Process" to see how this controls behavior.
    '    Return False
    'End If

    With dsWPCS.tblJob(0)
      Debug.WriteLine("Tag                  " & .Tag)
      Debug.WriteLine("JobKey               " & .JobKey)
      Debug.WriteLine("JobNumber            " & .JobNumber)
      Debug.WriteLine("ArticleKey           " & .ArticleKey)
      Debug.WriteLine("TotalPieces          " & .TotalPieces)
      Debug.WriteLine("BatchSize            " & .BatchSize)
      Debug.WriteLine("Name                 " & .Name)
      Debug.WriteLine("Hint                 " & .Hint)
      Debug.WriteLine("")
    End With
    If dsWPCS.tblJob(0).Tag.ToUpper = "[NEWJOB]" Then
      With dsWPCS.tblArticle(0)
        Debug.WriteLine("")
        Debug.WriteLine("Tag                  " & .Tag)
        Debug.WriteLine("ArticleKey           " & .ArticleKey)
        Debug.WriteLine("ArticleGroup         " & .ArticleGroup)
        Debug.WriteLine("NumberOfLeadSets     " & .NumberOfLeadSets)
        For Each oLead As WpcsSet.tblLeadRow In .GettblLeadRows
          With oLead
            Debug.WriteLine("")
            Debug.WriteLine("      Tag                  " & .Tag)
            Debug.WriteLine("      WireKey              " & CStr(.WireKey))
            Debug.WriteLine("      WireLength           " & CStr(.WireLength))
            Debug.WriteLine("      FontKey              " & CStr(.FontKey))
            Debug.WriteLine("      StrippingLengthLeft  " & CStr(.StrippingLengthLeft))
            Debug.WriteLine("      StrippingLengthRight " & CStr(.StrippingLengthRight))
            Debug.WriteLine("      PullOffLengthLeft    " & CStr(.PullOffLengthLeft))
            Debug.WriteLine("      PullOffLengthRight   " & CStr(.PullOffLengthRight))
            Debug.WriteLine("      TerminalKeyLeft      " & CStr(.TerminalKeyLeft))
            Debug.WriteLine("      TerminalKeyRight     " & CStr(.TerminalKeyRight))
                        Debug.WriteLine("      MarkingTextBegin          " & CStr(.MarkingTextBegin))
                        Debug.WriteLine("      MarkingTextEndless          " & CStr(.MarkingTextEndless))
                        Debug.WriteLine("      MarkingTextEnd         " & CStr(.MarkingTextEnd))
                    End With
        Next
      End With
    End If

    Select Case dsWPCS.tblJob(0).Tag.ToUpper
      Case "[NEWJOB]"
        UpdateSuccessInfo = "Cell Controller Job " & CStr(dsWPCS.tblJob(0).JobKey) & " (Hint=" & dsWPCS.tblJob(0).Hint & " Qty=" & CStr(dsWPCS.tblJob(0).TotalPieces) & " Leads=" & CStr(dsWPCS.tblArticle(0).NumberOfLeadSets) & ") - request sent to Cut Machine Data"
      Case Else
        UpdateSuccessInfo = "Job Tag " & dsWPCS.tblJob(0).Tag & " sent to Cut Machine Data"
    End Select

    ' Pass the Job through to the Cut Machine request folder
    ' DRB 1/8/08 Insert your own code here ...

    ' DRB 3/17/08 This routine sets Errinfo as needed and returns true if everything is fine.
    Try
      Me.ProcessSchlMachine(dsWPCS)
      Return True
    Catch ex As Exception
      Errinfo = ex.Message
      If Errinfo.ToUpper.Contains("RPC SERVER IS UNAVAILABLE") Then
        Errinfo = "Cannot talk to Cayman. If the Retry button does not work, check Cayman sorftware."
      End If
      Return False
    End Try

    'Return modUtil.DDSAppendFiles( _
    ' JobString:=sJob, _
    ' ArticleString:=sArticle, _
    ' FolderPath:=mdiCutRequestFolder.FullName, _
    ' FirstTryMS:=miWPCSFirstTryMS, _
    ' AutoRetryTimes:=miWPCSRetryTimes, _
    ' AutoRetryMs:=miWPCSRetrySleepMilliSeconds, _
    ' DoRetryQuestion:=mbWPCSDoPromptForRetry, _
    ' RetryQuestionTitle:="Writing Request to " & Me.msCutMachineName, _
    ' ErrInfo:=Errinfo)

  End Function

#End Region

#Region " Process Cut Machine Responses "

  'Private moSQLListener_WebSvc As SQLListener_WebSvc.SQLListenerWeb

  'Private Function ProcessSDCReadRequest(ByVal sJob As String, ByVal dsWPCS As WpcsSet, ByRef Errinfo As String, ByRef ErrCode As Integer, ByRef UpdateSuccessInfo As String) As Boolean
  '    Dim sContainer As String = ""

  '    ' Build the message that we want to show on screen and log if a read/process is successful.

  '    With dsWPCS.tblSDC(0)
  '        Debug.WriteLine("Tag                  " & .Tag)
  '        Debug.WriteLine("DateTimeStamp        " & .DateTimeStamp.ToString)
  '        Debug.WriteLine("JobKey               " & .JobKey)
  '        Debug.WriteLine("JobNumber            " & .JobNumber)
  '        Debug.WriteLine("ArticleKey           " & .ArticleKey)
  '        Debug.WriteLine("UserName             " & .UserName)
  '        Debug.WriteLine("JobRequestedPieces   " & .JobRequestedPieces)
  '        Debug.WriteLine("TotalGoodPieces      " & .TotalGoodPieces)
  '        Debug.WriteLine("Reason               " & .Reason)
  '    End With

  '    Select Case dsWPCS.tblSDC(0).Tag.ToUpper
  '        Case "[JOBSTARTED]"
  '            UpdateSuccessInfo = "Cut Job " & CStr(dsWPCS.tblSDC(0).JobKey) & " STARTED  (Qty=" & CStr(dsWPCS.tblSDC(0).JobRequestedPieces) & ") - response sent to Cell Controller Data"
  '        Case "[JOBTERMINATED]"
  '            Me.WindowState = FormWindowState.Normal
  '            Me.Focus()
  '            UpdateSuccessInfo = "Cut Job " & CStr(dsWPCS.tblSDC(0).JobKey) & " COMPLETED   (Qty=" & CStr(dsWPCS.tblSDC(0).JobRequestedPieces) & "," & CStr(dsWPCS.tblSDC(0).TotalGoodPieces) & ") - response processed and sent to Cell Controller Data"
  '            ErrCode = 0
  '            Errinfo = ""
  '            Do

  '                Do
  '                    Dim sOldErrInfo As String = Errinfo
  '                    ' DRB 12/27/07 Simplify what the user sees for some errors.
  '                    If sOldErrInfo Like "*already contains another assembly (*" Then
  '                        sOldErrInfo = sOldErrInfo.Substring(0, sOldErrInfo.IndexOf("(") - 1)
  '                    End If
  '                    Errinfo = ""
  '                    ErrCode = 0
  '                    sContainer = GetContainer("You are " & Me.msOpName & " (" & Me.msBadgeNumber & ")..." & vbCrLf & "Scan Container for Cut Job " & dsWPCS.tblSDC(0).JobKey & " (Qty=" & CStr(dsWPCS.tblSDC(0).JobRequestedPieces) & "," & CStr(dsWPCS.tblSDC(0).TotalGoodPieces) & ")", sOldErrInfo)
  '                    If sContainer = "LOGOUT" Then
  '                        If MsgBox("Scanning out as " & Me.msOpName & " (" & Me.msBadgeNumber & ") - OK?", MsgBoxStyle.SystemModal Or MsgBoxStyle.Question Or MsgBoxStyle.YesNo Or MsgBoxStyle.DefaultButton1) = MsgBoxResult.Yes Then
  '                            Errinfo = "Reading Cut Machine Response: User cancelled container scan to logout."
  '                            ErrCode = 1
  '                            Exit Do
  '                        End If
  '                    Else
  '                        Exit Do
  '                    End If
  '                Loop

  '                Dim RetryInfo As String = ""
  '                Dim TimerLog As String = ""
  '                Dim LogInfo As String = ""
  '                Dim ForceContainer As String = "N"
  '                Dim PrintBoxLabel As String = ""            ' BLANK means 

  '                If ErrCode = 0 Then

  '                    Try
  '                        ' ErrCode=5 means rescan container#.
  '                        ' ErrCode=2 means throw user back to not scanned in.
  '                        ' ErrCode=3 means serious data error - forward the response file anyway.
  '                        moSQLListener_WebSvc.ProcessScan( _
  '                            ConnectionString:=msConnectionString, _
  '                            RetrySleepMilliSeconds:=miSQLRetrySleepMilliseconds, _
  '                            RetryUntilSeconds:=miSQLRetryUntilSeconds, _
  '                            MessageType:="CUT", _
  '                            CutMachineID:=miCutMachineID, _
  '                            EmployeeBadge:=CInt(msBadgeNumber), _
  '                            JobSubAssyID:=CInt(dsWPCS.tblSDC(0).ArticleKey), _
  '                            ContName:=sContainer, _
  '                            PrintBoxLabel:="", _
  '                            ForceContainer:="N", _
  '                            CutUserID:=miUserID, _
  '                            BLPrinterName:=msBoxPrinter, _
  '                            CONNPrinterName:=msConnPrinter, _
  '                            EmployeeName:=msOpName, _
  '                            EmployeeID:=miOpEmployeeID, _
  '                            LTStartTime:=mdLTStartTime, _
  '                            OTBID:=miOTBID, _
  '                            TimeTxnID:=miTimeTxnID, _
  '                            StartTime:=mdStartTime, _
  '                            ErrCode:=ErrCode, _
  '                            ErrInfo:=Errinfo, _
  '                            LogInfo:=LogInfo, _
  '                            RetryInfo:=RetryInfo, _
  '                            TimerLog:=TimerLog)
  '                    Catch ex As Exception
  '                        Errinfo = "ERROR in moSQLListener_WebSvc.ProcessScan call: msBadgeNumber=[" & msBadgeNumber & " dsWPCS.tblSDC(0).ArticleKey=[" & dsWPCS.tblSDC(0).ArticleKey & "] " & ex.Message
  '                        If modUtil.ErrorFilterForUsers(ex.Message).StartsWith("Sql Server") OrElse modUtil.ErrorFilterForUsers(ex.Message).StartsWith("Sql Listener Web Service") Then
  '                            ErrCode = 5
  '                        Else
  '                            ErrCode = 1
  '                        End If
  '                    End Try

  '                    If RetryInfo > "" Then
  '                        LogEvent(RetryInfo)
  '                    End If

  '                End If

  '                If ErrCode <> 5 Then
  '                    Exit Do
  '                End If

  '            Loop

  '        Case "[JOBABORTED]"
  '            UpdateSuccessInfo = "Cut Job " & CStr(dsWPCS.tblSDC(0).JobKey) & " ABORTED  (Qty=" & CStr(dsWPCS.tblSDC(0).JobRequestedPieces) & "," & CStr(dsWPCS.tblSDC(0).TotalGoodPieces) & ") - response sent to Cell Controller Data"
  '        Case Else
  '            UpdateSuccessInfo = "Feedback Tag " & dsWPCS.tblSDC(0).Tag & " sent to Cell Controller Data"
  '    End Select

  '    If ErrCode = 0 OrElse ErrCode = 3 OrElse ErrCode = 4 Then
  '        Dim sOldErrInfo As String = Errinfo
  '        Dim iOldErrCode As Integer = ErrCode
  '        Errinfo = ""
  '        ErrCode = 0
  '        ' Pass the Job through to the Cell Controller response folder
  '        Dim bReturn As Boolean = modUtil.SDCAppendFiles( _
  '                                     JobString:=sJob, _
  '                                     FolderPath:=mdiCellResponseFolder.FullName, _
  '                                     FirstTryMS:=miWPCSFirstTryMS, _
  '                                     AutoRetryTimes:=miWPCSRetryTimes, _
  '                                     AutoRetryMs:=miWPCSRetrySleepMilliSeconds, _
  '                                     DoRetryQuestion:=mbWPCSDoPromptForRetry, _
  '                                     RetryQuestionTitle:="Writing Response to Cell Controller for " & Me.msCutMachineName, _
  '                                     ErrInfo:=Errinfo)
  '        ErrCode = iOldErrCode
  '        If sOldErrInfo > "" Then
  '            If Errinfo > "" Then
  '                Errinfo &= "; "
  '            End If
  '            If iOldErrCode = 3 Then
  '                Errinfo &= "Data Problem:" & sOldErrInfo
  '            Else
  '                Errinfo &= sOldErrInfo
  '            End If
  '        End If
  '        If iOldErrCode > 0 Then
  '            bReturn = False
  '        End If
  '        Return bReturn
  '    Else
  '        Return False
  '    End If

  'End Function

#End Region

#Region " Helpers "


  Private Sub DisplayMessage(ByVal Title As String, ByVal Message As String, ByVal MessageIsError As Boolean)
    ForceToTop()
    Dim frm As New frmInput
    With frm
      .IsMsgBox = True
      .Title = Title
      .Heading = Message
      .HeadingColor = Color.Black
      .FormBackColor = Color.Red
      '.ButtonBackColor = Color.LightGray
      .ShowDialog(Me)
      frm.Dispose()
    End With

    'Dim sInput As String = ""
    'Do
    '    sInput = InputBox(ErrorMessage, Title, "", Me.Left + 200, Me.Top + 200)
    '    If sInput = "ACK" Then
    '        Exit Sub
    '    End If
    'Loop
  End Sub

  Public Function LogEvent(ByVal LogEntry As String, Optional ByVal DoNTLog As Boolean = True) As Integer
    'MsgBox("Watcher.Logevent:LogEntry=[" & LogEntry & "] cs=[" & msConnectString & "] station=[" & msStationID & "]")
    If mbLogEvents Then
      If mbSkipAuthentication = False Then
        Return WriteEventLog(LogEntry, Me.msConnectionString, Me.msCutMachineName, miUserID, DoNTLog)
      Else
        AppendTextToFile(msCutMachineName & " : " & Format(Now, "ddd MM/dd/yyyy hh:mm:ss tt") & "  " & LogEntry & vbCrLf, msEventLogTextFile)
      End If
    End If
  End Function

#End Region

#Region " Message Grid "

  Private moErrorBrush As Brush
  Private moNormalBrush As Brush
  Private mtblMessage As DataTable
  Private mtblMessagesReversed As DataTable
  Private miMaxMessageRows As Integer = 16
  Private mbUpdatingStatus As Boolean

  Private Sub UpdateStatus(ByVal StatusString As String, Optional ByVal IsError As Boolean = False, Optional ByVal DoNTLog As Boolean = True, Optional ByVal LogError As String = "")
    'MsgBox("UpdateStatus:" & StatusString)
    'Add this entry to the table.
    If mbUpdatingStatus Then
      Exit Sub
    End If
    Try
      mbUpdatingStatus = True
      If LogError = "" Then
        LogError = StatusString
      End If
      If StatusString > "" Then
        With mtblMessage
          ' First clear the oldest row
          If .Rows.Count >= miMaxMessageRows Then
            .Rows(0).Delete()
            .AcceptChanges()
          End If
          Dim oRow As DataRow = .NewRow
          With oRow
            .Item("MessageString") = StatusString
            .Item("MessageType") = IIf(IsError, "ERROR", "MESSAGE")
            .Item("MESSAGETIME") = Format(Now, "ddd hh:mm:ss tt")
          End With
          .Rows.Add(oRow)
          .AcceptChanges()
        End With
        RefreshLog()
        'Dim i1 As Integer
        'i1 = mdvMessage.Count
        'i1 = mtblMessage.Rows.Count
        'i1 = mdvMessage.Count
        'mdvMessage = New DataView(mtblMessage, "", "MessageID DESC", DataViewRowState.None)
        'i1 = mdvMessage.Count
        'mdvMessage = New DataView(mtblMessage, "true", "MessageID DESC", DataViewRowState.CurrentRows)
        'i1 = mdvMessage.Count
      End If
    Catch ex As Exception
      Exit Sub
    Finally
      mbUpdatingStatus = False
      Try
        LogEvent(LogError, DoNTLog)
      Catch ex As Exception
      End Try
    End Try
  End Sub

  Private Sub RefreshLog()

    ' Do this to disconnect the table from the CGrid stuff - otherwise I got errors during merge in fill.
    dgEvents.DataSource = Nothing

    Dim oRow As DataRow
    Dim oOldRow As DataRow

    mtblMessagesReversed.Clear()
    With mtblMessagesReversed
      For i As Integer = mtblMessage.Rows.Count - 1 To 0 Step -1
        oOldRow = mtblMessage.Rows(i)
        oRow = .NewRow
        With oRow
          .Item("MessageString") = oOldRow("MessageString")
          .Item("MessageType") = oOldRow("MessageType")
          .Item("MESSAGETIME") = oOldRow("MESSAGETIME")
        End With
        .Rows.Add(oRow)
      Next
    End With
    DefineAndBindGrid(mtblMessagesReversed)

  End Sub

  Private Sub DefineAndBindGrid(ByVal oTable As DataTable)
    'Dim BoolCol As New ClickableBooleanColumn

    moErrorBrush = Brushes.Yellow
    moNormalBrush = Brushes.Silver

    With dgEvents

      .TableStyles.Clear()
      .RowHeadersVisible = False
      .RowHeaderWidth = 0
      .AllowNavigation = True
      .Enabled = True
      .ReadOnly = True


      'Create a TableStyle to contain the columnstyles for the grid.
      '(An alternate method for doing this is at the end of this method)
      Dim ts As DataGridTableStyle = CGrid.GetTableStyle(oTable)

      ''TextBox Column
      'Dim cs2 As New CGridTextBoxStyle("StationID", 110, _
      '                                  HorizontalAlignment.Left, True, _
      '                                  "Station", String.Empty, "")
      'AddHandler cs2.SetCellFormat, AddressOf FormatGridRow
      'CGrid.AddColumn(ts, cs2)


      'TextBox Column
      Dim cs3 As New CGridTextBoxStyle("MessageTime", 120, _
                                        HorizontalAlignment.Left, True, _
                                        "Time", String.Empty, "MM/dd/yyyy hh:mm:ss")
      AddHandler cs3.SetCellFormat, AddressOf FormatGridRow
      CGrid.AddColumn(ts, cs3)


      ' Multiline TextBox
      Dim cs7 As New CGridMultiLineTextBoxStyle("MessageString", 596, _
                                                HorizontalAlignment.Left, _
                                                True, "Cut Event", "")
      AddHandler cs7.SetCellFormat, AddressOf FormatGridRow
      CGrid.AddColumn(ts, cs7)

      ts.AllowSorting = True
      ts.AlternatingBackColor = Color.LightGray

      'Turn off the title bar for the grid
      dgEvents.CaptionVisible = False

      'Set the TableStyle for the Grid
      CGrid.SetGridStyle(dgEvents, CType(oTable, DataTable), ts)

      'Uncomment this line to prevent the user from adding rows to the grid
      CGrid.DisableAddNew(dgEvents, Me)

    End With

  End Sub

  Private Sub FormatGridRow(ByVal sender As Object, ByVal e As DataGridFormatCellEventArgs)
    ' Conditionally set properties in e depending upon e.Row and e.Col.
    'Dim bIsCurrent As Boolean = (e.Row = Me.dgEvents.CurrentRowIndex)
    Dim Row As DataRow = mtblMessagesReversed.Rows(e.Row)

    If Row("MessageType") = "ERROR" Then
      e.BackBrush = moErrorBrush
    Else
      e.BackBrush = moNormalBrush
    End If

  End Sub

#End Region

#Region " Timer "

  Private miTimerIntervalMS As Integer = 100
  Private miTimerCount As Integer = 0

  Private Sub TimerScanIn_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TimerScanIn.Tick
    'TimerScanIn.Interval = miTimerIntervalMS
    Try
      TimerScanIn.Stop()
      Do While Me.mqFilesChanged.Count > 0
        ReadFilePending(mqFilesChanged.Dequeue())
      Loop
    Catch ex As Exception
    Finally
      TimerScanIn.Start()
    End Try
  End Sub

#End Region

End Class

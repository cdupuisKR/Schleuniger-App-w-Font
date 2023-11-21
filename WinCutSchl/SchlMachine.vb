Imports Trilogy.KR.Wiretrac.LibRouting
Imports System.Data.SqlClient
Imports System.Data.SqlTypes
Imports System.Configuration
Imports Trilogy.LibTools
Imports Trilogy.KR.Wiretrac.LibPermissions.modPermissions

Partial Class frmCutSchl

  Public WithEvents oCayman As Cayman.Wirelist
  'Public WithEvents oCay As Cayman.ICaymanMachineInfo_SinkHelper



  Public Enum CayRV As Short
    ' Best case, everything was ok
    NOERROR = 0
    'The passed parameters were truncated before execution but the command was successful.
    ' (For example: "SetBatch" tried to set a batch size bigger than the "Total" value of the wire)
    PARAMETER_TRUNCATED = 1
    ' The execution failed
    FAILED = 2
    ' A limit was reached but the function was executed without any error.
    ' (For example: "ShowPrevious" command is used on the first wire)
    LIMIT_REACHED = 3
    ' Function is not usable under the current conditions
    ' (For example: "SetTotal" command is used on an empty Wire List)
    NOT_USABLE = 4
    ' A selection of a not existing item was made.
    ' (For example: "SetRawMaterial" uses a name not listed in the Raw Material library)
    NOT_EXISTING = 5
    ' An error in the passed parameters ( For example: out of range) makes command execution impossible
    PARAMETER_ERROR = 6
  End Enum

  Public Enum CayState As Short
    OFFLINE = 0
    CONNECTING = 1
    ONLINE = 2
    WIRE_LIST_RUNNING = 3
    MSG_FROM_MACHINE = 4
    LOAD_UNLOAD_CLOSE_OPEN_CUT_FEED = 5
    LOAD_UNLOAD_CLOSE_OPEN_CUT_FEED_MSG_FROM_MACHINE = 6
    WIRE_COMPLETE = 10
    BATCH_COMPLETE = 11
    TOTAL_COMPLETE = 12
    LIST_TOTAL_COMPLETE = 13
    GRAND_TOTAL_COMPLETE = 14
    PRODUCTION_COMPLETE = 15
    CLOSING = 99
  End Enum

  Public Function CaymanState(ByVal RV As Short) As String
    Select Case RV
      Case 0 : Return "Machine is offline"
      Case 1 : Return "Cayman is trying to connect or is in the process of connecting"
      Case 2 : Return "Machine is online and waiting"
      Case 3 : Return "Machine is running the current Wire List"
      Case 4 : Return "The production waits with a 'message from machine' dialog"
      Case 5 : Return "Machine performs load/unload, close/open, cut or feed"
      Case 6 : Return "Machine waits with a 'message from machine' dialog during load/unload, close/open, cut or feed"
      Case 10 : Return "A wire was produced complete"
      Case 11 : Return "A batch was produced complete"
      Case 12 : Return "A total was produced complete"
      Case 13 : Return "A list total was produced complete"
      Case 14 : Return "A grand total was produced complete"
      Case 15 : Return "The production was produced complete"
      Case 99 : Return "Cayman is closing"
      Case Else : Return "ERROR"
    End Select
  End Function

  ' Event fires whenever Cayman state changes as described in the above enum.  Used to catch when the machine finishes cutting a job handed to it.
  Private Sub CaymanStateChanged(ByVal State As Short) Handles oCayman.OnStateChanged, oCayman.OnSetUserDefinedCommand, oCayman.OnUserDefinedCommandSelected


    Dim sJob As String = ""
    Dim ArticleKey As String = ""
    Dim ErrInfo As String = ""
    ' This grabs the name (article key)

    If Not mbRespondToMachineEvents Then
      Exit Sub
    End If

    oCayman.sWireGetName(ArticleKey)

    Debug.WriteLine("CaymanStateChanged State=" & State.ToString)
    Dim p As Integer = 0
    Debug.WriteLine(CStr(oCayman.sListGetListPasses(p)))
    Debug.WriteLine("Cayman GetListPasses=" & CStr(p))

    Dim lng As Integer
    oCayman.sListGetStopConditions(lng)
    Debug.WriteLine("sListGetStopConditions:" & CStr(lng))

    ' Production complete means *everything* in the current showing wire list in Cayman is finished
    ' Construct SDC strings
    If State = CayState.TOTAL_COMPLETE Then
      sJob =
        "[JobTerminated]" & vbCrLf _
        & "ArticleKey = " & ArticleKey & vbCrLf
    ElseIf CaymanState(State) = "ERROR" Then
      sJob =
        "[JobAborted]" & vbCrLf _
        & "ArticleKey = " & ArticleKey & vbCrLf
    Else
      Exit Sub
      ' Cannot throw an exception... all other state changes fall here
    End If

    ' APPPEND ABORT 
    ' DRB 3/14/08 Modified the date format here (used to be "MM/dd/yyyyy  hh:mm:ss")
    'Dim sSDC As String = "[JobAborted]" & vbCrLf _
    '    & "DateTimeStamp = " & Format(Now, "dd/MM/yyyy,hh:mm:ss") & vbCrLf _
    '    & "Job = " & dsWPCS.tblJob(0).JobKey & "," & dsWPCS.tblJob(0).JobNumber & vbCrLf _
    '    & "ArticleKey = " & dsWPCS.tblJob(0).ArticleKey & vbCrLf _
    '    & "UserName = bwhite" & vbCrLf _
    '    & "JobRequestedPieces = " & CStr(dsWPCS.tblJob(0).TotalPieces) & vbCrLf _
    '    & "TotalGoodPieces = 0" & vbCrLf _
    '    & "Reason = 3" & vbCrLf

    ' Append the DDS file
    Dim bOK As Boolean = modUtil.SDCAppendFiles(
                                     JobString:=sJob,
                                     FolderPath:=mdiCellResponseFolder.FullName,
                                     FirstTryMS:=miWPCSFirstTryMS,
                                     AutoRetryTimes:=miWPCSRetryTimes,
                                     AutoRetryMs:=miWPCSRetrySleepMilliSeconds,
                                     DoRetryQuestion:=mbWPCSDoPromptForRetry,
                                     RetryQuestionTitle:="Writing Response to Cell Controller for " & Me.msCutMachineName,
                                     ErrInfo:=ErrInfo)
    If Not bOK Then
      ' More error handling if needed
      Throw New Exception(ErrInfo)
    End If
    '    oCayman.sListDeleteWire()
  End Sub

  ' Called every time this app connects to machine, but hopefully should only 'connect' once
  Private Sub AttachToCayman()


    If oCayman Is Nothing Then
      Try
        ' Try to attach to Cayman
        oCayman = GetObject(, "Cayman.Wirelist")
      Catch ex As Exception
        Try
          'Create Cayman as an new object
          oCayman = CreateObject("Cayman.Wirelist")
        Catch ex1 As Exception
          If ex1.Message = "asss" Then
            Throw New Exception("Could not attach to Cayman software.")
          Else
            Throw New Exception("Error attaching to Cayman software: " & ex1.Message)
          End If
        End Try
      End Try
    End If

  End Sub

  ' Called when DDS file read
  Private Sub ProcessSchlMachine(ByVal ds As WpcsSet)

        Me.AttachToCayman()
        Dim Sep() As String = {","}

        For Each oLead As WpcsSet.tblLeadRow In ds.tblLead.Rows
      Try
        With ds.tblJob(0)

                    ' Test ...
                    'Dim s As Short = 0
                    'oCayman.sListSetProductionOrder(s)
                    oCayman.sGeneSetUserDefinedCommand(1, "MyCommand")

          oCayman.sListNewWirelist()

          oCayman.sListInsertWire()
          ' Hide the article key as the wire name, no one cares about the wire name on the screen...?

          ' DRB 4/18/08 Used to be tblArticle(0).ArticleKey, but ALSO ArticleKey incorrectly held ArticleGroup instead of ArticleKey!
          oCayman.sWireSetName(ds.tblJob(0).Name) ' Currently oLead doesn't carry ArticleKey - is this normal?

          'oCayman.sWireSetName(oLead.wireid) ' Currently oLead doesn't carry ArticleKey - is this normal?

          oCayman.sWireSetRawMaterial(oLead.WireKey) ' ***add raw material ***
                    oCayman.sWireSetLength(oLead.WireLength, 0)
                    'Won't run if wire length is less than 110
                    If oLead.MarkingTextBegin <> "" And oLead.WireLength > 109 Then
                        Dim value As String = oLead.MarkingTextBegin
                        Dim x As Double
                        Dim fontEx As Int32
                        fontEx = 0
                        Dim re As SqlDataReader
                        Dim SplitBeg() = value.Split(Sep, StringSplitOptions.None)
                        'Calculates estimate of length of string
                        Dim arr() As Double = {2.4}
                        If Len(SplitBeg(1)) = 30 Then
                            x = 72
                        Else
                            x = Len(SplitBeg(1)) * arr(0)
                        End If

                        'Dim con As String = GetCNFConnectionString("DBConnectTypeCrimp")
                        'Generate Connection Factory to be able to connect to dev server
                        Dim con As String = GetConnectionString(ConfigurationManager.AppSettings("CNFServer"), ConfigurationManager.AppSettings("CNFDatabase"), ConfigurationManager.AppSettings("CNFTable"), ConfigurationManager.AppSettings("DBConnectTypeCrimp"))
                        'Dim connect = ConfigurationManager.AppSettings(con)
                        'Dim conn As String = ConnectionStringWithPasswordRemoved(con)
                        Dim sqlConn As New SqlConnection(con)
                        'Try Query that searches dbo.FontSizeGauge for connecting wirekey and returns font size correlated
                        Try
                            sqlConn.Open()
                            Dim font As New SqlCommand("SELECT Font_Size FROM dbo.tblFontSizeGauge WHERE KR_Num = '" & oLead.WireKey & "'", sqlConn)
                            'MsgBox(font.ToString())
                            re = font.ExecuteReader()
                            'If re.HasRows Then
                            ' MsgBox("Rows")
                            'Else
                            'MsgBox("NO")
                            'End If
                            'MsgBox(re.GetName(0))
                            re.Read()
                            fontEx = re.Item("Font_Size")
                            re.Close()
                            'fontEx = re.GetInt32(0)
                            're.Close()
                            'fontEx = re.Item("tblFontSizeGauge.[Font_Size]").ToString()
                            'fontEx = font.ExecuteNonQuery()
                            'Return fontEx
                            'Catch ex As Exception
                            'fontEx = 5
                            'MsgBox("Font Defaulted to 5. The WireKey is not in the font database. Please contact Daniel Nelson with the Wirekey")
                            'Err.Raise(2000, "", "The WireKey is not in the database. Please contact Daniel Nelson")
                            'If query doesn't work sets font to 5 and tells user to connect Daniel with wirekey to update
                        Finally
                            If fontEx = 0 Then
                                fontEx = 5
                                MsgBox("Font Defaulted to 5. The WireKey is not in the font database. Please contact Daniel Montanez or Bonnie Clark with the Wirekey")
                            End If
                            sqlConn.Close()
                        End Try

                        Dim Ed As Double = oLead.WireLength - oLead.MarkingTextEnd
                        Dim str As String = oLead.FontKey
                        Dim Res As String
                        Dim num As Int32
                        Dim ori As Int32
                        Dim prin As Int32
                        If SplitBeg(2) = 0 Then
                            ori = 96
                        Else
                            ori = -1
                        End If
                        If oLead.FontKey <> "" Then
                            For Each c As Char In str
                                If IsNumeric(c) Then
                                    Res = Res & c
                                End If
                            Next
                        End If

                        Dim cal As Double = x + oLead.MarkingTextEndless
                        Dim cal2 As Double = x + oLead.MarkingTextEnd
                        Dim fin As Double = oLead.WireLength - (cal2 + (1.05 * x))
                        Dim testString1 As String = oLead.FontKey
                        'Checks to see whether the ink is White or Black and tell which printer to use
                        Dim charArray() As Char = testString1.ToCharArray
                        If oLead.FontKey <> "" Then
                            If charArray(0) = "B" Then
                                num = 0
                                prin = 0
                            Else
                                num = 255
                                prin = 1
                            End If
                        Else
                            num = 0
                            prin = 0
                        End If
                        'Print for big wires
                        If oLead.WireLength > 164 Then
                            'oCayman.sWireInsertIJTextAreaEx(1, SplitBeg(0), SplitBeg(1), 2, -1, Ed, 1, 1, 5, ori, num, num, num, 0, prin)
                            oCayman.sWireInsertIJTextAreaEx(0, cal2, SplitBeg(1), 2, -1, oLead.WireLength, 1, 1, fontEx, ori, num, num, num, 0, prin)
                            oCayman.sWireInsertIJTextAreaEx(1, SplitBeg(0), SplitBeg(1), 0, cal, fin, -1, 1, fontEx, ori, num, num, num, 0, prin)
                            'Print for smaller wires that only need 2 labels
                        ElseIf oLead.WireLength >= 110 And oLead.WireLength <= 164 Then
                            oCayman.sWireInsertIJTextAreaEx(1, SplitBeg(0), SplitBeg(1), 2, -1, Ed, 1, 1, fontEx, ori, num, num, num, 0, prin)
                            oCayman.sWireInsertIJTextAreaEx(0, cal2, SplitBeg(1), 2, -1, oLead.WireLength, 1, 1, fontEx, ori, num, num, num, 0, prin)

                        End If

                        ' Test ...
                    End If

                    oCayman.sWireSetTotal(Math.Max(.TotalPieces, 1))
                    oCayman.sWireSetBatch(Math.Max(.BatchSize, 1))

                    ' Replaced ...
                    'If .TotalPieces > 0 Then oCayman.sWireSetTotal(.TotalPieces)
                    'If .BatchSize > 0 Then oCayman.sWireSetBatch(.BatchSize) ' Comment out if causing problems with batches



                    If oLead.StrippingLengthLeft > 0 Then
                        ' DRB 9/3/08 Addeds this business logic to control slug removal:
                        ' If PullOffLength is >0 and < StrippingLength then pass along the PullOffLength as sent; 
                        ' else "Remove the Slug" by sending PullOffLength = StrippingLength + 8
                        If oLead.PullOffLengthLeft <= 0 OrElse oLead.PullOffLengthLeft >= oLead.StrippingLengthLeft Then
                            oLead.PullOffLengthLeft = oLead.StrippingLengthLeft + 8
                        End If

                        ' DRB 9/3/08 Do NOT round small pullofflengths up to 1.
                        'oCayman.sWireInsertOpElement(0, 1, 0, 0, oLead.StrippingLengthLeft, Math.Max(oLead.PullOffLengthLeft, 1), -1, -1, 0, 0)
                        oCayman.sWireInsertOpElement(0, 1, 0, 0, oLead.StrippingLengthLeft, oLead.PullOffLengthLeft, -1, -1, 0, 0)
                    End If
                    If oLead.StrippingLengthRight > 0 Then
            ' DRB 9/3/08 Addeds this business logic to control slug removal:
            ' If PullOffLength is >0 and < StrippingLength then pass along the PullOffLength as sent; 
            ' else "Remove the Slug" by sending PullOffLength = StrippingLength + 8
            If oLead.PullOffLengthRight <= 0 OrElse oLead.PullOffLengthRight >= oLead.StrippingLengthRight Then
              oLead.PullOffLengthRight = oLead.StrippingLengthRight + 8
            End If

            ' DRB 9/3/08 Do NOT round small pullofflengths up to 1.
            'oCayman.sWireInsertOpElement(1, 1, 0, 0, oLead.StrippingLengthRight, Math.Max(oLead.PullOffLengthRight, 1), -1, -1, 0, 0)
            oCayman.sWireInsertOpElement(1, 1, 0, 0, oLead.StrippingLengthRight, oLead.PullOffLengthRight, -1, -1, 0, 0)
          End If

          ' TEST....
          'oCayman.sProdRun()

        End With
      Catch ex As Exception
        ' Add error handling here
        Throw ex
      End Try
    Next
  End Sub

End Class

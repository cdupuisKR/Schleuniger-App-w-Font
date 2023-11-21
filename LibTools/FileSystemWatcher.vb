Imports System.IO

Public Class FileSystemWatcher
  ' DRB 8/8/09 Used in WireTrac

  Private WithEvents objFsw As IO.FileSystemWatcher

  Public Event FileCreated(ByVal FullPath As String)
  Public Event FileDeleted(ByVal FilePath As String)
  Public Event FileChanged(ByVal FullPath As String)
  Public Event FileRenamed(ByVal OldFileName As String, ByVal newFileName As String)
  Public Event FileWatchError(ByVal ErrMsg As String)

  Public Sub New()
    objFsw = New IO.FileSystemWatcher()
    'Watch all changes
    objFsw.NotifyFilter = IO.NotifyFilters.Attributes Or _
                                       IO.NotifyFilters.CreationTime Or _
                                       IO.NotifyFilters.DirectoryName Or _
                                       IO.NotifyFilters.FileName Or _
                                       IO.NotifyFilters.LastWrite Or _
                                       IO.NotifyFilters.Security Or _
                                       IO.NotifyFilters.Size
    'Can also use io.notifyfilter.LastAccess but
    'that raises event every time file is accessed.
    'just looking for changes in this example



  End Sub

  Public Property FolderToMonitor() As String 'Folder to monitor
    'Simple Demo so I am only allowing
    'one folder to be monitored at a time


    Get
      FolderToMonitor = objFsw.Path
    End Get
    Set(ByVal Value As String)
      If Right(Value, 1) <> "\" Then Value = Value & "\"
      If IO.Directory.Exists(Value) Then
        objFsw.Path = Value
      End If
    End Set
  End Property
  Public Property IncludeSubfolders() As Boolean
    Get
      IncludeSubfolders = objFsw.IncludeSubdirectories
    End Get
    Set(ByVal Value As Boolean)
      objFsw.IncludeSubdirectories = Value
    End Set
  End Property


  Public Function StartWatch() As Boolean
    Dim bAns As Boolean = False
    Try
      objFsw.EnableRaisingEvents = True
      bAns = True
    Catch ex As Exception
      RaiseEvent FileWatchError(ex.Message)
    End Try
    Return bAns
  End Function

  Public Function StopWatch() As Boolean
    Dim bAns As Boolean = False
    Try
      objFsw.EnableRaisingEvents = False
      bAns = True
    Catch ex As Exception
      RaiseEvent FileWatchError(ex.Message)
    End Try
    Return bAns
  End Function

  'WithEvents Keyword automatically gives you the
  'signature for these subs


  Private Sub objFsw_Created(ByVal sender As Object, ByVal e As System.IO.FileSystemEventArgs) Handles objFsw.Created
    RaiseEvent FileCreated(e.FullPath)
  End Sub

  Private Sub objFsw_Changed(ByVal sender As Object, ByVal e As System.IO.FileSystemEventArgs) Handles objFsw.Changed
    'Debug_Writeline("objFsw_Changed:" & e.FullPath)
    RaiseEvent FileChanged(e.FullPath)
  End Sub

  Private Sub objFsw_Renamed(ByVal sender As Object, ByVal e As System.IO.RenamedEventArgs) Handles objFsw.Renamed
    RaiseEvent FileRenamed(e.OldFullPath, e.FullPath)
  End Sub

  Private Sub objFsw_Deleted(ByVal sender As Object, ByVal e As System.IO.FileSystemEventArgs) Handles objFsw.Deleted
    RaiseEvent FileDeleted(e.FullPath)
  End Sub

  Private Sub objFsw_Error(ByVal sender As Object, ByVal e As System.IO.ErrorEventArgs) Handles objFsw.Error
    RaiseEvent FileWatchError(e.GetException.Message)
  End Sub
End Class

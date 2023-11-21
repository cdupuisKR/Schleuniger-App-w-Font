

' DRB 2/15/12 Now the Role/Function list loaded into a UserPrincipal is an arraylist of strings representing tblFunction.SourceCodeKey instead of 
' tblFunction.Name.

Public Module modPermissions

  Public Enum Permissions
    ' DRB 2/15/12 Note that these enumeration integers happen to correspond with the PKs of the tblFunction rows, but this would work jsut as well
    ' if they did not. All that is important is that UserHasPermission() uses the exact strings in SourceCodeKey.

    NoPermissionsRequired = 0        ' DRB Added per Kyle 6/15/09   DRB Changed to NoPermissionsRequired from None 6/19/09
    RunReworkKiosk = 1
    WorkTypeMaint = 2
    BatchCreate = 3
    JobTreeUtilityView = 4
    JobDetailTab = 5
    ChangeContainer = 6
    EmployeeMaint = 7
    TerminalMaint = 8
    JobTreeTimeView = 9
    Floor = 10
    Point = 11
    'EditMasterData = 12       ' DRB 6/2/09 Removed as per Kyle, for MasterEdit
    SystemAdministration = 13
    CutInterceptor = 14
    CutQueueManager = 15      ' DRB 2/21/12 Deprecated - app not used.
    BreakoutOperator = 16

    'BreakoutRegen = 17        ' DRB 3/12/12 Per FB 1432, deprecated function. All breakouts users are allowed this function.

    ' DRB 6/2/09 Added 18 - 21 for MasterEdit, per Kyle
    EditHarness = 18
    EditWire = 19
    EditTerminal = 20
    EditConnector = 21
    EditImage = 37        ' DRB 2/4/19 FB 2606

    ' DRB 2/11/10 Added to support the new CAN Printer WinApp.
    HeatShrinkLabelServer = 22

    ' DRB 11/1/10 Added to support the new UseCutAllowApp function for CutAllowmaintenance
    UseCutAllowApp = 23
    UserMaint = 24

    ' DRB 1/24/12 
    EquipmentBasic = 25
    EquipmentSecureOps = 26

    ' DRB 2/21/12 Added for Detach mode
    EnableDetachedMode = 27
    RoleFunctionMaint = 28


    ' DRB 2/4/19 These values adjusted to match the actual tblFunction values
    'CreateRework = 29
    'ManageShortages = 30
    'DeleteJobs = 31
    'BreakoutsViewOnly = 32
    'DataChangeRequests = 33

    ' DRB 3/12/12 added to support an EMPLOYEE type function for Rework. separate from User Rework permission.
    CreateRework = 30
    ' DRB 9/26/13 added a function for ManageShortages 
    ManageShortages = 31
    DeleteJobs = 33
    BreakoutsViewOnly = 34
    DataChangeRequests = 35

    KeyenceMeasurement = 36

  End Enum

  Public Function UserHasPermission(ByVal Principal As PersonPrincipal, ByVal iFunction As Integer) As Boolean

    Select Case iFunction
      Case Permissions.NoPermissionsRequired
        Return True
      Case Permissions.RunReworkKiosk
        Return Principal.HasFunction("RunReworkKiosk")
      Case Permissions.WorkTypeMaint
        Return Principal.HasFunction("WorkTypeMaint")
      Case Permissions.BatchCreate
        Return Principal.HasFunction("BatchCreate")
      Case Permissions.JobTreeUtilityView
        Return Principal.HasFunction("JobTreeUtilityView")
      Case Permissions.JobDetailTab
        Return Principal.HasFunction("JobDetailTab")
      Case Permissions.ChangeContainer
        Return Principal.HasFunction("ChangeAssignedContainer")
      Case Permissions.EmployeeMaint
        Return Principal.HasFunction("EmployeeMaint")
      Case Permissions.TerminalMaint
        Return Principal.HasFunction("TerminalMaint")
      Case Permissions.JobTreeTimeView
        Return Principal.HasFunction("JobTreeTimeView")
      Case Permissions.Floor
        Return Principal.HasFunction("Floor")
      Case Permissions.Point
        Return Principal.HasFunction("Point")

        ' DRB 6/2/09 Removed as per Kyle, for MasterEdit
        'Case Permissions.EditMasterData
        '  Return Principal.HasFunction("Edit Master Data")

      Case Permissions.SystemAdministration
        Return Principal.HasFunction("SystemAdmin")
      Case Permissions.CutInterceptor
        Return Principal.HasFunction("CutInterceptor")
      Case Permissions.CutQueueManager
        Return Principal.HasFunction("CutQueueManager")
      Case Permissions.BreakoutOperator
        Return Principal.HasFunction("Breakouts")

        'Case Permissions.BreakoutRegen                       ' DRB 3/12/12 Per FB 1432, deprecated function. All breakouts users are allowed this function.
        '  Return Principal.HasFunction("BreakoutRegen")

        ' DRB 6/2/09 Added next 4 cases for MasterEdit, per Kyle
      Case Permissions.EditHarness
        Return Principal.HasFunction("EditHarness")
      Case Permissions.EditWire
        Return Principal.HasFunction("EditWire")
      Case Permissions.EditTerminal
        Return Principal.HasFunction("EditTerminal")
      Case Permissions.EditConnector
        Return Principal.HasFunction("EditConnector")

      Case Permissions.EditImage
        Return Principal.HasFunction("EditImage")

        ' DRB 2/12/10
      Case Permissions.HeatShrinkLabelServer
        Return Principal.HasFunction("HeatShrinkLabelServer")

        ' DRB 11/1/10
      Case Permissions.UseCutAllowApp
        Return Principal.HasFunction("CutPoolManagement")
      Case Permissions.UserMaint
        Return Principal.HasFunction("UserMaint")

        ' DRB 1/24/12 
      Case Permissions.EquipmentBasic
        Return Principal.HasFunction("UseEquipmentAppBasic")
      Case Permissions.EquipmentSecureOps
        Return Principal.HasFunction("UseEquipmentAppSecureOps")

        ' DRB 2/21/12
      Case Permissions.EnableDetachedMode
        Return Principal.HasFunction("EnableDetachedMode")

      Case Permissions.RoleFunctionMaint
        Return Principal.HasFunction("RoleFunctionMaint")

      Case Permissions.CreateRework
        Return Principal.HasFunction("CreateRework")

      Case Permissions.ManageShortages
        Return Principal.HasFunction("ManageShortages")

      Case Permissions.DeleteJobs
        Return Principal.HasFunction("DeleteJobs")

      Case Permissions.BreakoutsViewOnly
        Return Principal.HasFunction("BreakoutsViewOnly")

      Case Permissions.DataChangeRequests
        Return Principal.HasFunction("DataChangeRequests")

      Case Permissions.KeyenceMeasurement
        Return Principal.HasFunction("KeyenceMeasurement")

      Case Else
        Err.Raise(2000, , "UserHasPermission : Function index [" & CStr(iFunction) & "] is invalid.")
    End Select
  End Function

End Module


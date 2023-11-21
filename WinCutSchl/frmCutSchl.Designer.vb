<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmCutSchl
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container
        Me.dgEvents = New System.Windows.Forms.DataGrid
        Me.lblCellResponsePending = New System.Windows.Forms.Label
        Me.TimerScanIn = New System.Windows.Forms.Timer(Me.components)
        Me.btnScan = New System.Windows.Forms.Button
        Me.BackgroundWorker1 = New System.ComponentModel.BackgroundWorker
        Me.lblCellRequestPending = New System.Windows.Forms.Label
        Me.lblCellController = New System.Windows.Forms.Label
        Me.btnRetry = New System.Windows.Forms.Button
        CType(Me.dgEvents, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'dgEvents
        '
        Me.dgEvents.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.dgEvents.DataMember = ""
        Me.dgEvents.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.dgEvents.HeaderForeColor = System.Drawing.SystemColors.ControlText
        Me.dgEvents.Location = New System.Drawing.Point(15, 49)
        Me.dgEvents.Name = "dgEvents"
        Me.dgEvents.Size = New System.Drawing.Size(738, 357)
        Me.dgEvents.TabIndex = 4
        Me.dgEvents.TabStop = False
        '
        'lblCellResponsePending
        '
        Me.lblCellResponsePending.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.lblCellResponsePending.BackColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.lblCellResponsePending.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblCellResponsePending.Location = New System.Drawing.Point(50, 470)
        Me.lblCellResponsePending.Name = "lblCellResponsePending"
        Me.lblCellResponsePending.Size = New System.Drawing.Size(314, 23)
        Me.lblCellResponsePending.TabIndex = 5
        Me.lblCellResponsePending.Text = "Responses To Cut Interceptor Pending"
        Me.lblCellResponsePending.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'TimerScanIn
        '
        '
        'btnScan
        '
        Me.btnScan.Location = New System.Drawing.Point(413, 258)
        Me.btnScan.Name = "btnScan"
        Me.btnScan.Size = New System.Drawing.Size(75, 23)
        Me.btnScan.TabIndex = 7
        Me.btnScan.TabStop = False
        Me.btnScan.Text = "Scan"
        Me.btnScan.UseVisualStyleBackColor = True
        '
        'lblCellRequestPending
        '
        Me.lblCellRequestPending.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.lblCellRequestPending.BackColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.lblCellRequestPending.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblCellRequestPending.Location = New System.Drawing.Point(50, 440)
        Me.lblCellRequestPending.Name = "lblCellRequestPending"
        Me.lblCellRequestPending.Size = New System.Drawing.Size(314, 23)
        Me.lblCellRequestPending.TabIndex = 8
        Me.lblCellRequestPending.Text = "Requests From Cut Interceptor Pending"
        Me.lblCellRequestPending.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'lblCellController
        '
        Me.lblCellController.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.lblCellController.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblCellController.Location = New System.Drawing.Point(50, 420)
        Me.lblCellController.Name = "lblCellController"
        Me.lblCellController.Size = New System.Drawing.Size(314, 19)
        Me.lblCellController.TabIndex = 9
        Me.lblCellController.Text = "Cut Interceptor Messages"
        Me.lblCellController.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'btnRetry
        '
        Me.btnRetry.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnRetry.Location = New System.Drawing.Point(345, 412)
        Me.btnRetry.Name = "btnRetry"
        Me.btnRetry.Size = New System.Drawing.Size(75, 25)
        Me.btnRetry.TabIndex = 18
        Me.btnRetry.TabStop = False
        Me.btnRetry.Text = "Retry"
        Me.btnRetry.UseVisualStyleBackColor = True
        '
        'frmCutSchl
        '
        Me.AcceptButton = Me.btnScan
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(762, 494)
        Me.Controls.Add(Me.btnRetry)
        Me.Controls.Add(Me.lblCellController)
        Me.Controls.Add(Me.lblCellRequestPending)
        Me.Controls.Add(Me.dgEvents)
        Me.Controls.Add(Me.btnScan)
        Me.Controls.Add(Me.lblCellResponsePending)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D
        Me.MaximizeBox = False
        Me.Name = "frmCutSchl"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "WireTrac Schleuniger Controller v1.0a"
        CType(Me.dgEvents, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents dgEvents As System.Windows.Forms.DataGrid
    Friend WithEvents lblCellResponsePending As System.Windows.Forms.Label
    Friend WithEvents TimerScanIn As System.Windows.Forms.Timer
    Friend WithEvents btnScan As System.Windows.Forms.Button
    Friend WithEvents BackgroundWorker1 As System.ComponentModel.BackgroundWorker
    Friend WithEvents lblCellRequestPending As System.Windows.Forms.Label
    Friend WithEvents lblCellController As System.Windows.Forms.Label
    Friend WithEvents btnRetry As System.Windows.Forms.Button

End Class

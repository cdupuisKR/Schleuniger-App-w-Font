Public Class FormattableTextBoxColumn
  Inherits DataGridTextBoxColumn

  Public Event SetCellFormat As FormatCellEventHandler

  Protected Overloads Overrides Sub Paint(ByVal g As System.Drawing.Graphics, _
                                          ByVal bounds As System.Drawing.Rectangle, _
                                          ByVal source As System.Windows.Forms.CurrencyManager, _
                                          ByVal rowNum As Integer, _
                                          ByVal backBrush As System.Drawing.Brush, _
                                          ByVal foreBrush As System.Drawing.Brush, _
                                          ByVal alignToRight As Boolean)

    Dim e As DataGridFormatCellEventArgs = Nothing

    'fire the formatting event
    Dim col As Integer = Me.DataGridTableStyle.GridColumnStyles.IndexOf(Me)
    e = New DataGridFormatCellEventArgs(rowNum, col, Me.GetColumnValueAtRow([source], rowNum))
    RaiseEvent SetCellFormat(Me, e)

    Dim callBaseClass As Boolean = True ' assume we will call the baseclass

    If Not (e.BackBrush Is Nothing) Then
      backBrush = e.BackBrush
    End If
    If Not (e.ForeBrush Is Nothing) Then
      foreBrush = e.ForeBrush
    End If

    'if TextFont set, then must call drawstring
    If Not (e.TextFont Is Nothing) Then
      g.FillRectangle(backBrush, bounds)
      Try
        Dim charWidth As Integer = Fix(Math.Ceiling(g.MeasureString("c", e.TextFont, 20, StringFormat.GenericTypographic).Width))

        Dim s As String = Me.GetColumnValueAtRow([source], rowNum).ToString()
        Dim maxChars As Integer = Math.Min(s.Length, bounds.Width / charWidth)

        Try
          g.DrawString(s.Substring(0, maxChars), e.TextFont, foreBrush, bounds.X, bounds.Y + 2)
        Catch ex As Exception
          Console.WriteLine(ex.Message.ToString())
        End Try
      Catch 'empty catch
      End Try
      callBaseClass = False
    End If

    If Not e.UseBaseClassDrawing Then
      callBaseClass = False
    End If
    If callBaseClass Then
      MyBase.Paint(g, bounds, [source], rowNum, backBrush, foreBrush, alignToRight)
    End If
    'clean up
    If Not (e Is Nothing) Then
      If e.BackBrushDispose Then
        e.BackBrush.Dispose()
      End If
      If e.ForeBrushDispose Then
        e.ForeBrush.Dispose()
      End If
      If e.TextFontDispose Then
        e.TextFont.Dispose()
      End If
    End If
  End Sub 'Paint    

End Class

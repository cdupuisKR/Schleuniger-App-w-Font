Public Delegate Sub FormatCellEventHandler( _
     ByVal sender As Object, _
     ByVal e As DataGridFormatCellEventArgs)

Public Class DataGridFormatCellEventArgs
  Inherits EventArgs

  Public Sub New(ByVal row As Integer, ByVal col As Integer, ByVal cellValue As Object)
    rowNum = row
    colNum = col
    fontVal = Nothing
    backBrushVal = Nothing
    foreBrushVal = Nothing
    fontDisposeVal = False
    backBrushDisposeVal = False
    foreBrushDisposeVal = False
    useBaseClassDrawingVal = True
    currentCellValueVal = cellValue
  End Sub 'New

  ' Holds the column number of the cell being painted.
  Public Property Column() As Integer
    Get
      Return colNum
    End Get
    Set(ByVal Value As Integer)
      colNum = Value
    End Set
  End Property

  ' Holds the row number of the cell being painted.
  Public Property Row() As Integer
    Get
      Return rowNum
    End Get
    Set(ByVal Value As Integer)
      rowNum = Value
    End Set
  End Property

  ' Holds the font to be used to draw text in the cell.
  Public Property TextFont() As Font
    Get
      Return fontVal
    End Get
    Set(ByVal Value As Font)
      fontVal = Value
    End Set
  End Property

  ' Holds the brush used to paint the cell's background.
  Public Property BackBrush() As Brush
    Get
      Return backBrushVal
    End Get
    Set(ByVal Value As Brush)
      backBrushVal = Value
    End Set
  End Property

  ' Holds the brush used to paint the text in the cell.
  Public Property ForeBrush() As Brush
    Get
      Return foreBrushVal
    End Get
    Set(ByVal Value As Brush)
      foreBrushVal = Value
    End Set
  End Property

  ' Set to true if the Dispose method of the TextFont 
  '     should be called by the Paint override.
  Public Property TextFontDispose() As Boolean
    Get
      Return fontDisposeVal
    End Get
    Set(ByVal Value As Boolean)
      fontDisposeVal = Value
    End Set
  End Property

  ' Set to true if the Dispose method of the BackBrush 
  '     should be called by the Paint override.
  Public Property BackBrushDispose() As Boolean
    Get
      Return backBrushDisposeVal
    End Get
    Set(ByVal Value As Boolean)
      backBrushDisposeVal = Value
    End Set
  End Property

  ' Set to true if the Dispose method of the ForeBrush 
  '     should be called by the Paint override.
  Public Property ForeBrushDispose() As Boolean
    Get
      Return foreBrushDisposeVal
    End Get
    Set(ByVal Value As Boolean)
      foreBrushDisposeVal = Value
    End Set
  End Property

  ' Set to false if the MyBase.Paint method  
  '     should not be called in the Paint override.
  Public Property UseBaseClassDrawing() As Boolean
    Get
      Return useBaseClassDrawingVal
    End Get
    Set(ByVal Value As Boolean)
      useBaseClassDrawingVal = Value
    End Set
  End Property

  ' Holds the current cell value. 
  Public ReadOnly Property CurrentCellValue() As Object
    Get
      Return currentCellValueVal
    End Get
  End Property

  ' Private fields to hold the public properties.
  Private colNum As Integer
  Private rowNum As Integer
  Private fontVal As Font
  Private backBrushVal As Brush
  Private foreBrushVal As Brush
  Private fontDisposeVal As Boolean
  Private backBrushDisposeVal As Boolean
  Private foreBrushDisposeVal As Boolean
  Private useBaseClassDrawingVal As Boolean
  Private currentCellValueVal As Object

End Class

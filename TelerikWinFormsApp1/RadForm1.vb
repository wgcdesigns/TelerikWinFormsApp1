Imports Telerik.WinControls.UI
Imports Telerik.Charting
Imports System.Drawing.Drawing2D

Public Class RadForm1
    Protected Overrides Sub OnLoad(e As System.EventArgs)
        MyBase.OnLoad(e)
        AddHandler RadChartView1.CreateRenderer, AddressOf RadChartView1_CreateRenderer
        Dim series As New LineSeries()


        Dim rnd As New Random()
        For i = 0 To 30

            series.DataPoints.Add(New CategoricalDataPoint(rnd.Next(0, 40), DateTime.Now.AddDays(i)))

        Next

        series.DataPoints.Add(New CategoricalDataPoint(rnd.Next(0, 40), DateTime.Now.AddDays(i)))
        'series.DataSource = isqlDS

        Me.RadChartView1.Series.Add(series)
        series.VerticalAxis.LabelFormat = "{0}°"
        series.HorizontalAxis.LabelFormat = "{0:M}"
        series.HorizontalAxis.LabelFitMode = AxisLabelFitMode.MultiLine
        Me.RadChartView1.ShowGrid = True
    End Sub

    Private Sub RadChartView1_CreateRenderer(sender As System.Object, e As Telerik.WinControls.UI.ChartViewCreateRendererEventArgs)
        e.Renderer = New CustomCartesianRenderer(DirectCast(e.Area, CartesianArea))
    End Sub
End Class

Public Class CustomCartesianRenderer
    Inherits CartesianRenderer
    Public Sub New(area As CartesianArea)
        MyBase.New(area)
    End Sub
    Protected Overrides Sub Initialize()
        MyBase.Initialize()
        For i As Integer = 0 To Me.DrawParts.Count - 1
            Dim linePart As LineSeriesDrawPart = TryCast(Me.DrawParts(i), LineSeriesDrawPart)
            If linePart IsNot Nothing Then
                Me.DrawParts(i) = New CustomLineSeriesDrawPart(DirectCast(linePart.Element, LineSeries), Me)
            End If
        Next
    End Sub
End Class

Public Class CustomLineSeriesDrawPart
    Inherits LineSeriesDrawPart
    Public Sub New(series As LineSeriesBase, renderer As IChartRenderer)
        MyBase.New(series, renderer)
    End Sub
    Private predefinedValues As Integer() = New Integer() {5, 16, 25, 35}
    Private predefinedColors As Color() = New Color() {Color.Green, Color.Orange, Color.Red, Color.DarkRed}
    Protected Overrides Sub DrawLine()
        Dim series As LineSeries = TryCast(Me.Element, LineSeries)
        If series.DataPoints.Count < 2 Then
            Return
        End If
        Dim rect As RectangleF = Me.Element.Bounds
        rect.Offset(Me.OffsetX, Me.OffsetY)
        If rect.IsEmpty Then
            Return
        End If
        Dim path As GraphicsPath = GetLinePath()
        Dim linearBrush As New LinearGradientBrush(rect, Color.Transparent, Color.Transparent, 0.0F)
        Dim colorPositionBlend As ColorPositionBlend = GetColorPositionBlend(series.DataPoints)
        Dim blend As New System.Drawing.Drawing2D.ColorBlend()
        blend.Positions = colorPositionBlend.Positions
        blend.Colors = colorPositionBlend.Colors
        linearBrush.InterpolationColors = blend
        Dim graphics As Graphics = TryCast(Me.Renderer.Surface, Graphics)
        graphics.SmoothingMode = SmoothingMode.AntiAlias
        graphics.DrawPath(New Pen(linearBrush, 3), path)
    End Sub
    Private Function GetColorPositionBlend(dataPoints As ChartDataPointCollection) As ColorPositionBlend
        Dim blend As New ColorPositionBlend()
        Dim majorStep As Decimal = 1D / (dataPoints.Count - 1)
        For i As Integer = 0 To dataPoints.Count - 1
            Dim position As Single = CSng(i * majorStep)
            Dim color As Color = GetValueColor(CDbl(DirectCast(dataPoints(i), Telerik.Charting.CategoricalDataPoint).Value))
            blend.Add(color, position)
            If i < dataPoints.Count - 1 Then
                Dim currentValue As Double = CDbl(DirectCast(dataPoints(i), Telerik.Charting.CategoricalDataPoint).Value)
                Dim nextValue As Double = CDbl(DirectCast(dataPoints(i + 1), Telerik.Charting.CategoricalDataPoint).Value)
                Dim additionalBlends As ColorPositionBlend = GetAdditionalColorPositionBlend(currentValue, nextValue, majorStep, i)
                blend.Add(additionalBlends)
            End If
        Next
        Return blend
    End Function
    Private Function GetAdditionalColorPositionBlend(currentValue As Double, nextValue As Double, majorStep As Decimal, iteration As Integer) As ColorPositionBlend
        Dim blend As New ColorPositionBlend()
        If currentValue < nextValue Then
            For j As Integer = 0 To predefinedValues.Length - 1
                Dim colorValue As Single = predefinedValues(j)
                If currentValue < colorValue AndAlso colorValue < nextValue Then
                    Dim additionalPosition As Single = CSng(iteration * majorStep) + CSng((colorValue - currentValue) / (nextValue - currentValue)) * CSng(majorStep)
                    Dim additionalColor As Color = GetValueColor(colorValue)
                    blend.Add(additionalColor, additionalPosition)
                End If
            Next
        End If
        If currentValue > nextValue Then
            For j As Integer = predefinedValues.Length - 1 To 0 Step -1
                Dim colorValue As Single = predefinedValues(j)
                If currentValue > colorValue AndAlso colorValue > nextValue Then
                    Dim additionalPosition As Single = CSng(iteration * majorStep) + CSng((currentValue - colorValue) / (currentValue - nextValue)) * CSng(majorStep)
                    Dim additionalColor As Color = GetValueColor(colorValue)
                    blend.Add(additionalColor, additionalPosition)
                End If
            Next
        End If
        Return blend
    End Function

    Private Function GetValueColor(value As Double) As Color
        If value <= predefinedValues(0) Then
            Return predefinedColors(0)
        End If
        If value >= predefinedValues(predefinedValues.Length - 1) Then
            Return predefinedColors(predefinedValues.Length - 1)
        End If
        For i As Integer = 0 To predefinedValues.Length - 2
            If predefinedValues(i) <= value AndAlso value <= predefinedValues(i + 1) Then
                Dim c As Double = (predefinedValues(i + 1) - value) / (predefinedValues(i + 1) - predefinedValues(i))
                Return Color.FromArgb(CInt(predefinedColors(i).R * c + predefinedColors(i + 1).R * (1 - c)), CInt(predefinedColors(i).G * c + predefinedColors(i + 1).G * (1 - c)), CInt(predefinedColors(i).B * c + predefinedColors(i + 1).B * (1 - c)))
            End If
        Next
        Return Color.Transparent
    End Function
End Class

Public Class ColorPositionBlend
    Private m_positions As List(Of Single)
    Private m_colors As List(Of Color)
    Public Sub New()
        m_positions = New List(Of Single)()
        m_colors = New List(Of Color)()
    End Sub
    Public Sub Add(color As Color, position As Single)
        Me.m_colors.Add(color)
        Me.m_positions.Add(position)
    End Sub
    Public Sub Add(colorPositionBlend As ColorPositionBlend)
        Me.m_positions.AddRange(colorPositionBlend.Positions)
        Me.m_colors.AddRange(colorPositionBlend.Colors)
    End Sub
    Public ReadOnly Property Positions() As Single()
        Get
            Return Me.m_positions.ToArray()
        End Get
    End Property
    Public ReadOnly Property Colors() As Color()
        Get
            Return Me.m_colors.ToArray()
        End Get
    End Property
End Class

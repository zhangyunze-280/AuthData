Option Strict On : Option Explicit On

Public NotInheritable Class Sequence
    Private Shared _cur As UShort = 0US
    Private Sub New() : End Sub

    Public Shared Function [Next]() As UShort
        _cur = CUShort(If(_cur = UShort.MaxValue, 1, _cur + 1))
        If _cur = 0US Then _cur = 1US
        Return _cur
    End Function
End Class


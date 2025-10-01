Option Strict On : Option Explicit On

Public Enum SeqClass
    Class1 = 1 ' 判定要求/乗車券照会
    Class2 = 2 ' ヘルスチェック/認証データ/タンキング
End Enum

Public NotInheritable Class Sequence
    Private Shared _c1 As UShort = 0US
    Private Shared _c2 As UShort = 0US
    Private Sub New() : End Sub

    Public Shared Function [Next](cls As SeqClass) As UShort
        Select Case cls
            Case SeqClass.Class1
                _c1 = CUShort(If(_c1 = UShort.MaxValue, 1, _c1 + 1))
                If _c1 = 0US Then _c1 = 1US
                Return _c1
            Case Else
                _c2 = CUShort(If(_c2 = UShort.MaxValue, 1, _c2 + 1))
                If _c2 = 0US Then _c2 = 1US
                Return _c2
        End Select
    End Function
End Class

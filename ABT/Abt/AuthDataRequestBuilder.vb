' ================================
' AuthDataRequestBuilder.vb
' 認証用データ要求の AppData(12B) を生成
' 形式: C3 00 08 00 | BCD(yyyyMMdd 4B) | Sum(Int32 LE)
' ================================
Option Strict On
Option Explicit On

Public NotInheritable Class AuthDataRequestBuilder

    ' yyyyMMdd 8桁 → BCD 4B（yy:2B, MM:1B, dd:1B）
    Public Shared Function ToBcd8Date(yyyyMMdd As String) As Byte()
        If yyyyMMdd Is Nothing OrElse yyyyMMdd.Length <> 8 Then
            Throw New ArgumentException("yyyyMMdd は8桁で指定してください。例: 20250701")
        End If

        Dim y As Integer = Integer.Parse(yyyyMMdd.Substring(0, 4))
        Dim m As Integer = Integer.Parse(yyyyMMdd.Substring(4, 2))
        Dim d As Integer = Integer.Parse(yyyyMMdd.Substring(6, 2))

        Dim b(3) As Byte
        ' 年(2B)
        b(0) = CByte((y \ 1000) * 16 + ((y \ 100) Mod 10))
        b(1) = CByte(((y \ 10) Mod 10) * 16 + (y Mod 10))
        ' 月(1B)
        b(2) = CByte((m \ 10) * 16 + (m Mod 10))
        ' 日(1B)
        b(3) = CByte((d \ 10) * 16 + (d Mod 10))
        Return b
    End Function

    ' 指定バイト列の1B総加算（Int32で返却）
    Public Shared Function SumBytes(bytes As Byte()) As Integer
        If bytes Is Nothing Then Return 0
        Dim sum As Integer = 0
        For Each v In bytes
            sum += v
        Next
        Return sum
    End Function

    ' AppData本体(12B)を生成
    ' 戻り値:
    '   [0]   = 0xC3 (Cmd)
    '   [1]   = 0x00 (Sub)
    '   [2-3] = 0x0008 (Length, LE)
    '   [4-7] = BCD(yyyyMMdd)
    '   [8-11]= Sum(Int32, LE)  ※BCD4Bの総加算
    Public Shared Function Build(yyyyMMdd As String) As Byte()
        Dim bcd As Byte() = ToBcd8Date(yyyyMMdd)
        Dim sum As Integer = SumBytes(bcd)
        Dim sumLe As Byte() = BitConverter.GetBytes(sum) ' LE

        Dim app(11) As Byte
        app(0) = &HC3
        app(1) = &H0
        app(2) = &H8 ' Length = 8 (BCD4 + Sum4)  ※LE
        app(3) = &H0

        Buffer.BlockCopy(bcd, 0, app, 4, 4)
        Buffer.BlockCopy(sumLe, 0, app, 8, 4)
        Return app
    End Function

End Class

Option Strict On : Option Explicit On
Imports System.Text

Public NotInheritable Class CommandDispatcher
    Private Sub New() : End Sub

    ' 例: "Call,AbtAuthenticationData,<引数1(20B hex)>,<引数2(yyyyMMdd)>"
    ' ※引数1はPipe仕様に合わせてバイナリ→Base64やHexのどちらかで来る想定。ここではHexを例に。
    Public Shared Sub Dispatch(line As String)
        If String.IsNullOrWhiteSpace(line) Then Return
        Dim parts = line.Split(","c)
        If parts.Length < 2 Then Return

        Dim head = parts(0).Trim()
        Dim cmd = parts(1).Trim()

        If Not head.Equals("Call", StringComparison.OrdinalIgnoreCase) Then
            Logger.LogWarn($"Unknown head: {head}")
            Return
        End If

        Select Case cmd
            Case "AbtAuthenticationData"
                HandleAuthData(parts)
            Case Else
                Logger.LogWarn($"Unknown command: {cmd}")
        End Select
    End Sub

    Private Shared Sub HandleAuthData(parts As String())
        ' 期待: Call,AbtAuthenticationData, 引数1, 引数2
        If parts.Length < 4 Then
            Logger.LogError("AuthData: 引数不足")
            Return
        End If

        ' 引数1: 駅務機器情報(20B) 受け渡し形式に合わせてデコード
        ' ここでは "xx-xx-... (Hex)" もしくは連続Hex "001122..." を許容
        Dim equip20 As Byte() = ParseHex(parts(2).Trim())
        If equip20 Is Nothing OrElse equip20.Length < 10 Then
            Logger.LogError("AuthData: 引数1(駅務機器情報)不正")
            Return
        End If

        ' 引数2: yyyyMMdd
        Dim ymd As String = parts(3).Trim()
        If ymd.Length <> 8 Then
            Logger.LogError("AuthData: 引数2(yyyyMMdd)不正")
            Return
        End If

        ' 宛先設定
        Dim ip = Config.DestIp
        Dim port = Config.Port

        ' Seqは分類2
        Dim seq = Sequence.Next(SeqClass.Class2)
        Dim retry As UShort = 0US

        Try
            Logger.LogInfo($"AuthData Send start: ip={ip}, port={port}, seq={seq}")
            Dim frame = AuthDataLogic.SendOnce(ip, port, equip20, ymd, seq, retry)
            Logger.LogInfo($"AuthData Send done: bytes={frame.Length}, seq={seq}")
        Catch ex As Exception
            Logger.LogError("AuthData Send error: " & ex.Message)
            Logger.NotifyOperator("認証データ要求の送信失敗")
        End Try
    End Sub

    Private Shared Function ParseHex(s As String) As Byte()
        If String.IsNullOrWhiteSpace(s) Then Return Nothing
        s = s.Replace("-", "").Replace(" ", "")
        If (s.Length Mod 2) <> 0 Then Return Nothing
        Dim n = s.Length \ 2
        Dim b(n - 1) As Byte
        For i = 0 To n - 1
            b(i) = Convert.ToByte(s.Substring(i * 2, 2), 16)
        Next
        Return b
    End Function
End Class


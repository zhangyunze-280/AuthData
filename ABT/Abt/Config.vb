Option Strict On : Option Explicit On
Imports System.IO

Public NotInheritable Class Config
    Private Sub New() : End Sub

    Public Shared Property Mode As Integer = 1 ' 0:本番 1:試験
    Public Shared Property ProdIp As String = "127.0.0.1"
    Public Shared Property TestIp As String = "127.0.0.1"
    Public Shared Property Port As Integer = 50000
    Public Shared Property TimeoutMs As Integer = 500
    Public Shared Property RetryMax As Integer = 1

    Public Shared Sub Load(iniPath As String)
        If Not File.Exists(iniPath) Then Exit Sub
        For Each raw In File.ReadAllLines(iniPath)
            Dim line = raw.Trim()
            If line = "" OrElse line.StartsWith("#") OrElse line.StartsWith(";") Then Continue For
            Dim kv = line.Split({"="c}, 2)
            If kv.Length <> 2 Then Continue For
            Dim k = kv(0).Trim() : Dim v = kv(1).Trim()
            Select Case k
                Case "接続環境モード" : Integer.TryParse(v, Mode) ' 0:本番 1:試験
                Case "本番用サーバIPアドレス1" : If v <> "" Then ProdIp = v
                Case "試験用サーバIPアドレス1" : If v <> "" Then TestIp = v
                Case "判定要求通知（タンキング）送信インターバル" ' 未使用例
                Case "応答電文受信待ち待機時間" : Integer.TryParse(v, TimeoutMs)
                Case "リトライ回数" : Integer.TryParse(v, RetryMax)
                Case "UDPポート" : Integer.TryParse(v, Port)
            End Select
        Next
    End Sub

    Public Shared ReadOnly Property DestIp As String
        Get
            Return If(Mode = 0, ProdIp, TestIp)
        End Get
    End Property
End Class


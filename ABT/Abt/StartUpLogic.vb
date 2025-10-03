' ================================
' StartUpLogic.vb  起動処理の同期待ち/完了通知のハブ
' ================================
Option Strict On : Option Explicit On
Imports System.Threading

Public NotInheritable Class StartUpLogic
    Private Sub New() : End Sub

    ' 起動処理は同時1件想定。必要ならSeqごとに辞書管理へ拡張可
    Private Shared ReadOnly _wait As New ManualResetEventSlim(False)
    Private Shared _result As Boolean = False

    ' iniから読み込む応答待ち時間(ms)
    Private Shared ReadOnly Property TimeoutMs As Integer
        Get
            Return Config.TimeoutMs
        End Get
    End Property

    ' 2. AbtOpenReceived() ー Pipe受信側から呼ばれる（ここで同期的に待つ）
    Public Shared Function AbtOpenReceived() As Boolean
        Logger.LogInfo("StartUpLogic: AbtOpenReceived() begin")
        _result = False
        _wait.Reset()

        ' 3. sendInitialProcessingReq() → 4. UDPでサーバへ非同期送信
        sendInitialProcessingReq()

        ' 6. onInitialProcessingRes() が来るまで待機（タイムアウトで失敗扱い）
        Dim ok As Boolean = _wait.Wait(TimeoutMs)
        Dim ret As Boolean = ok AndAlso _result
        Logger.LogInfo($"StartUpLogic: AbtOpenReceived() end -> {If(ret, "OK", "NG")}")
        Return ret
    End Function

    ' 3/5. 起動要求（初期化要求）をサーバへ投げる
    Public Shared Sub sendInitialProcessingReq()
        Logger.LogDebug("StartUpLogic: sendInitialProcessingReq() -> UdpSender")
        UdpSender.sendInitialProcessingToServer()
    End Sub

    ' 6/7. UDP受信側からの完了コールバック
    Public Shared Sub onInitialProcessingRes(success As Boolean)
        Logger.LogInfo($"StartUpLogic: onInitialProcessingRes(success={success})")
        _result = success
        _wait.Set()
    End Sub
End Class

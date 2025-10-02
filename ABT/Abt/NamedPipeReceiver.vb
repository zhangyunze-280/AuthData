Imports System.IO.Pipes
Imports System.Text
Imports System.Threading

Public Class NamedPipeReceiver
    Private _cts As CancellationTokenSource    '_cts:受信ループを停止するためのキャンセルトークン
    Private _pipeServer As NamedPipeServerStream    'NamedPipeServerのインスタンス
    Private _judgeLogic As New JudgeRequestLogic()    '受信データを処理する外部ロジック
    Private _tankingManager As New TankingDataManager
    Private _startUp As New StartUpLogic

    '===== 受信開始 =====
    Public Sub StartReceiver()
        _cts = New CancellationTokenSource()
        _pipeServer = New NamedPipeServerStream("MyPipe", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous)    'NamePipeServerを作成

        WriteLog("制御アプリからの接続を待機中...")
        _pipeServer.WaitForConnection()    '制御アプリと接続待機
        WriteLog("制御アプリと接続されました")

        Dim t As New Thread(AddressOf ReceiveLoop)    'バックグランドスレッドでReceiveLoopを開始
        t.IsBackground = True
        t.Start()
    End Sub

    '===== 受信ループ =====
    Private Sub ReceiveLoop()
        Dim buffer(4095) As Byte    '送られてきたデータを格納する

        Try
            '受信ループ無限条件
            While Not _cts.Token.IsCancellationRequested
                Dim bytesRead = _pipeServer.Read(buffer, 0, buffer.Length)   'namedPipeServerStreamからデータを読み込む（パイプかrア実際に読み込まれたバイト数が整数である）
                If bytesRead > 0 Then
                    Dim msg As String = Encoding.GetEncoding("shift_jis").GetString(buffer, 0, bytesRead).Trim()　'buffer0からbutesread文だけ文字列に変換する　.Trimは余白削除

                    ' "Call," で始まるかチェック
                    If msg.StartsWith("Call,") Then
                        Dim parts() As String = msg.Split(","c)   'Callをparts(0)に格納
                        If parts.Length >= 2 Then
                            Dim methodName As String = parts(1).Trim()   'メソッド名を取得
                            Dim args() As String = parts.Skip(2).ToArray()    '３番目以降の要素を引数配列argsに格納

                            ' メソッドごとに Select Case で判定
                            Dim calledMethod As String = "Unknown"

                            Select Case methodName
                                Case "AbtTicketGateJudgment"
                                    calledMethod = "AbtTicketGateJudgment"
                                    HandleAbtTicketGateJudgment(args)

                                Case "AbtTicketGateJudgmentTanking"
                                    calledMethod = "AbtTicketGateTanking"
                                    HandleAbtTicketGateTanking(args)

                                Case "AbtOpen"
                                    calledMethod = "AbtOpen"
                                    HandleAbtOpen(args)

                                Case "AbtAuthenticationData"
                                    calledMethod = "AbtAuthenticationData"
                                    HandleAbtAuthenticationData(args)

                                Case Else
                                    WriteLog("不明なメソッド: " & methodName)
                            End Select

                            ' 呼ばれたメソッドをログに出力
                            WriteLog("呼ばれたメソッド: " & calledMethod)
                        Else
                            WriteLog("Callメッセージが不正: " & msg)
                        End If
                    Else
                        WriteLog("Callではないメッセージを受信: " & msg)
                    End If
                End If
            End While

        Catch ex As Exception
            WriteLog("受信ループエラー: " & ex.Message)
        Finally
            StopReceiver()
        End Try
    End Sub

    '===== 各ハンドラ =====
    Private Sub HandleAbtTicketGateJudgment(args() As String)
        WriteLog($"TicketGateJudgment 引数数: {args.Length}")
        WriteLog("内容: " & String.Join("|", args))


        If args.Length < 14 Then
            WriteLog("TicketGateJudgment 引数不足")
            Return
        End If

        Dim procDir = args(0)
        Dim qrCode = args(1)
        Dim qrNum = args(2)
        Dim reqTime = args(3)
        Dim issueDisFlag = args(4)
        Dim appBailFlag = args(5)
        Dim offlineTktGateFlag = args(6)
        Dim execPermitFlag = args(7)
        Dim modelType = args(8)
        Dim otherStaAppFlag = args(9)
        Dim bizOpRegCode = args(10)
        Dim bizOpUserCode = args(11)
        Dim lineSec = args(12)
        Dim staOrder = args(13)

        _judgeLogic.DecisionRequest(procDir, qrCode, qrNum, reqTime, issueDisFlag,
                                   appBailFlag, offlineTktGateFlag, execPermitFlag,
                                   modelType, otherStaAppFlag, bizOpRegCode,
                                   bizOpUserCode, lineSec, staOrder)

        WriteLog($"TicketGateJudgment受信: {String.Join(", ", args)}")
    End Sub

    Private Sub HandleAbtTicketGateTanking(args() As String)

        Dim extra = args(0)

        _tankingManager.StartTankingProcess(extra)

        WriteLog($"TicketGateJudgmentTanking受信: {String.Join(", ", args)}")
    End Sub

    Private Sub HandleAbtOpen(args() As String)

        Dim extra = args(0)

        _startUp.AbtOpenReceived(extra)

        WriteLog("AbtOpen 受信: " & String.Join(",", args))
    End Sub

    Private Sub HandleAbtAuthenticationData(args() As String)
        ' 仮の例
        WriteLog("AbtAuthenticationData 受信: " & String.Join(",", args))
    End Sub

    '===== ログ =====
    Private Sub WriteLog(message As String)
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}")
    End Sub

    '===== 停止処理 =====
    Private Sub StopReceiver()
        Try
            If _pipeServer IsNot Nothing Then
                _pipeServer.Dispose()
                _pipeServer = Nothing
            End If
            If _cts IsNot Nothing Then
                _cts.Cancel()
                _cts.Dispose()
                _cts = Nothing
            End If
            WriteLog("NamedPipeReceiverを停止しました")
        Catch
        End Try
    End Sub

End Class

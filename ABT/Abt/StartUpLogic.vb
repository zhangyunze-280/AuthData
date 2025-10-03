Option Strict On : Option Explicit On

' 起動処理：AbtOpenReceived(extra) を受けて同期で応答文字列を返すだけ
' ・extra は仕様上 "0" 固定
' ・UDP 送信なし（起動処理ではサーバ連携しない）
' ・Logger は使わず Console.WriteLine のみ
Public NotInheritable Class StartUpLogic
    Public Sub New()
    End Sub

    Public Function AbtOpenReceived(extra As String) As String
        Try
            Dim x As String = If(extra, String.Empty).Trim()

            ' 引数チェック（"0" 固定）
            If x <> "0" Then
#If DEBUG Then
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] AbtOpenReceived: invalid extra='{x}' (expected '0')")
#End If
                ' 失敗応答
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Result,AbtOpen,1,0")
                Return "Result,AbtOpen,1,0"
            End If

            ' 必要なら設定ファイル読込（任意）
            ' 例: 同期初期化として INI を読む。失敗しても起動応答は返す。
            ' Try
            'Config.Load("ABTControl.ini")
            ' #If DEBUG Then
            'Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Config loaded (Mode={Config.Mode}, Port={Config.Port})")
            '#End If
            '       Catch ex As Exception
            '#If DEBUG Then
            '           Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Config load skipped/error: {ex.Message}")
            '#End If
            '       End Try

            ' ここまで来たら正常
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] StartUpLogic受信完了: extra={x}")
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Result,AbtOpen,0,0")
            Return "Result,AbtOpen,0,0"

        Catch ex As Exception
            ' エラー時のログと応答
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] AbtOpenReceived exception: {ex.Message}")
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Result,AbtOpen,1,0")
            Return "Result,AbtOpen,1,0"
        End Try
    End Function
End Class

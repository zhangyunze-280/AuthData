' ================================
' Config.vb  設定ファイル管理
' 役割:
'  - ABTControl.ini の読み込みと設定値の提供
' ================================
Option Strict On : Option Explicit On

Public NotInheritable Class Config
    ' インスタンス化防止
    Private Sub New()
    End Sub

    ' ===== 設定値 =====
    ' [機器動作]
    Public Shared Property DebugMode As Integer = 0

    ' [通信設定]
    Public Shared Property ProductionServerIP1 As String    ' 本番用サーバIPアドレス1
    Public Shared Property ProductionServerIP2 As String    ' 本番用サーバIPアドレス2
    Public Shared Property TestServerIP1 As String          ' 試験用サーバIPアドレス1
    Public Shared Property TestServerIP2 As String          ' 試験用サーバIPアドレス2
    Public Shared Property HealthCheckWaitTime As Integer = 1000        ' ヘルスチェック待ち時間
    Public Shared Property HealthCheckInterval As Integer = 60000       ' ヘルスチェック送信インターバル
    Public Shared Property ResponseWaitTime As Integer = 1000          ' 応答電文受信待ち待機時間
    Public Shared Property Mode As Integer = 0                         ' 接続環境モード
    Public Shared Property RetryCount As Integer = 3                   ' リトライ回数
    Public Shared Property TankingNotifyInterval As Integer = 10000    ' 判定要求通知（タンキング）遅信インターバル

    ' [ログ]
    Public Shared Property LogLevel As Integer = 4
    Public Shared Property LogSize As Integer = 10000000
    Public Shared Property LogCount As Integer = 1
    Public Shared Property LogPath As String = "C:\log"
    Public Shared Property LogName As String = "AbtControlLog"

    ' ===== 設定ファイル読み込み =====
    Public Shared Sub Load(filePath As String)
        Try
#If DEBUG Then
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Loading config: {filePath}")
#End If
            ' ファイルの存在チェック
            If Not System.IO.File.Exists(filePath) Then
                Throw New System.IO.FileNotFoundException($"設定ファイルが見つかりません: {filePath}")
            End If

            ' INIファイル読み込み
            Using reader As New System.IO.StreamReader(filePath)
                Dim currentSection As String = ""
                While Not reader.EndOfStream
                    Dim line = reader.ReadLine()?.Trim()
                    If String.IsNullOrEmpty(line) OrElse line.StartsWith(";") Then Continue While

                    ' セクション行
                    If line.StartsWith("[") AndAlso line.EndsWith("]") Then
                        currentSection = line.Substring(1, line.Length - 2)
                        Continue While
                    End If

                    ' 値の行
                    Dim parts = line.Split("="c)
                    If parts.Length <> 2 Then Continue While

                    Dim key = parts(0).Trim()
                    Dim value = parts(1).Trim()

                    ' セクションごとの処理
                    Select Case currentSection
                        Case "機器動作"
                            HandleOperationSection(key, value)
                        Case "通信設定"
                            HandleCommunicationSection(key, value)
                        Case "ログ"
                            HandleLogSection(key, value)
                    End Select
                End While
            End Using

#If DEBUG Then
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Config loaded successfully")
#End If

        Catch ex As Exception
#If DEBUG Then
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Config load error: {ex.Message}")
#End If
            Throw ' 再スロー
        End Try
    End Sub

    ' セクションごとの処理
    Private Shared Sub HandleOperationSection(key As String, value As String)
        Select Case key
            Case "デバッグモード"
                If Integer.TryParse(value, DebugMode) Then
#If DEBUG Then
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Debug mode: {DebugMode}")
#End If
                End If
        End Select
    End Sub

    Private Shared Sub HandleCommunicationSection(key As String, value As String)
        Select Case key
            Case "本番用サーバIPアドレス1"
                ProductionServerIP1 = value
            Case "本番用サーバIPアドレス2"
                ProductionServerIP2 = value
            Case "試験用サーバIPアドレス1"
                TestServerIP1 = value
            Case "試験用サーバIPアドレス2"
                TestServerIP2 = value
            Case "ヘルスチェック待ち時間"
                Integer.TryParse(value, HealthCheckWaitTime)
            Case "ヘルスチェック送信インターバル"
                Integer.TryParse(value, HealthCheckInterval)
            Case "応答電文受信待ち待機時間"
                Integer.TryParse(value, ResponseWaitTime)
            Case "接続環境モード"
                Integer.TryParse(value, Mode)
            Case "リトライ回数"
                Integer.TryParse(value, RetryCount)
            Case "判定要求通知（タンキング）遅信インターバル"
                Integer.TryParse(value, TankingNotifyInterval)
        End Select
    End Sub

    Private Shared Sub HandleLogSection(key As String, value As String)
        Select Case key
            Case "LogLevel"
                Integer.TryParse(value, LogLevel)
            Case "LogSize"
                Integer.TryParse(value, LogSize)
            Case "LogCount"
                Integer.TryParse(value, LogCount)
            Case "LogPath"
                LogPath = value
            Case "LogName"
                LogName = value
        End Select
    End Sub
End Class
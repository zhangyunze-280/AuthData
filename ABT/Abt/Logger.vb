' ================================
' Logger.vb  最小ロガー（スレッドセーフ）
' 提供API:
'   Logger.Initialize(level, logDir, logName, maxBytes, count)
'   Logger.InitializeFromIni(path)   ' あれば読む、なければ既定
'   Logger.LogInfo(msg)
'   Logger.LogWarn(msg)
'   Logger.LogError(msg)
'   Logger.NotifyOperator(msg)       ' 係員通知：今はログ＋Console
' ================================
Option Strict On
Option Explicit On
Imports System.IO
Imports System.Text
Imports System.Threading

Public NotInheritable Class Logger
    Public Enum LogLevel
        None = 0
        Fatal = 1
        [Error] = 2
        Warn = 3
        Info = 4
        Debug = 5
    End Enum

    Private Shared _level As LogLevel = LogLevel.Info
    Private Shared _logDir As String = "C:\log"
    Private Shared _logName As String = "AbtControlLog"
    Private Shared _maxBytes As Long = 10000000 ' 10MB/世代
    Private Shared _count As Integer = 1        ' 世代数（1以上）
    Private Shared ReadOnly _lock As New Object()

    Private Sub New()
    End Sub

    ' ===== 初期化 =====
    Public Shared Sub Initialize(Optional level As LogLevel = LogLevel.Info,
                               Optional logDir As String = "C:\log",
                               Optional logName As String = "AbtControlLog",
                               Optional maxBytes As Long = 10000000,
                               Optional count As Integer = 1)
        _level = level
        _logDir = logDir
        _logName = logName
        _maxBytes = Math.Max(1024, maxBytes)
        _count = Math.Max(1, count)
        EnsureDir()
    End Sub

    ' INI（キー=値 の簡易形式）を読む。なければ既定。
    Public Shared Sub InitializeFromIni(iniPath As String)
        ' 例のキー:
        ' LogLevel=4
        ' LogPath=C:\log
        ' LogName=AbtControlLog
        ' LogSize=10000000
        ' LogCount=3
        If Not File.Exists(iniPath) Then
            Initialize()
            Return
        End If
        Dim lvl As Integer = 4
        Dim dir As String = "C:\log"
        Dim name As String = "AbtControlLog"
        Dim size As Long = 10000000
        Dim cnt As Integer = 1

        For Each line In File.ReadAllLines(iniPath, Encoding.UTF8)
            Dim s = line.Trim()
            If s.StartsWith("#") OrElse s.StartsWith(";") OrElse s = "" Then Continue For
            Dim kv = s.Split({"="c}, 2)
            If kv.Length <> 2 Then Continue For
            Dim k = kv(0).Trim()
            Dim v = kv(1).Trim()
            Select Case k
                Case "LogLevel" : Integer.TryParse(v, lvl)
                Case "LogPath" : If v <> "" Then dir = v
                Case "LogName" : If v <> "" Then name = v
                Case "LogSize" : Long.TryParse(v.Replace(",", ""), size)
                Case "LogCount" : Integer.TryParse(v, cnt)
            End Select
        Next
        Initialize(CType(Math.Max(0, Math.Min(5, lvl)), LogLevel), dir, name, size, cnt)
    End Sub

    ' ===== API =====
    Public Shared Sub LogDebug(msg As String)
        Write(LogLevel.Debug, "DEBUG", msg)
    End Sub

    Public Shared Sub LogInfo(msg As String)
        Write(LogLevel.Info, "INFO ", msg)
    End Sub

    Public Shared Sub LogWarn(msg As String)
        Write(LogLevel.Warn, "WARN ", msg)
    End Sub

    Public Shared Sub LogError(msg As String)
        Write(LogLevel.Error, "ERROR", msg)
    End Sub

    ' 係員通知：今はログ＋コンソール。後で実装差し替え可
    Public Shared Sub NotifyOperator(msg As String)
        LogWarn("NOTIFY: " & msg)
        Console.WriteLine("[NOTIFY] " & msg)
    End Sub

    ' ===== 内部 =====
    Private Shared Sub EnsureDir()
        Try
            If Not Directory.Exists(_logDir) Then Directory.CreateDirectory(_logDir)
        Catch
            ' 権限がないなど。C:\temp にフォールバック
            _logDir = "C:\temp"
            If Not Directory.Exists(_logDir) Then Directory.CreateDirectory(_logDir)
        End Try
    End Sub

    Private Shared Function CurrentLogPath() As String
        Dim suffix = If(_count <= 1, "", "_001")
        Return Path.Combine(_logDir, $"{_logName}{suffix}.log")
    End Function

    Private Shared Sub Write(level As LogLevel, tag As String, msg As String)
        If level > _level OrElse _level = LogLevel.None Then Return
        Dim line = $"{DateTime.Now:yyyyMMdd-HHmmss-fff} [{Thread.CurrentThread.ManagedThreadId}] {tag}  {msg}"
        SyncLock _lock
            RotateIfNeeded()
            File.AppendAllText(CurrentLogPath(), line & Environment.NewLine, Encoding.UTF8)
        End SyncLock
    End Sub

    ' 簡易ローテーション（_count 分を循環）
    Private Shared Sub RotateIfNeeded()
        Dim currentPath = CurrentLogPath()
        Try
            If Not File.Exists(currentPath) Then Return
            Dim len = New FileInfo(currentPath).Length
            If len < _maxBytes Then Return

            ' 末尾から繰上げ
            If _count > 1 Then
                For i = _count To 2 Step -1
                    Dim src = Path.Combine(_logDir, $"{_logName}_{(i - 1).ToString("000")}.log")
                    Dim dst = Path.Combine(_logDir, $"{_logName}_{i.ToString("000")}.log")
                    If File.Exists(src) Then
                        If File.Exists(dst) Then File.Delete(dst)
                        File.Move(src, dst)
                    End If
                Next
                ' 現在ログ → _001 に移動
                Dim first = Path.Combine(_logDir, $"{_logName}_001.log")
                If File.Exists(first) Then File.Delete(first)
                File.Move(currentPath, first)
            Else
                ' 1世代運用なら単純に削除
                File.Delete(currentPath)
            End If
        Catch
            ' 失敗しても書き込み継続
        End Try
    End Sub
End Class
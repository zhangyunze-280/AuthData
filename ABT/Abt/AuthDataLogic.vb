' ================================
' AuthDataLogic.vb
' 役割:
'  - Pipe(NamedPipeReceiver) から渡される引数を検証
'  - 認証データ要求フレーム(ヘッダ32B + AppData12B + CRC32 4B)を生成
'  - UdpSender でサーバへ送信（非同期応答は UdpReceiver 側で別途処理）
'  - 制御アプリへ返すレスポンス文字列を返却:
'       "Result,AbtAuthenticationData,<処理結果(0/1/2)>,<シーケンス番号>"
'
' 依存: UdpSender（共通チームが実装中）
' 使い方（例 / NamedPipeReceiver 側）:
'   Dim logic As New AuthDataLogic()
'   Dim resp As String = logic.AbtAuthenticationData(
'                           equip20Hex, yyyymmdd)
'   ' resp を NamedPipeReceiver→制御アプリへそのまま返す
' ================================

' ================================
' AuthDataLogic.vb  認証データ要求 入口
' ================================
Option Strict On : Option Explicit On
Imports System.Text.RegularExpressions

Public NotInheritable Class AuthDataLogic
    ' 分類2のシーケンス
    Private Shared _seq2 As UShort = 0US
    Private Shared Function NextSeq() As UShort
        SyncLock GetType(AuthDataLogic)
            _seq2 = CUShort(If(_seq2 = UShort.MaxValue, 1, _seq2 + 1))
            If _seq2 = 0US Then _seq2 = 1US
            Return _seq2
        End SyncLock
    End Function

    ' ★ここが入口：制御アプリ→NamedPipeReceiver→本メソッド
    '   返り値は「Result,AbtAuthenticationData,<処理結果>,<シーケンス>」
    '   処理結果: 0=正常 / 1=内部異常 / 2=タイムアウト等（今は未使用で0/1のみ）
    Public Function AbtAuthenticationData(equip20Hex As String,
                                        yyyyMMdd As String) As String
        Dim result As Integer = 1
        Dim seq As UShort = NextSeq()

        Try
            ' 設定ファイルから送信先情報を取得
            Try
                Config.Load("ABTControl.ini")

                ' 接続環境モードに応じたサーバーIPを取得
                Dim destIp As String = If(Config.Mode = 0,
                                        Config.ProductionServerIP1,
                                        Config.TestServerIP1)

                ' 送信先IPアドレスのチェック
                If String.IsNullOrWhiteSpace(destIp) Then
#If DEBUG Then
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Server IP not configured")
#End If
                    Return $"Result,AbtAuthenticationData,{result},{seq}"
                End If

                ' 引数チェック
                Dim equip20 = ParseHexToBytes(equip20Hex)
                If equip20 Is Nothing OrElse equip20.Length <> 20 Then
                    Return $"Result,AbtAuthenticationData,{result},{seq}"
                End If
                If Not IsValidYyyyMmDd(yyyyMMdd) Then
                    Return $"Result,AbtAuthenticationData,{result},{seq}"
                End If

                ' フレーム生成と送信
                Dim frame = BuildAuthFrame(seq, equip20, yyyyMMdd)
                UdpSender.Send(destIp, Config.ResponseWaitTime, frame)

                result = 0
                Return $"Result,AbtAuthenticationData,{result},{seq}"

            Catch ex As Exception
#If DEBUG Then
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Config load error: {ex.Message}")
#End If
                Return $"Result,AbtAuthenticationData,{result},{seq}"
            End Try

        Catch ex As Exception
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] AbtAuthenticationData exception: {ex.Message}")
            Return $"Result,AbtAuthenticationData,1,{seq}"
        End Try
    End Function

    ' ===== ヘルパ =====
    Private Shared Function ParseHexToBytes(hex As String) As Byte()
        If String.IsNullOrWhiteSpace(hex) Then Return Nothing
        Dim compact = Regex.Replace(hex, "[\s,\-_,:]", "")
        If compact.Length Mod 2 <> 0 Then Return Nothing
        Dim n = compact.Length \ 2
        Dim b(n - 1) As Byte
        For i = 0 To n - 1
            b(i) = Convert.ToByte(compact.Substring(i * 2, 2), 16)
        Next
        Return b
    End Function

    Private Shared Function IsValidYyyyMmDd(s As String) As Boolean
        If s Is Nothing OrElse s.Length <> 8 OrElse Not s.All(AddressOf Char.IsDigit) Then Return False
        Dim y = Integer.Parse(s.Substring(0, 4))
        Dim m = Integer.Parse(s.Substring(4, 2))
        Dim d = Integer.Parse(s.Substring(6, 2))
        If y < 2000 OrElse y > 2100 Then Return False
        If m < 1 OrElse m > 12 Then Return False
        If d < 1 OrElse d > 31 Then Return False
        Return True
    End Function

    ' フレーム作成（最小実装）
    Private Shared Function BuildAuthFrame(seq As UShort,
                                           equip20 As Byte(),
                                           yyyyMMdd As String) As Byte()

        ' ヘッダ 32B
        Dim header(31) As Byte
        WriteU16(header, 0, seq)                  ' seq
        WriteU16(header, 2, 0US)                  ' retry
        WriteU16(header, 4, 1US)                  ' blockNo
        WriteU16(header, 6, 1US)                  ' blockTotal
        WriteU32(header, 8, &H1UI)                ' version
        WriteU16(header, 12, 0US)                 ' reserved
        Buffer.BlockCopy(equip20, 0, header, 14, 10)  ' 駅務機器 先頭10B
        WriteU32(header, 24, 1UI)                 ' appCount
        WriteU32(header, 28, 12UI)                ' appSize

        ' AppData 12B  (C3 00 08 00 | BCD(yyyyMMdd) | Sum(Int32 LE))
        Dim app(11) As Byte
        app(0) = &HC3 : app(1) = &H0
        app(2) = &H8 : app(3) = &H0
        Dim bcd = ToBcd8Date(yyyyMMdd)
        Buffer.BlockCopy(bcd, 0, app, 4, 4)
        Dim sum = SumBytes(bcd)
        Buffer.BlockCopy(BitConverter.GetBytes(sum), 0, app, 8, 4)

        ' CRC32(IEEE)
        Dim body(header.Length + app.Length - 1) As Byte
        Buffer.BlockCopy(header, 0, body, 0, header.Length)
        Buffer.BlockCopy(app, 0, body, header.Length, app.Length)
        Dim crc = Crc32IEEE(body)
        Dim frame(header.Length + app.Length + 4 - 1) As Byte
        Buffer.BlockCopy(body, 0, frame, 0, body.Length)
        Buffer.BlockCopy(BitConverter.GetBytes(crc), 0, frame, body.Length, 4)
        Return frame
    End Function

    Private Shared Function ToBcd8Date(yyyyMMdd As String) As Byte()
        Dim y = Integer.Parse(yyyyMMdd.Substring(0, 4))
        Dim m = Integer.Parse(yyyyMMdd.Substring(4, 2))
        Dim d = Integer.Parse(yyyyMMdd.Substring(6, 2))
        Return {
            CByte((y \ 1000) * 16 + (y \ 100 Mod 10)),
            CByte((y \ 10 Mod 10) * 16 + (y Mod 10)),
            CByte((m \ 10) * 16 + (m Mod 10)),
            CByte((d \ 10) * 16 + (d Mod 10))
        }
    End Function

    Private Shared Function SumBytes(bytes As Byte()) As Integer
        Dim s As Integer = 0
        For Each b In bytes : s += b : Next
        Return s
    End Function

    Private Shared Sub WriteU16(buf As Byte(), off As Integer, v As UShort)
        Buffer.BlockCopy(BitConverter.GetBytes(v), 0, buf, off, 2)
    End Sub
    Private Shared Sub WriteU32(buf As Byte(), off As Integer, v As UInteger)
        Buffer.BlockCopy(BitConverter.GetBytes(v), 0, buf, off, 4)
    End Sub

    ' 最小CRC32(IEEE/PKZip)
    Private Shared Function Crc32IEEE(data As Byte()) As UInteger
        Dim poly As UInteger = &HEDB88320UI
        Dim tbl(255) As UInteger
        For i = 0 To 255
            Dim c As UInteger = CUInt(i)
            For j = 0 To 7
                If (c And 1UI) <> 0UI Then c = (c >> 1) Xor poly Else c >>= 1
            Next
            tbl(i) = c
        Next
        Dim crc As UInteger = &HFFFFFFFFUI
        For Each b In data
            Dim idx = CInt((crc Xor b) And &HFFUI)
            crc = (crc >> 8) Xor tbl(idx)
        Next
        Return Not crc
    End Function
End Class
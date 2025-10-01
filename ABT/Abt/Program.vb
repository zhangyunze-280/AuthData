' ================================
' Program.vb  ― 単体送信の最小実行
' ================================
Option Strict On
Option Explicit On
Imports System

Module Program
    Sub Main()
        ' 1) ロガー初期化（INIがあれば読み、なければ既定）
        Logger.InitializeFromIni("dtABTControl.ini")

        ' 2) 送信先（仮）— あとでINIに移す
        Dim ip As String = "127.0.0.1"   ' 実サーバに合わせて変更
        Dim port As Integer = 50000

        ' 3) 駅務機器情報 20B（先頭10Bを使用）— ダミー
        Dim equip20 As Byte() = {
            &H1, &H0,  ' IdentifyCode = 0x0001 (LE)
            &H12,        ' OperatorArea
            &H34,        ' OperatorUser
            &H16,        ' DeviceModel（自動改札機）
            &H1,        ' Line
            &H5,        ' StationSeq
            &H1,        ' Corner
            &H10,        ' Machine
            &H0,        ' Reserved
            &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0
        }

        ' 4) 認証データ送信日時（yyyyMMdd）
        Dim ymd As String = "20250701"

        Try
            ' 5) 送信（分類2のシーケンスを使う）
            Dim seq As UShort = Sequence.Next(SeqClass.Class2)
            Dim frame = AuthDataLogic.SendOnce(ip, port, equip20, ymd, seq, 0US)

            Console.WriteLine("Send OK")
            Console.WriteLine("TotalBytes = " & frame.Length)      ' 期待: 48
            Console.WriteLine("Frame HEX = " & BitConverter.ToString(frame))
            Logger.LogInfo($"ManualSend OK: bytes={frame.Length}, seq={seq}")

        Catch ex As Exception
            Console.WriteLine("ERROR: " & ex.Message)
            Logger.LogError("ManualSend ERROR: " & ex.Message)
        End Try

        Console.WriteLine("Enterで終了…")
        Console.ReadLine() ' コンソールが即閉じるのを防ぐ
    End Sub
End Module

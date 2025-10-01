' ================================
' FrameBuilder.vb
' 基本電文(ヘッダ32B) + AppData(12B) + CRC32(4B)
' ================================
Option Strict On
Option Explicit On
Imports System

Public NotInheritable Class FrameBuilder
    Private Sub New() : End Sub

    Private Shared _seq As UShort = 0US
    Public Shared Function NextSeq() As UShort
        _seq = CUShort(If(_seq = UShort.MaxValue, 1, _seq + 1))
        Return _seq
    End Function

    ' equip20: 駅務機器情報20B（制御アプリから受領。未接続時はダミーOK）
    Public Shared Function BuildAuthDataFrame(equip20 As Byte(), yyyyMMdd As String, Optional seqOpt As UShort? = Nothing) As Byte()
        If equip20 Is Nothing OrElse equip20.Length <> 20 Then
            Throw New ArgumentException("equip20 は20バイト必須です。")
        End If

        ' --- ヘッダ32B ---
        Dim header(31) As Byte

        ' 1) 管理情報
        Dim seq As UShort = If(seqOpt.HasValue, seqOpt.Value, NextSeq())      ' シーケンス番号
        Dim retry As UShort = 0US                                             ' リトライカウンタ
        Dim mainBlock As UShort = 1US                                         ' 本ブロック番号
        Dim totalBlock As UShort = 1US                                        ' 総ブロック番号
        Dim ver As UInteger = &H1UI                                           ' 0x00000001
        Dim reserved As UShort = 0US                                          ' 0x0000

        ' LEで詰める
        Buffer.BlockCopy(BitConverter.GetBytes(seq), 0, header, 0, 2)
        Buffer.BlockCopy(BitConverter.GetBytes(retry), 0, header, 2, 2)
        Buffer.BlockCopy(BitConverter.GetBytes(mainBlock), 0, header, 4, 2)
        Buffer.BlockCopy(BitConverter.GetBytes(totalBlock), 0, header, 6, 2)
        Buffer.BlockCopy(BitConverter.GetBytes(ver), 0, header, 8, 4)
        Buffer.BlockCopy(BitConverter.GetBytes(reserved), 0, header, 12, 2)

        ' 2) 駅務機器情報(20B)
        Buffer.BlockCopy(equip20, 0, header, 14, 20) ' 14～33だがヘッダは32Bなので14～31で20Bぴったり

        ' --- AppData 12B（認証用データ要求）---
        Dim app As Byte() = AuthDataRequestBuilder.Build(yyyyMMdd) ' C3 00 08 00 | BCD4 | SUM4

        ' --- ヘッダ後半のアプリ個数/サイズ ---
        ' ヘッダ仕様上は「アプリデータ数(4B)」「アプリデータサイズ(4B)」がヘッダ末尾に続く前提
        ' 上でヘッダにequip20を詰め切っているため、ここで続けて配置する
        Dim appCount As UInteger = 1UI
        Dim appSize As UInteger = CUInt(app.Length)
        Dim meta(7) As Byte ' 8B
        Buffer.BlockCopy(BitConverter.GetBytes(appCount), 0, meta, 0, 4)
        Buffer.BlockCopy(BitConverter.GetBytes(appSize), 0, meta, 4, 4)

        ' 実際は「ヘッダ32B + AppCount(4) + AppSize(4) + AppData(12)」の並びで送る想定
        Dim withoutCrc As Byte() = New Byte(header.Length + meta.Length + app.Length - 1) {}
        Buffer.BlockCopy(header, 0, withoutCrc, 0, header.Length)
        Buffer.BlockCopy(meta, 0, withoutCrc, header.Length, meta.Length)
        Buffer.BlockCopy(app, 0, withoutCrc, header.Length + meta.Length, app.Length)

        ' --- CRC32(LE) ---
        Dim crcLe As Byte() = Crc32.ComputeLe(withoutCrc)
        Dim frame As Byte() = New Byte(withoutCrc.Length + 4 - 1) {}
        Buffer.BlockCopy(withoutCrc, 0, frame, 0, withoutCrc.Length)
        Buffer.BlockCopy(crcLe, 0, frame, withoutCrc.Length, 4)
        Return frame
    End Function
End Class

' ---------------- CRC32 ----------------
Public NotInheritable Class Crc32
    Private Sub New() : End Sub
    Private Shared ReadOnly Table As UInteger() = Init()

    Private Shared Function Init() As UInteger()
        Const poly As UInteger = &HEDB88320UI ' 0x04C11DB7 reflected
        Dim t(255) As UInteger
        For i = 0 To 255
            Dim c As UInteger = CUInt(i)
            For j = 0 To 7
                If (c And 1UI) <> 0UI Then
                    c = poly Xor (c >> 1)
                Else
                    c >>= 1
                End If
            Next
            t(i) = c
        Next
        Return t
    End Function

    Public Shared Function ComputeLe(data As Byte()) As Byte()
        Dim c As UInteger = &HFFFFFFFFUI
        For Each b In data
            c = Table((c Xor b) And &HFFUI) Xor (c >> 8)
        Next
        c = c Xor &HFFFFFFFFUI
        Return BitConverter.GetBytes(c) ' LE
    End Function
End Class

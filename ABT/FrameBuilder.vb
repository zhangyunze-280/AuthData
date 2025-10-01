Option Strict On
Option Explicit On

' 駅務機器情報（ヘッダ内10B）
Public Structure StationInfo
    Public IdentifyCode As UShort     ' 2B: 0x0001=取扱機能, 0x0002=取扱機能以外
    Public OperatorArea As Byte       ' 1B: サイバネ
    Public OperatorUser As Byte       ' 1B: サイバネ
    Public DeviceModel As Byte        ' 1B: 0x16=自動改札機 など
    Public LineCode As Byte           ' 1B: サイバネ
    Public StationSeq As Byte         ' 1B: サイバネ
    Public CornerNo As Byte           ' 1B: 取り決め値
    Public MachineNo As Byte          ' 1B: 取り決め値
    Public Reserved As Byte           ' 1B: 0x00
End Structure

Public NotInheritable Class FrameBuilder
    Private Sub New()
    End Sub

    Private Const HeaderSize As Integer = 32

    ''' <summary>
    ''' 基本電文（ヘッダ32B + AppData + CRC32 4B）を生成
    ''' </summary>
    Public Shared Function Build(seq As UShort,
                                 retry As UShort,
                                 blockNo As UShort,         ' 通常 1
                                 blockTotal As UShort,      ' 通常 1
                                 st As StationInfo,         ' 上の構造体
                                 appData As Byte()) As Byte()

        If appData Is Nothing OrElse appData.Length = 0 Then
            Throw New ArgumentException("appData が空です。")
        End If

        Dim header(HeaderSize - 1) As Byte

        ' 0..1: Seq
        WriteUInt16(header, 0, seq)
        ' 2..3: Retry
        WriteUInt16(header, 2, retry)
        ' 4..5: 本ブロック番号
        WriteUInt16(header, 4, blockNo)
        ' 6..7: 総ブロック数
        WriteUInt16(header, 6, blockTotal)
        ' 8..11: Version = 0x00000001
        WriteUInt32(header, 8, &H1UI)
        ' 12..13: 予備 = 0x0000
        WriteUInt16(header, 12, CUShort(0))

        ' 14..23: 駅務機器情報（10B）
        WriteUInt16(header, 14, st.IdentifyCode)
        header(16) = st.OperatorArea
        header(17) = st.OperatorUser
        header(18) = st.DeviceModel
        header(19) = st.LineCode
        header(20) = st.StationSeq
        header(21) = st.CornerNo
        header(22) = st.MachineNo
        header(23) = st.Reserved     ' 通常 0x00

        ' 24..27: Appデータ数 = 1
        WriteUInt32(header, 24, 1UI)
        ' 28..31: Appデータサイズ
        WriteUInt32(header, 28, CUInt(appData.Length))

        ' ヘッダ + AppData
        Dim withoutCrc As Byte() = New Byte(header.Length + appData.Length - 1) {}
        Buffer.BlockCopy(header, 0, withoutCrc, 0, header.Length)
        Buffer.BlockCopy(appData, 0, withoutCrc, header.Length, appData.Length)

        ' CRC32（LE）
        Dim crc As UInteger = Crc32.Compute(withoutCrc)
        Dim crcLe = BitConverter.GetBytes(crc)

        Dim frame As Byte() = New Byte(withoutCrc.Length + 4 - 1) {}
        Buffer.BlockCopy(withoutCrc, 0, frame, 0, withoutCrc.Length)
        Buffer.BlockCopy(crcLe, 0, frame, withoutCrc.Length, 4)

        Return frame
    End Function

    ' --- ユーティリティ ---
    Private Shared Sub WriteUInt16(buf As Byte(), offset As Integer, value As UShort)
        Dim b = BitConverter.GetBytes(value) ' LE
        Buffer.BlockCopy(b, 0, buf, offset, 2)
    End Sub
    Private Shared Sub WriteUInt32(buf As Byte(), offset As Integer, value As UInteger)
        Dim b = BitConverter.GetBytes(value) ' LE
        Buffer.BlockCopy(b, 0, buf, offset, 4)
    End Sub
End Class

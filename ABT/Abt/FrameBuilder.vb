' ================================
' FrameBuilder.vb（確定版）
' 基本電文: ヘッダ32B + AppData(可変) + CRC32(4B)
' ・ヘッダの内訳は仕様表に準拠
' ・CRCはヘッダ先頭～CRC直前までを対象（IEEE, LE格納）
' ================================
Option Strict On
Option Explicit On

' ヘッダ内 10B の「駅務機器情報」構造
Public Structure StationInfo
    Public IdentifyCode As UShort   ' 2B
    Public OperatorArea As Byte     ' 1B
    Public OperatorUser As Byte     ' 1B
    Public DeviceModel As Byte      ' 1B
    Public LineCode As Byte         ' 1B
    Public StationSeq As Byte       ' 1B
    Public CornerNo As Byte         ' 1B
    Public MachineNo As Byte        ' 1B
    Public Reserved As Byte         ' 1B (0x00)
End Structure

Public NotInheritable Class FrameBuilder
    Private Sub New() : End Sub
    Private Const HeaderSize As Integer = 32

    ''' <summary>
    ''' 基本電文を生成（ヘッダ32B + AppData + CRC32(4B)）
    ''' </summary>
    Public Shared Function Build(seq As UShort,
                                 retry As UShort,
                                 blockNo As UShort,        ' 1固定(通常)
                                 blockTotal As UShort,     ' 1固定(通常)
                                 st As StationInfo,
                                 appData As Byte()) As Byte()

        If appData Is Nothing OrElse appData.Length = 0 Then
            Throw New ArgumentException("appData が空です。")
        End If

        Dim header(HeaderSize - 1) As Byte

        ' 0..1: シーケンス番号
        WriteUInt16(header, 0, seq)
        ' 2..3: リトライカウンタ
        WriteUInt16(header, 2, retry)
        ' 4..5: 本ブロック番号
        WriteUInt16(header, 4, blockNo)
        ' 6..7: 総ブロック数
        WriteUInt16(header, 6, blockTotal)
        ' 8..11: バージョン 0x00000001
        WriteUInt32(header, 8, &H1UI)
        ' 12..13: 予備 0x0000
        WriteUInt16(header, 12, 0US)

        ' 14..23: 駅務機器情報(10B)
        WriteUInt16(header, 14, st.IdentifyCode)
        header(16) = st.OperatorArea
        header(17) = st.OperatorUser
        header(18) = st.DeviceModel
        header(19) = st.LineCode
        header(20) = st.StationSeq
        header(21) = st.CornerNo
        header(22) = st.MachineNo
        header(23) = st.Reserved

        ' 24..27: アプリデータ数 = 1
        WriteUInt32(header, 24, 1UI)
        ' 28..31: アプリデータサイズ = appData.Length
        WriteUInt32(header, 28, CUInt(appData.Length))

        ' ヘッダ + AppData
        Dim withoutCrc As Byte() = New Byte(header.Length + appData.Length - 1) {}
        Buffer.BlockCopy(header, 0, withoutCrc, 0, header.Length)
        Buffer.BlockCopy(appData, 0, withoutCrc, header.Length, appData.Length)

        ' CRC32 を計算（IEEE）し、LEで末尾に付与
        Dim crc As UInteger = Crc32.Compute(withoutCrc)
        Dim crcLe As Byte() = BitConverter.GetBytes(crc)

        Dim frame As Byte() = New Byte(withoutCrc.Length + 4 - 1) {}
        Buffer.BlockCopy(withoutCrc, 0, frame, 0, withoutCrc.Length)
        Buffer.BlockCopy(crcLe, 0, frame, withoutCrc.Length, 4)
        Return frame
    End Function

    ' --- Util ---
    Private Shared Sub WriteUInt16(buf As Byte(), offset As Integer, value As UShort)
        Dim b = BitConverter.GetBytes(value) ' LE
        Buffer.BlockCopy(b, 0, buf, offset, 2)
    End Sub
    Private Shared Sub WriteUInt32(buf As Byte(), offset As Integer, value As UInteger)
        Dim b = BitConverter.GetBytes(value) ' LE
        Buffer.BlockCopy(b, 0, buf, offset, 4)
    End Sub
End Class

Option Strict On
Option Explicit On

' �w���@����i�w�b�_��10B�j
Public Structure StationInfo
    Public IdentifyCode As UShort     ' 2B: 0x0001=�戵�@�\, 0x0002=�戵�@�\�ȊO
    Public OperatorArea As Byte       ' 1B: �T�C�o�l
    Public OperatorUser As Byte       ' 1B: �T�C�o�l
    Public DeviceModel As Byte        ' 1B: 0x16=�������D�@ �Ȃ�
    Public LineCode As Byte           ' 1B: �T�C�o�l
    Public StationSeq As Byte         ' 1B: �T�C�o�l
    Public CornerNo As Byte           ' 1B: ��茈�ߒl
    Public MachineNo As Byte          ' 1B: ��茈�ߒl
    Public Reserved As Byte           ' 1B: 0x00
End Structure

Public NotInheritable Class FrameBuilder
    Private Sub New()
    End Sub

    Private Const HeaderSize As Integer = 32

    ''' <summary>
    ''' ��{�d���i�w�b�_32B + AppData + CRC32 4B�j�𐶐�
    ''' </summary>
    Public Shared Function Build(seq As UShort,
                                 retry As UShort,
                                 blockNo As UShort,         ' �ʏ� 1
                                 blockTotal As UShort,      ' �ʏ� 1
                                 st As StationInfo,         ' ��̍\����
                                 appData As Byte()) As Byte()

        If appData Is Nothing OrElse appData.Length = 0 Then
            Throw New ArgumentException("appData ����ł��B")
        End If

        Dim header(HeaderSize - 1) As Byte

        ' 0..1: Seq
        WriteUInt16(header, 0, seq)
        ' 2..3: Retry
        WriteUInt16(header, 2, retry)
        ' 4..5: �{�u���b�N�ԍ�
        WriteUInt16(header, 4, blockNo)
        ' 6..7: ���u���b�N��
        WriteUInt16(header, 6, blockTotal)
        ' 8..11: Version = 0x00000001
        WriteUInt32(header, 8, &H1UI)
        ' 12..13: �\�� = 0x0000
        WriteUInt16(header, 12, CUShort(0))

        ' 14..23: �w���@����i10B�j
        WriteUInt16(header, 14, st.IdentifyCode)
        header(16) = st.OperatorArea
        header(17) = st.OperatorUser
        header(18) = st.DeviceModel
        header(19) = st.LineCode
        header(20) = st.StationSeq
        header(21) = st.CornerNo
        header(22) = st.MachineNo
        header(23) = st.Reserved     ' �ʏ� 0x00

        ' 24..27: App�f�[�^�� = 1
        WriteUInt32(header, 24, 1UI)
        ' 28..31: App�f�[�^�T�C�Y
        WriteUInt32(header, 28, CUInt(appData.Length))

        ' �w�b�_ + AppData
        Dim withoutCrc As Byte() = New Byte(header.Length + appData.Length - 1) {}
        Buffer.BlockCopy(header, 0, withoutCrc, 0, header.Length)
        Buffer.BlockCopy(appData, 0, withoutCrc, header.Length, appData.Length)

        ' CRC32�iLE�j
        Dim crc As UInteger = Crc32.Compute(withoutCrc)
        Dim crcLe = BitConverter.GetBytes(crc)

        Dim frame As Byte() = New Byte(withoutCrc.Length + 4 - 1) {}
        Buffer.BlockCopy(withoutCrc, 0, frame, 0, withoutCrc.Length)
        Buffer.BlockCopy(crcLe, 0, frame, withoutCrc.Length, 4)

        Return frame
    End Function

    ' --- ���[�e�B���e�B ---
    Private Shared Sub WriteUInt16(buf As Byte(), offset As Integer, value As UShort)
        Dim b = BitConverter.GetBytes(value) ' LE
        Buffer.BlockCopy(b, 0, buf, offset, 2)
    End Sub
    Private Shared Sub WriteUInt32(buf As Byte(), offset As Integer, value As UInteger)
        Dim b = BitConverter.GetBytes(value) ' LE
        Buffer.BlockCopy(b, 0, buf, offset, 4)
    End Sub
End Class

' ================================
' FrameBuilder.vb�i�m��Łj
' ��{�d��: �w�b�_32B + AppData(��) + CRC32(4B)
' �E�w�b�_�̓���͎d�l�\�ɏ���
' �ECRC�̓w�b�_�擪�`CRC���O�܂ł�ΏہiIEEE, LE�i�[�j
' ================================
Option Strict On
Option Explicit On

' �w�b�_�� 10B �́u�w���@����v�\��
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
    ''' ��{�d���𐶐��i�w�b�_32B + AppData + CRC32(4B)�j
    ''' </summary>
    Public Shared Function Build(seq As UShort,
                                 retry As UShort,
                                 blockNo As UShort,        ' 1�Œ�(�ʏ�)
                                 blockTotal As UShort,     ' 1�Œ�(�ʏ�)
                                 st As StationInfo,
                                 appData As Byte()) As Byte()

        If appData Is Nothing OrElse appData.Length = 0 Then
            Throw New ArgumentException("appData ����ł��B")
        End If

        Dim header(HeaderSize - 1) As Byte

        ' 0..1: �V�[�P���X�ԍ�
        WriteUInt16(header, 0, seq)
        ' 2..3: ���g���C�J�E���^
        WriteUInt16(header, 2, retry)
        ' 4..5: �{�u���b�N�ԍ�
        WriteUInt16(header, 4, blockNo)
        ' 6..7: ���u���b�N��
        WriteUInt16(header, 6, blockTotal)
        ' 8..11: �o�[�W���� 0x00000001
        WriteUInt32(header, 8, &H1UI)
        ' 12..13: �\�� 0x0000
        WriteUInt16(header, 12, 0US)

        ' 14..23: �w���@����(10B)
        WriteUInt16(header, 14, st.IdentifyCode)
        header(16) = st.OperatorArea
        header(17) = st.OperatorUser
        header(18) = st.DeviceModel
        header(19) = st.LineCode
        header(20) = st.StationSeq
        header(21) = st.CornerNo
        header(22) = st.MachineNo
        header(23) = st.Reserved

        ' 24..27: �A�v���f�[�^�� = 1
        WriteUInt32(header, 24, 1UI)
        ' 28..31: �A�v���f�[�^�T�C�Y = appData.Length
        WriteUInt32(header, 28, CUInt(appData.Length))

        ' �w�b�_ + AppData
        Dim withoutCrc As Byte() = New Byte(header.Length + appData.Length - 1) {}
        Buffer.BlockCopy(header, 0, withoutCrc, 0, header.Length)
        Buffer.BlockCopy(appData, 0, withoutCrc, header.Length, appData.Length)

        ' CRC32 ���v�Z�iIEEE�j���ALE�Ŗ����ɕt�^
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

Module Test3
    Sub Main()
        Try
            ' 1. AppData�̐����i�O���AuthDataRequestBuilder���g�p�j
            Console.WriteLine("1. AppData�̐���")
            Dim app = AuthDataRequestBuilder.Build("20250701")
            Console.WriteLine($"AppData: {BitConverter.ToString(app)}")
            Console.WriteLine()

            ' 2. �w���@����̐ݒ�
            Console.WriteLine("2. �w���@����̐ݒ�")
            Dim st As New StationInfo With {
                .IdentifyCode = &H1US,    ' 0x0001: �戵�@�\
                .OperatorArea = &H12,     ' �G���A
                .OperatorUser = &H34,     ' ���[�U�[
                .DeviceModel = &H16,      ' �������D�@
                .LineCode = &H1,         ' �H��
                .StationSeq = &H5,       ' �w��
                .CornerNo = &H1,         ' �R�[�i�[
                .MachineNo = &H10,        ' �@��ԍ�
                .Reserved = &H0           ' �\��
            }
            Console.WriteLine($"StationInfo�ݒ芮��")
            Console.WriteLine()

            ' 3. �t���[������
            Console.WriteLine("3. �t���[������")
            Dim seq As UShort = 1  ' �e�X�g�p�ɌŒ�l
            Dim frame = FrameBuilder.Build(
                seq,               ' �V�[�P���X�ԍ�
                retry:=0,         ' ���g���C��
                blockNo:=1,       ' �u���b�N�ԍ�
                blockTotal:=1,    ' ���u���b�N��
                st,               ' �w���@����
                app               ' �A�v���f�[�^
            )

            ' 4. ���ʕ\��
            Console.WriteLine("�������ꂽ�t���[��:")
            Console.WriteLine($"�S�̒�: {frame.Length}�o�C�g")
            Console.WriteLine($"�f�[�^: {BitConverter.ToString(frame)}")

            ' 5. �t���[���̓��e�𕪉����ĕ\��
            Console.WriteLine()
            Console.WriteLine("�t���[���̓��e:")
            Console.WriteLine($"�w�b�_(32B): {BitConverter.ToString(frame, 0, 32)}")
            Console.WriteLine($"AppData(12B): {BitConverter.ToString(frame, 32, 12)}")
            Console.WriteLine($"CRC32(4B): {BitConverter.ToString(frame, 44, 4)}")

        Catch ex As Exception
            Console.WriteLine($"�G���[: {ex.Message}")
        End Try

        Console.WriteLine()
        Console.WriteLine("Enter�L�[�������ďI��...")
        Console.ReadLine()
    End Sub
End Module
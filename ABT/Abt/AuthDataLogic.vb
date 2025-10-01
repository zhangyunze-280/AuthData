Option Strict On : Option Explicit On

Public NotInheritable Class AuthDataLogic
    Private Const TIMEOUT_MS As Integer = 500
    Private Const RETRY_MAX As Integer = 1   ' 1回だけ再送

    ' Pipe引数1(20B)から StationInfo を組み立てるヘルパ
    Public Shared Function MapStationInfoFrom20(equip20 As Byte()) As StationInfo
        If equip20 Is Nothing OrElse equip20.Length < 10 Then
            Throw New ArgumentException("equip20 は10B以上が必要")
        End If
        Dim si As New StationInfo
        ' LE: 低位→高位
        si.IdentifyCode = CUShort(CUInt(equip20(0)) Or (CUInt(equip20(1)) << 8))
        si.OperatorArea = equip20(2)
        si.OperatorUser = equip20(3)
        si.DeviceModel = equip20(4)
        si.LineCode = equip20(5)
        si.StationSeq = equip20(6)
        si.CornerNo = equip20(7)
        si.MachineNo = equip20(8)
        si.Reserved = equip20(9)
        Return si
    End Function

    ' 単発送信
    Public Shared Function SendOnce(destIp As String, port As Integer,
                                    equip20 As Byte(),
                                    yyyymmdd As String,
                                    seq As UShort,
                                    retry As UShort) As Byte()

        Dim st = MapStationInfoFrom20(equip20)
        Dim app = AuthDataRequestBuilder.Build(yyyymmdd)
        Dim frame = FrameBuilder.Build(seq, retry, 1US, 1US, st, app)

        ' ローカルCRC検証
        Dim bodyLen = frame.Length - 4
        Dim body(bodyLen - 1) As Byte
        Buffer.BlockCopy(frame, 0, body, 0, bodyLen)
        Dim crcCalc = Crc32.Compute(body)
        Dim crcOnFrame = BitConverter.ToUInt32(frame, bodyLen)
        If crcCalc <> crcOnFrame Then
            Throw New InvalidOperationException("CRC mismatch（生成不整合）")
        End If

        UdpSender.Send(destIp, port, frame)
        Return frame
    End Function

    ' 500ms待ち＋1回リトライ（受信は未実装なのでタイムアウトは疑似）
    Public Shared Function SendWithRetry(destIp As String, port As Integer,
                                         equip20 As Byte(),
                                         yyyymmdd As String) As UShort
        Dim seq = Sequence.Next(SeqClass.Class2)
        Dim retry As UShort = 0US

        Logger.LogInfo($"AuthReq Send seq={seq}, retry={retry}")
        Dim frame = SendOnce(destIp, port, equip20, yyyymmdd, seq, retry)

        ' 受信系が未完成なので、ここはタイムアウト待ちのみ
        System.Threading.Thread.Sleep(TIMEOUT_MS)

        ' 実際は Ack/Res 有無を確認。未着ならリトライ
        Dim needRetry As Boolean = True  ' 今は常に未着扱い
        If needRetry AndAlso RETRY_MAX >= 1 Then
            retry = CUShort(retry + 1)
            Logger.LogWarn($"AuthReq Timeout → Retry1 seq={seq}, retry={retry}")
            frame = SendOnce(destIp, port, equip20, yyyymmdd, seq, retry)
        End If

        Return seq
    End Function
End Class

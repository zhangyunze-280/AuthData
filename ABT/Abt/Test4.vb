Module Test4
    Sub Main()
        Try
            Console.WriteLine("認証データフレーム生成テスト")
            Console.WriteLine("----------------------------")

            ' 1. AppDataの生成と確認
            Console.WriteLine("1. AppDataの生成")
            Dim app = AuthDataRequestBuilder.Build("20250701")
            Console.WriteLine($"AppData(12B): {BitConverter.ToString(app)}")
            Console.WriteLine($"期待値: C3-00-08-00-20-25-07-01-4D-00-00-00")
            Console.WriteLine()

            ' 2. ダミー駅務機器情報の準備
            Console.WriteLine("2. 駅務機器情報の準備")
            Dim equip20 As Byte() = {
                &H1, &H0,  ' IdentifyCode = 0x0001 (LE)
                &H12,        ' Area
                &H34,        ' User
                &H16,        ' DeviceModel (自動改札機)
                &H1,        ' Line
                &H5,        ' StationSeq
                &H1,        ' Corner
                &H10,        ' Machine
                &H0,        ' Reserved
                &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0
            }
            Console.WriteLine($"Equip20: {BitConverter.ToString(equip20)}")
            Console.WriteLine()

            ' 3. フレーム生成
            Console.WriteLine("3. フレーム生成")
            Dim st = AuthDataLogic.MapStationInfoFrom20(equip20)
            Dim frame = FrameBuilder.Build(
                seq:=1US,
                retry:=0US,
                blockNo:=1US,
                blockTotal:=1US,
                st:=st,
                appData:=app
            )

            ' フレームの内容を表示
            Console.WriteLine("生成されたフレーム:")
            Console.WriteLine($"ヘッダ(32B): {BitConverter.ToString(frame, 0, 32)}")
            Console.WriteLine($"AppData(12B): {BitConverter.ToString(frame, 32, 12)}")
            Console.WriteLine($"CRC32(4B): {BitConverter.ToString(frame, 44, 4)}")
            Console.WriteLine()

            ' 4. CRC検証
            Console.WriteLine("4. CRC検証")
            Dim bodyLen = frame.Length - 4
            Dim body(bodyLen - 1) As Byte
            Buffer.BlockCopy(frame, 0, body, 0, bodyLen)
            Dim crcCalc = Crc32.Compute(body)
            Dim crcOnFrame = BitConverter.ToUInt32(frame, bodyLen)

            Console.WriteLine($"計算したCRC: 0x{crcCalc:X8}")
            Console.WriteLine($"フレーム内CRC: 0x{crcOnFrame:X8}")
            Console.WriteLine($"CRC一致: {crcCalc = crcOnFrame}")
            Console.WriteLine($"総バイト数: {frame.Length} (期待値: 48)")

        Catch ex As Exception
            Console.WriteLine($"エラー: {ex.Message}")
            Console.WriteLine($"スタックトレース: {ex.StackTrace}")
        End Try

        Console.WriteLine()
        Console.WriteLine("Enterキーを押して終了...")
        Console.ReadLine()
    End Sub
End Module
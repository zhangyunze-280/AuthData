Module Test3
    Sub Main()
        Try
            ' 1. AppDataの生成（前回のAuthDataRequestBuilderを使用）
            Console.WriteLine("1. AppDataの生成")
            Dim app = AuthDataRequestBuilder.Build("20250701")
            Console.WriteLine($"AppData: {BitConverter.ToString(app)}")
            Console.WriteLine()

            ' 2. 駅務機器情報の設定
            Console.WriteLine("2. 駅務機器情報の設定")
            Dim st As New StationInfo With {
                .IdentifyCode = &H1US,    ' 0x0001: 取扱機能
                .OperatorArea = &H12,     ' エリア
                .OperatorUser = &H34,     ' ユーザー
                .DeviceModel = &H16,      ' 自動改札機
                .LineCode = &H1,         ' 路線
                .StationSeq = &H5,       ' 駅順
                .CornerNo = &H1,         ' コーナー
                .MachineNo = &H10,        ' 機器番号
                .Reserved = &H0           ' 予約
            }
            Console.WriteLine($"StationInfo設定完了")
            Console.WriteLine()

            ' 3. フレーム生成
            Console.WriteLine("3. フレーム生成")
            Dim seq As UShort = 1  ' テスト用に固定値
            Dim frame = FrameBuilder.Build(
                seq,               ' シーケンス番号
                retry:=0,         ' リトライ回数
                blockNo:=1,       ' ブロック番号
                blockTotal:=1,    ' 総ブロック数
                st,               ' 駅務機器情報
                app               ' アプリデータ
            )

            ' 4. 結果表示
            Console.WriteLine("生成されたフレーム:")
            Console.WriteLine($"全体長: {frame.Length}バイト")
            Console.WriteLine($"データ: {BitConverter.ToString(frame)}")

            ' 5. フレームの内容を分解して表示
            Console.WriteLine()
            Console.WriteLine("フレームの内容:")
            Console.WriteLine($"ヘッダ(32B): {BitConverter.ToString(frame, 0, 32)}")
            Console.WriteLine($"AppData(12B): {BitConverter.ToString(frame, 32, 12)}")
            Console.WriteLine($"CRC32(4B): {BitConverter.ToString(frame, 44, 4)}")

        Catch ex As Exception
            Console.WriteLine($"エラー: {ex.Message}")
        End Try

        Console.WriteLine()
        Console.WriteLine("Enterキーを押して終了...")
        Console.ReadLine()
    End Sub
End Module
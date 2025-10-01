Module Test1
    Sub Main()
        Try
            ' テストケース1: 正常系
            Console.WriteLine("テストケース1: 20250701")
            Dim result1 = AuthDataRequestBuilder.Build("20250701")
            Console.WriteLine("結果: " & BitConverter.ToString(result1))
            Console.WriteLine()

            ' テストケース2: 別の日付
            Console.WriteLine("テストケース2: 20991231")
            Dim result2 = AuthDataRequestBuilder.Build("20991231")
            Console.WriteLine("結果: " & BitConverter.ToString(result2))
            Console.WriteLine()

            ' テストケース3: エラーケース（7桁の日付）
            Console.WriteLine("テストケース3: 不正な日付（7桁）")
            Dim result3 = AuthDataRequestBuilder.Build("2025070")  ' エラーになるはず

        Catch ex As Exception
            Console.WriteLine("エラー: " & ex.Message)
        End Try

        Console.WriteLine()
        Console.WriteLine("Enterキーを押して終了...")
        Console.ReadLine()
    End Sub
End Module
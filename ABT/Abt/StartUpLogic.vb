Public Class StartUpLogic
    Public Sub AbtOpenReceived(extra As String)

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] StartUpLogic受信完了: " &
                      $"{extra}")

    End Sub

End Class

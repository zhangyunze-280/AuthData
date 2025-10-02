Public Class TankingDataManager1

    Public Sub StartTankingProcess(extra As String)

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] TankingDataManager受信完了: " &
                      $"{extra}")

    End Sub
End Class

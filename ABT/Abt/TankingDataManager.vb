Public Class TankingDataManager

    Public Sub StartTankingProcess(extra As String)

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] TankingDataManager��M����: " &
                      $"{extra}")

    End Sub
End Class

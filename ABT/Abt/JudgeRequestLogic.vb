Public Class JudgeRequestLogic

    Public Sub DecisionRequest(procDir As String, qrCode As String, qrNum As String,
                              reqTime As String, issueDisFlag As String, appBailFlag As String,
                              offlineTktGateFlag As String, execPermitFlag As String,
                              modelType As String, otherStaAppFlag As String,
                              bizOpRegCode As String, bizOpUserCode As String,
                              lineSec As String, staOrder As String)
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] JudgeRequestLogic受信完了: " &
                      $"{procDir}, {qrCode}, {qrNum}, {reqTime}, {issueDisFlag}, {appBailFlag}, " &
                      $"{offlineTktGateFlag}, {execPermitFlag}, {modelType}, {otherStaAppFlag}, " &
                      $"{bizOpRegCode}, {bizOpUserCode}, {lineSec}, {staOrder}")


    End Sub
End Class

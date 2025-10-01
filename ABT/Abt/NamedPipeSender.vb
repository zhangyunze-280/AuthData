Imports System.IO.Pipes
Imports System.Text

Public Class NamedPipeSender

    Private Const PipeName As String = "MyPipe"

    ' 共通：バイト配列をパイプで送信
    Private Sub SendBytes(data As Byte())
        Try
            Using pipeClient As New NamedPipeClientStream(".", PipeName, PipeDirection.Out)
                pipeClient.Connect()
                pipeClient.Write(data, 0, data.Length)
                Console.WriteLine("Data sent: " & BitConverter.ToString(data))
            End Using
        Catch ex As Exception
            Console.WriteLine("Error sending data: " & ex.Message)
        End Try
    End Sub

    ' 共通：文字列をShift-JISバイト配列に変換して送信
    Private Sub SendString(message As String)
        Dim data As Byte() = Encoding.GetEncoding("shift_jis").GetBytes(message)
        SendBytes(data)
    End Sub

    ' -------------------------------
    ' 1. DateRight
    ' -------------------------------
    Public Sub DateRight(procResult As String, seqNum As String, procDir As String,
                         respStatus As String, detCode As String, decResultInfo As String,
                         validStartDate As String, validEndDate As String, tktName As String,
                         errTitle As String, errContText As String, errHandText As String)

        Dim payload As String = String.Join("|", {
            procResult, seqNum, procDir, respStatus, detCode,
            decResultInfo, validStartDate, validEndDate,
            tktName, errTitle, errContText, errHandText
        })

        SendString(payload)
    End Sub

    ' -------------------------------
    ' 2. BackEvent
    ' -------------------------------
    Public Sub BackEvent(procResult As String, seqNum As String, procDir As String,
                         respStatus As String, detCode As String, decResultInfo As String,
                         validStartDate As String, validEndDate As String, tktName As String,
                         errTitle As String, errContText As String, errHandText As String)

        Dim payload As String = String.Join("|", {
            procResult, seqNum, procDir, respStatus, detCode,
            decResultInfo, validStartDate, validEndDate,
            tktName, errTitle, errContText, errHandText
        })

        SendString(payload)
    End Sub

    ' -------------------------------
    ' 3. 状態変更通知
    ' -------------------------------
    ' 判定要求応答（タンキング完了通知）
    Public Sub SendEventAbtTicketGateJudgmentTanking(resultCode As String, sequenceNumber As String)
        Dim payload As String = String.Join("|", {resultCode, sequenceNumber})
        SendString(payload)
    End Sub

    ' -------------------------------
    ' 4. エラー通知
    ' -------------------------------
    Public Sub SendErrorNotification(respStatus As String, detCode As String)
        Dim payload As String = String.Join("|", {respStatus, detCode})
        SendString(payload)
    End Sub

    ' -------------------------------
    ' 5. イベント通知（Abt状態変更）
    ' -------------------------------
    Public Sub SendEventAbtStatusChange(abtCtrlState As String, ebdptDest As String, hasTankingData As Boolean)
        Dim payload As String = String.Join("|", {abtCtrlState}, {ebdptDest}, {If(hasTankingData, "1", "0")})
        SendString(payload)
    End Sub

    ' -------------------------------
    ' 6. 初期データ通知
    ' -------------------------------
    Public Sub SendEventAbtInitialData()
        Dim payload As String = "INITIAL_DATA"
        SendString(payload)
    End Sub

    ' -------------------------------
    ' 7. 認証データ通知
    ' -------------------------------
    Public Sub SendEventAbtAuthenticationData(procResult As String, seqNum As String,
                                              respStatus As String, detCode As String,
                                              newAuthData As String, authData As String,
                                              qrCompanyNum As String, qrCode1 As String,
                                              qrCode128 As String)

        Dim payload As String = String.Join("|", {
            procResult, seqNum, respStatus, detCode,
            newAuthData, authData, qrCompanyNum, qrCode1, qrCode128
        })

        SendString(payload)
    End Sub

    ' -------------------------------
    ' 8. 認証データエラー通知
    ' -------------------------------
    Public Sub SendErrorAbtAuthenticationData(procResult As String, seqNum As String,
                                              respStatus As String, detCode As String,
                                              newAuthData As String, authData As String,
                                              qrCompanyNum As String, qrCode1 As String,
                                              qrCode128 As String)

        Dim payload As String = String.Join("|", {
            procResult, seqNum, respStatus, detCode,
            newAuthData, authData, qrCompanyNum, qrCode1, qrCode128
        })

        SendString(payload)
    End Sub

    ' -------------------------------
    ' 9. タンキングデータ送信（CSV利用）
    ' -------------------------------
    Public Sub SendTankingDataToCsv(filePath As String, tankingData As List(Of String()))
        ' tankingData: 各行が配列（列ごとのデータ）
        Using writer As New IO.StreamWriter(filePath, False, Encoding.UTF8)
            For Each row As String() In tankingData
                writer.WriteLine(String.Join(",", row))
            Next
        End Using
        Console.WriteLine("Tanking data saved to CSV: " & filePath)
    End Sub

End Class

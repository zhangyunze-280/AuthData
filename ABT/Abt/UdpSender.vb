' ================================
' UdpSender.vb
' ================================
Option Strict On
Option Explicit On
Imports System.Net
Imports System.Net.Sockets

Public NotInheritable Class UdpSender
    Private Sub New() : End Sub

    Public Shared Sub Send(ip As String, port As Integer, payload As Byte())
        Using udp As New UdpClient()
            udp.Client.SendTimeout = 1000
            udp.Send(payload, payload.Length, ip, port)
        End Using
    End Sub
End Class

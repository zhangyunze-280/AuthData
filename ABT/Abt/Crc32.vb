' ================================
' Crc32.vb  (IEEE 802.3 / PKZip 方式)
' 初期値: 0xFFFFFFFF, 反転入出力, 最終XOR 0xFFFFFFFF
' 多項式: 0xEDB88320 (reflected)
' 使い方:
'   Dim crc As UInteger = Crc32.Compute(bytes)
'   Dim crcLe As Byte() = BitConverter.GetBytes(crc) ' フレーム末尾へ(LE)
' 検証値:
'   Crc32.Compute(ASCII("123456789")) = &HCBF43926UI
' ================================
Option Strict On
Option Explicit On

Public NotInheritable Class Crc32
    Private Sub New()
    End Sub

    Private Shared ReadOnly Poly As UInteger = &HEDB88320UI
    Private Shared ReadOnly Table As UInteger() = CreateTable()

    Private Shared Function CreateTable() As UInteger()
        Dim t(255) As UInteger
        For i As Integer = 0 To 255
            Dim c As UInteger = CUInt(i)
            For j As Integer = 0 To 7
                If (c And 1UI) <> 0UI Then
                    c = (c >> 1) Xor Poly
                Else
                    c >>= 1
                End If
            Next
            t(i) = c
        Next
        Return t
    End Function

    ' 一括計算
    Public Shared Function Compute(data As Byte()) As UInteger
        If data Is Nothing OrElse data.Length = 0 Then
            Return &H0UI Xor &HFFFFFFFFUI ' 空配列→ 0 (IEEE慣習)
        End If
        Dim crc As UInteger = &HFFFFFFFFUI
        For Each b As Byte In data
            Dim idx As Integer = CInt((crc Xor b) And &HFFUI)
            crc = (crc >> 8) Xor Table(idx)
        Next
        Return Not crc
    End Function

    ' インクリメンタル（任意）
    Public Shared Function Update(current As UInteger, chunk As Byte()) As UInteger
        Dim crc As UInteger = Not current ' 入口で反転してから回す
        For Each b As Byte In chunk
            Dim idx As Integer = CInt((crc Xor b) And &HFFUI)
            crc = (crc >> 8) Xor Table(idx)
        Next
        Return Not crc
    End Function
End Class

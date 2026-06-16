Partial Public Class GBACore
    Public Property R(index As Integer) As UInteger
        Get
            If index = 15 Then Return UserRegs(15)
            Dim mode = CPSR And &H1FUI
            Select Case index
                Case 8 To 12 : Return If(mode = &H11, FIQRegs(index - 8), UserRegs(index))
                Case 13, 14
                    Select Case mode
                        Case &H11 : Return FIQRegs(index - 8)
                        Case &H12 : Return IRQRegs(index - 13)
                        Case &H13 : Return SVCRegs(index - 13)
                        Case &H17 : Return ABTRegs(index - 13)
                        Case &H1B : Return UNDRegs(index - 13)
                        Case Else : Return UserRegs(index)
                    End Select
                Case Else : Return UserRegs(index)
            End Select
        End Get
        Set(value As UInteger)
            If index = 15 Then UserRegs(15) = value : Return
            Dim mode = CPSR And &H1FUI
            Select Case index
                Case 8 To 12 : If mode = &H11 Then FIQRegs(index - 8) = value Else UserRegs(index) = value
                Case 13, 14
                    Select Case mode
                        Case &H11 : FIQRegs(index - 8) = value
                        Case &H12 : IRQRegs(index - 13) = value
                        Case &H13 : SVCRegs(index - 13) = value
                        Case &H17 : ABTRegs(index - 13) = value
                        Case &H1B : UNDRegs(index - 13) = value
                        Case Else : UserRegs(index) = value
                    End Select
                Case Else : UserRegs(index) = value
            End Select
        End Set
    End Property

    Public Property SPSR As UInteger
        Get
            Select Case CPSR And &H1FUI
                Case &H11 : Return SPSRs(0)
                Case &H12 : Return SPSRs(3)
                Case &H13 : Return SPSRs(1)
                Case &H17 : Return SPSRs(2)
                Case &H1B : Return SPSRs(4)
                Case Else : Return CPSR
            End Select
        End Get
        Set(value As UInteger)
            Select Case CPSR And &H1FUI
                Case &H11 : SPSRs(0) = value
                Case &H12 : SPSRs(3) = value
                Case &H13 : SPSRs(1) = value
                Case &H17 : SPSRs(2) = value
                Case &H1B : SPSRs(4) = value
            End Select
        End Set
    End Property

    Public ReadOnly Property PC As UInteger
        Get
            Return R(15)
        End Get
    End Property

    Public Function GetRegister(index As Integer) As UInteger
        If index >= 0 AndAlso index <= 15 Then Return R(index)
        If index = 16 Then Return CPSR
        Return 0
    End Function

    Public Property ThumbMode As Boolean
        Get
            Return (CPSR And &H20) <> 0
        End Get
        Set(value As Boolean)
            If value Then CPSR = CPSR Or &H20UI Else CPSR = CPSR And Not &H20UI
        End Set
    End Property
End Class

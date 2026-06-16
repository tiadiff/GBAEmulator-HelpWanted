Partial Public Class GBACore
    Partial Public Class GBA_APU
        Public Class RingBuffer
            Private Buffer() As Single
            Private WriteIndex As Integer
            Public Capacity As Integer

            Public Sub New(cap As Integer)
                Capacity = cap
                ReDim Buffer(Capacity - 1)
                WriteIndex = 0
            End Sub

            Public Sub Push(val As Single)
                Buffer(WriteIndex) = val
                WriteIndex = (WriteIndex + 1) Mod Capacity
            End Sub

            Public Function GetSnapshot() As Single()
                Dim snap(Capacity - 1) As Single
                Dim startIdx = WriteIndex
                For i = 0 To Capacity - 1
                    snap(i) = Buffer((startIdx + i) Mod Capacity)
                Next
                Return snap
            End Function
        End Class
    End Class
End Class

@Code
    Dim i as Integer = 1
End Code

@While i <= 10
    @<p>Hello from VB.Net, #@(i)</p>
    i += 1
    'End While
End While

@If i = 11 Then
    @<p>We wrote 10 lines!</p>
    Dim s = "End If"
End If

@Select Case i
    Case 11
        @<p>No really, we wrote 10 lines!</p>
    Case Else
        @<p>Actually, we didn't...</p>
End Select

@For j as Integer = 1 to 10 Step 2
    @<p>Hello again from VB.Net, #@(j)</p>
Next

@Try
    @<p>That time, we wrote 5 lines!</p>
Catch ex as Exception
    @<p>Oh no! An error occurred: @(ex.Message)</p>
End Try

@With i
    @<p>i is now @(.ToString())</p>
End With

@SyncLock New Object()
    @<p>This block is locked, for your security!</p>
End SyncLock

@Using New System.IO.MemoryStream()
    @<p>Some random memory stream will be disposed after rendering this block</p>
End Using

@SyntaxSampleHelpers.CodeForLink(Me)
@Functions

End Functions

@Functions
    Private _rand as New Random()
    Private Function RandomInt() as Integer
        Return _rand.Next()
    End Function
End Functions

Here's a random number: @RandomInt()
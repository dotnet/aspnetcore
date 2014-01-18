@Helper Italic(s As String)
    s = s.ToUpper()
    @Helper Bold(s As String)
        s = s.ToUpper()
        @<strong>@s</strong>
    End Helper
    @<em>@Bold(s)</em>
End Helper

@Italic("Hello")
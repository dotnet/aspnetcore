@Helper Bold(s as String)
    s = s.ToUpper()
    @<strong>@s</strong>
End Helper

@Helper Italic

@Bold("Hello")
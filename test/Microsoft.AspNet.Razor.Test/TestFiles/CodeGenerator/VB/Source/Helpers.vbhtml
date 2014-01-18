@Helper Bold(s as String)
    s = s.ToUpper()
    @<strong>@s</strong>
End Helper

@Helper Italic(s as String)
    s = s.ToUpper()
    @<em>@s</em>
End Helper

@Bold("Hello")
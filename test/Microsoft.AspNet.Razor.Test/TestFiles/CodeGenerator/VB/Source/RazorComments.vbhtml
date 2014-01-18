@*This is not going to be rendered*@
<p>This should @* not *@ be shown</p>

@Code
    @* Throw new Exception("Oh no!") *@
End Code

@Code Dim bar As String = "@* bar *@" End Code
<p>But this should show the comment syntax: @bar</p>
<p>So should this: @@* bar *@@</p>

@(a@**@b)
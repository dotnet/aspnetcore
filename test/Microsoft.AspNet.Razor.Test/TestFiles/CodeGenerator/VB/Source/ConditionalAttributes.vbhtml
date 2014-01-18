@Helper Link(ByVal url As String, text As String) 
    @<a href="@url">@text</a> 
End Helper 

@Code
    Dim ch = True
    Dim cls = "bar"
    @<a href="Foo" />
    @<p class="@cls" />
    @<p class="foo @cls" />
    @<p class="@cls foo" />
    @<input type="checkbox" checked="@ch" />
    @<input type="checkbox" checked="foo @ch" />
    @<p class="@If cls IsNot Nothing Then @cls End If" />
    @<a href="~/Foo" />
End Code
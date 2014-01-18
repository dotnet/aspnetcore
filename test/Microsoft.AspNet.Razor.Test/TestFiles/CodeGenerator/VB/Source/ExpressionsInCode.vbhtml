@Code
    Dim foo As Object = Nothing
    Dim bar as String = "Foo"
End Code

@If foo IsNot Nothing Then
    @foo
Else
    @<p>Foo is Null!</p>
End If

<p>
@If Not String.IsNullOrEmpty(bar) Then
    @(bar.Replace("F", "B"))
End If
</p>
@Imports System.Web.Helpers

@Functions
    Public Function Repeat(times As Integer, template As Func(Of Integer, object)) As HelperResult
        Return New HelperResult(Sub(writer)
            For i = 0 to times
                DirectCast(template(i), HelperResult).WriteTo(writer)
            Next i
        End Sub)
    End Function
End Functions

@Code
    Dim foo As Func(Of Object, Object) = @<text>This works @item!</text>
    @foo("too")
End Code

<ul>
@(Repeat(10, @@<li>Item #@item</li>))
</ul>

<p>
@Repeat(10,
    @@: This is line#@item of markup<br/>
)
</p>

<ul>
    @Repeat(10, @@<li>
        Item #@item
        @Code Dim parent = item End Code
        <ul>
            <li>Child Items... ?</li>
        </ul>
    </li>)
</ul>
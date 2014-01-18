<div>
            @For i = 1 to 10
@<p>This is item #@i</p>
            Next
</div>

<p>
@(Foo(Bar.Baz))
@Foo(@@<p>Bar @baz Biz</p>)
</p>

@Section Footer
    <p>Foo</p>
    @bar
End Section

@Helper Foo()
    If True Then
        @<p>Foo</p>
    End If
End Helper
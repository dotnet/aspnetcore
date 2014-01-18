<a href="~/Foo">Foo</a>
<a href="~/Products/@product.id">@product.Name</a>
<a href="~/Products/@product.id/Detail">Details</a>
<a href="~/A+Really(Crazy),Url.Is:This/@product.id/Detail">Crazy Url!</a>

@Code
    @<text>
        <a href="~/Foo">Foo</a>
        <a href="~/Products/@product.id">@product.Name</a>
        <a href="~/Products/@product.id/Detail">Details</a>
        <a href="~/A+Really(Crazy),Url.Is:This/@product.id/Detail">Crazy Url!</a>
    </text>
End Code

@Section Foo
    <a href="~/Foo">Foo</a>
    <a href="~/Products/@product.id">@product.Name</a>
    <a href="~/Products/@product.id/Detail">Details</a>
    <a href="~/A+Really(Crazy),Url.Is:This/@product.id/Detail">Crazy Url!</a>
End Section
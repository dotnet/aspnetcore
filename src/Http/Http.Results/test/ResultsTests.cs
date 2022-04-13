// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.AspNetCore.Http.HttpResults;

public class ResultsTests
{
    [Theory]
    [MemberData(nameof(FactoryMethodsFromTuples))]
    public void FactoryMethod_ReturnsCorrectResultType(Expression<Func<IResult>> expression, Type expectedReturnType)
    {
        var method = expression.Compile();
        Assert.IsType(expectedReturnType, method());
    }

    [Fact]
    public void TestTheTests()
    {
        var testedMethods = new HashSet<string>(FactoryMethodsTuples.Select(t => GetMemberName(t.Item1.Body)));
        var actualMethods = new HashSet<string>(typeof(Results).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name));

        // Ensure every static method on Results type is covered by at least the default case for its parameters
        Assert.All(actualMethods, name => Assert.Single(testedMethods, name));
    }

    private static string GetMemberName(Expression expression)
    {
        return expression switch
        {
            MethodCallExpression mce => mce.Method.Name,
            MemberExpression me => me.Member.Name,
            _ => throw new InvalidOperationException()
        };
    }

    private static IEnumerable<(Expression<Func<IResult>>, Type)> FactoryMethodsTuples { get; } = new List<(Expression<Func<IResult>>, Type)>
    {
        (() => Results.Accepted(null, null), typeof(Accepted)),
        (() => Results.Accepted(null, new()), typeof(Accepted<object>)),
        (() => Results.AcceptedAtRoute("routeName", null, null), typeof(AcceptedAtRoute)),
        (() => Results.AcceptedAtRoute("routeName", null, new()), typeof(AcceptedAtRoute<object>)),
        (() => Results.BadRequest(null), typeof(BadRequest)),
        (() => Results.BadRequest(new()), typeof(BadRequest<object>)),
        (() => Results.Bytes(new byte[0], null, null, false, null, null), typeof(FileContentHttpResult)),
        (() => Results.Challenge(null, null), typeof(ChallengeHttpResult)),
        (() => Results.Conflict(null), typeof(Conflict)),
        (() => Results.Conflict(new()), typeof(Conflict<object>)),
        (() => Results.Content("content", null, null), typeof(ContentHttpResult)),
        (() => Results.Created("/path", null), typeof(Created)),
        (() => Results.Created("/path", new()), typeof(Created<object>)),
        (() => Results.CreatedAtRoute("routeName", null, null), typeof(CreatedAtRoute)),
        (() => Results.CreatedAtRoute("routeName", null, new()), typeof(CreatedAtRoute<object>)),
        (() => Results.Empty, typeof(EmptyHttpResult)),
        (() => Results.File(new byte[0], null, null, false, null, null), typeof(FileContentHttpResult)),
        (() => Results.File(new MemoryStream(), null, null, null, null, false), typeof(FileStreamHttpResult)),
        (() => Results.File("C:\\path", null, null, null, null, false), typeof(PhysicalFileHttpResult)),
        (() => Results.File("path", null, null, null, null, false), typeof(VirtualFileHttpResult)),
        (() => Results.Forbid(null, null), typeof(ForbidHttpResult)),
        (() => Results.Json(new(), null, null, null), typeof(JsonHttpResult<object>)),
        (() => Results.NoContent(), typeof(NoContent)),
        (() => Results.NotFound(null), typeof(NotFound)),
        (() => Results.NotFound(new()), typeof(NotFound<object>)),
        (() => Results.Ok(null), typeof(Ok)),
        (() => Results.Ok(new()), typeof(Ok<object>)),
        (() => Results.Problem(new()), typeof(ProblemHttpResult)),
        (() => Results.Stream(new MemoryStream(), null, null, null, null, false), typeof(FileStreamHttpResult)),
        (() => Results.Stream(s => Task.CompletedTask, null, null, null, null), typeof(PushStreamHttpResult)),
        (() => Results.Text("content", null, null), typeof(ContentHttpResult)),
        (() => Results.Redirect("/path", false, false), typeof(RedirectHttpResult)),
        (() => Results.LocalRedirect("/path", false, false), typeof(RedirectHttpResult)),
        (() => Results.RedirectToRoute("routeName", null, false, false, null), typeof(RedirectToRouteHttpResult)),
        (() => Results.SignIn(new(), null, null), typeof(SignInHttpResult)),
        (() => Results.SignOut(new(), null), typeof(SignOutHttpResult)),
        (() => Results.StatusCode(200), typeof(StatusCodeHttpResult)),
        (() => Results.Unauthorized(), typeof(UnauthorizedHttpResult)),
        (() => Results.UnprocessableEntity(null), typeof(UnprocessableEntity)),
        (() => Results.UnprocessableEntity(new()), typeof(UnprocessableEntity<object>)),
        (() => Results.ValidationProblem(new Dictionary<string, string[]>(), null, null, null, null, null, null), typeof(ProblemHttpResult))
    };

    public static IEnumerable<object[]> FactoryMethodsFromTuples() => FactoryMethodsTuples.Select(t => new object[] { t.Item1, t.Item2 });
}

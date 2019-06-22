# Microsoft.CodeAnalysis.CSharp.Syntax

``` diff
-namespace Microsoft.CodeAnalysis.CSharp.Syntax {
 {
-    public sealed class AccessorDeclarationSyntax : CSharpSyntaxNode {
 {
-        public SyntaxList<AttributeListSyntax> AttributeLists { get; }

-        public BlockSyntax Body { get; }

-        public ArrowExpressionClauseSyntax ExpressionBody { get; }

-        public SyntaxToken Keyword { get; }

-        public SyntaxTokenList Modifiers { get; }

-        public SyntaxToken SemicolonToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public AccessorDeclarationSyntax AddAttributeLists(params AttributeListSyntax[] items);

-        public AccessorDeclarationSyntax AddBodyStatements(params StatementSyntax[] items);

-        public AccessorDeclarationSyntax AddModifiers(params SyntaxToken[] items);

-        public AccessorDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken keyword, BlockSyntax body, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolonToken);

-        public AccessorDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken keyword, BlockSyntax body, SyntaxToken semicolonToken);

-        public AccessorDeclarationSyntax WithAttributeLists(SyntaxList<AttributeListSyntax> attributeLists);

-        public AccessorDeclarationSyntax WithBody(BlockSyntax body);

-        public AccessorDeclarationSyntax WithExpressionBody(ArrowExpressionClauseSyntax expressionBody);

-        public AccessorDeclarationSyntax WithKeyword(SyntaxToken keyword);

-        public AccessorDeclarationSyntax WithModifiers(SyntaxTokenList modifiers);

-        public AccessorDeclarationSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-    }
-    public sealed class AccessorListSyntax : CSharpSyntaxNode {
 {
-        public SyntaxList<AccessorDeclarationSyntax> Accessors { get; }

-        public SyntaxToken CloseBraceToken { get; }

-        public SyntaxToken OpenBraceToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public AccessorListSyntax AddAccessors(params AccessorDeclarationSyntax[] items);

-        public AccessorListSyntax Update(SyntaxToken openBraceToken, SyntaxList<AccessorDeclarationSyntax> accessors, SyntaxToken closeBraceToken);

-        public AccessorListSyntax WithAccessors(SyntaxList<AccessorDeclarationSyntax> accessors);

-        public AccessorListSyntax WithCloseBraceToken(SyntaxToken closeBraceToken);

-        public AccessorListSyntax WithOpenBraceToken(SyntaxToken openBraceToken);

-    }
-    public sealed class AliasQualifiedNameSyntax : NameSyntax {
 {
-        public IdentifierNameSyntax Alias { get; }

-        public SyntaxToken ColonColonToken { get; }

-        public SimpleNameSyntax Name { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public AliasQualifiedNameSyntax Update(IdentifierNameSyntax alias, SyntaxToken colonColonToken, SimpleNameSyntax name);

-        public AliasQualifiedNameSyntax WithAlias(IdentifierNameSyntax alias);

-        public AliasQualifiedNameSyntax WithColonColonToken(SyntaxToken colonColonToken);

-        public AliasQualifiedNameSyntax WithName(SimpleNameSyntax name);

-    }
-    public abstract class AnonymousFunctionExpressionSyntax : ExpressionSyntax {
 {
-        public abstract SyntaxToken AsyncKeyword { get; }

-        public abstract CSharpSyntaxNode Body { get; }

-    }
-    public sealed class AnonymousMethodExpressionSyntax : AnonymousFunctionExpressionSyntax {
 {
-        public override SyntaxToken AsyncKeyword { get; }

-        public BlockSyntax Block { get; }

-        public override CSharpSyntaxNode Body { get; }

-        public SyntaxToken DelegateKeyword { get; }

-        public ParameterListSyntax ParameterList { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public AnonymousMethodExpressionSyntax AddBlockStatements(params StatementSyntax[] items);

-        public AnonymousMethodExpressionSyntax AddParameterListParameters(params ParameterSyntax[] items);

-        public AnonymousMethodExpressionSyntax Update(SyntaxToken asyncKeyword, SyntaxToken delegateKeyword, ParameterListSyntax parameterList, CSharpSyntaxNode body);

-        public AnonymousMethodExpressionSyntax WithAsyncKeyword(SyntaxToken asyncKeyword);

-        public AnonymousMethodExpressionSyntax WithBlock(BlockSyntax block);

-        public AnonymousMethodExpressionSyntax WithBody(CSharpSyntaxNode body);

-        public AnonymousMethodExpressionSyntax WithDelegateKeyword(SyntaxToken delegateKeyword);

-        public AnonymousMethodExpressionSyntax WithParameterList(ParameterListSyntax parameterList);

-    }
-    public sealed class AnonymousObjectCreationExpressionSyntax : ExpressionSyntax {
 {
-        public SyntaxToken CloseBraceToken { get; }

-        public SeparatedSyntaxList<AnonymousObjectMemberDeclaratorSyntax> Initializers { get; }

-        public SyntaxToken NewKeyword { get; }

-        public SyntaxToken OpenBraceToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public AnonymousObjectCreationExpressionSyntax AddInitializers(params AnonymousObjectMemberDeclaratorSyntax[] items);

-        public AnonymousObjectCreationExpressionSyntax Update(SyntaxToken newKeyword, SyntaxToken openBraceToken, SeparatedSyntaxList<AnonymousObjectMemberDeclaratorSyntax> initializers, SyntaxToken closeBraceToken);

-        public AnonymousObjectCreationExpressionSyntax WithCloseBraceToken(SyntaxToken closeBraceToken);

-        public AnonymousObjectCreationExpressionSyntax WithInitializers(SeparatedSyntaxList<AnonymousObjectMemberDeclaratorSyntax> initializers);

-        public AnonymousObjectCreationExpressionSyntax WithNewKeyword(SyntaxToken newKeyword);

-        public AnonymousObjectCreationExpressionSyntax WithOpenBraceToken(SyntaxToken openBraceToken);

-    }
-    public sealed class AnonymousObjectMemberDeclaratorSyntax : CSharpSyntaxNode {
 {
-        public ExpressionSyntax Expression { get; }

-        public NameEqualsSyntax NameEquals { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public AnonymousObjectMemberDeclaratorSyntax Update(NameEqualsSyntax nameEquals, ExpressionSyntax expression);

-        public AnonymousObjectMemberDeclaratorSyntax WithExpression(ExpressionSyntax expression);

-        public AnonymousObjectMemberDeclaratorSyntax WithNameEquals(NameEqualsSyntax nameEquals);

-    }
-    public sealed class ArgumentListSyntax : BaseArgumentListSyntax {
 {
-        public override SeparatedSyntaxList<ArgumentSyntax> Arguments { get; }

-        public SyntaxToken CloseParenToken { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ArgumentListSyntax AddArguments(params ArgumentSyntax[] items);

-        public ArgumentListSyntax Update(SyntaxToken openParenToken, SeparatedSyntaxList<ArgumentSyntax> arguments, SyntaxToken closeParenToken);

-        public ArgumentListSyntax WithArguments(SeparatedSyntaxList<ArgumentSyntax> arguments);

-        public ArgumentListSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public ArgumentListSyntax WithOpenParenToken(SyntaxToken openParenToken);

-    }
-    public sealed class ArgumentSyntax : CSharpSyntaxNode {
 {
-        public ExpressionSyntax Expression { get; }

-        public NameColonSyntax NameColon { get; }

-        public SyntaxToken RefKindKeyword { get; }

-        public SyntaxToken RefOrOutKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ArgumentSyntax Update(NameColonSyntax nameColon, SyntaxToken refKindKeyword, ExpressionSyntax expression);

-        public ArgumentSyntax WithExpression(ExpressionSyntax expression);

-        public ArgumentSyntax WithNameColon(NameColonSyntax nameColon);

-        public ArgumentSyntax WithRefKindKeyword(SyntaxToken refKindKeyword);

-        public ArgumentSyntax WithRefOrOutKeyword(SyntaxToken refOrOutKeyword);

-    }
-    public sealed class ArrayCreationExpressionSyntax : ExpressionSyntax {
 {
-        public InitializerExpressionSyntax Initializer { get; }

-        public SyntaxToken NewKeyword { get; }

-        public ArrayTypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ArrayCreationExpressionSyntax AddTypeRankSpecifiers(params ArrayRankSpecifierSyntax[] items);

-        public ArrayCreationExpressionSyntax Update(SyntaxToken newKeyword, ArrayTypeSyntax type, InitializerExpressionSyntax initializer);

-        public ArrayCreationExpressionSyntax WithInitializer(InitializerExpressionSyntax initializer);

-        public ArrayCreationExpressionSyntax WithNewKeyword(SyntaxToken newKeyword);

-        public ArrayCreationExpressionSyntax WithType(ArrayTypeSyntax type);

-    }
-    public sealed class ArrayRankSpecifierSyntax : CSharpSyntaxNode {
 {
-        public SyntaxToken CloseBracketToken { get; }

-        public SyntaxToken OpenBracketToken { get; }

-        public int Rank { get; }

-        public SeparatedSyntaxList<ExpressionSyntax> Sizes { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ArrayRankSpecifierSyntax AddSizes(params ExpressionSyntax[] items);

-        public ArrayRankSpecifierSyntax Update(SyntaxToken openBracketToken, SeparatedSyntaxList<ExpressionSyntax> sizes, SyntaxToken closeBracketToken);

-        public ArrayRankSpecifierSyntax WithCloseBracketToken(SyntaxToken closeBracketToken);

-        public ArrayRankSpecifierSyntax WithOpenBracketToken(SyntaxToken openBracketToken);

-        public ArrayRankSpecifierSyntax WithSizes(SeparatedSyntaxList<ExpressionSyntax> sizes);

-    }
-    public sealed class ArrayTypeSyntax : TypeSyntax {
 {
-        public TypeSyntax ElementType { get; }

-        public SyntaxList<ArrayRankSpecifierSyntax> RankSpecifiers { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ArrayTypeSyntax AddRankSpecifiers(params ArrayRankSpecifierSyntax[] items);

-        public ArrayTypeSyntax Update(TypeSyntax elementType, SyntaxList<ArrayRankSpecifierSyntax> rankSpecifiers);

-        public ArrayTypeSyntax WithElementType(TypeSyntax elementType);

-        public ArrayTypeSyntax WithRankSpecifiers(SyntaxList<ArrayRankSpecifierSyntax> rankSpecifiers);

-    }
-    public sealed class ArrowExpressionClauseSyntax : CSharpSyntaxNode {
 {
-        public SyntaxToken ArrowToken { get; }

-        public ExpressionSyntax Expression { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ArrowExpressionClauseSyntax Update(SyntaxToken arrowToken, ExpressionSyntax expression);

-        public ArrowExpressionClauseSyntax WithArrowToken(SyntaxToken arrowToken);

-        public ArrowExpressionClauseSyntax WithExpression(ExpressionSyntax expression);

-    }
-    public sealed class AssignmentExpressionSyntax : ExpressionSyntax {
 {
-        public ExpressionSyntax Left { get; }

-        public SyntaxToken OperatorToken { get; }

-        public ExpressionSyntax Right { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public AssignmentExpressionSyntax Update(ExpressionSyntax left, SyntaxToken operatorToken, ExpressionSyntax right);

-        public AssignmentExpressionSyntax WithLeft(ExpressionSyntax left);

-        public AssignmentExpressionSyntax WithOperatorToken(SyntaxToken operatorToken);

-        public AssignmentExpressionSyntax WithRight(ExpressionSyntax right);

-    }
-    public sealed class AttributeArgumentListSyntax : CSharpSyntaxNode {
 {
-        public SeparatedSyntaxList<AttributeArgumentSyntax> Arguments { get; }

-        public SyntaxToken CloseParenToken { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public AttributeArgumentListSyntax AddArguments(params AttributeArgumentSyntax[] items);

-        public AttributeArgumentListSyntax Update(SyntaxToken openParenToken, SeparatedSyntaxList<AttributeArgumentSyntax> arguments, SyntaxToken closeParenToken);

-        public AttributeArgumentListSyntax WithArguments(SeparatedSyntaxList<AttributeArgumentSyntax> arguments);

-        public AttributeArgumentListSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public AttributeArgumentListSyntax WithOpenParenToken(SyntaxToken openParenToken);

-    }
-    public sealed class AttributeArgumentSyntax : CSharpSyntaxNode {
 {
-        public ExpressionSyntax Expression { get; }

-        public NameColonSyntax NameColon { get; }

-        public NameEqualsSyntax NameEquals { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public AttributeArgumentSyntax Update(NameEqualsSyntax nameEquals, NameColonSyntax nameColon, ExpressionSyntax expression);

-        public AttributeArgumentSyntax WithExpression(ExpressionSyntax expression);

-        public AttributeArgumentSyntax WithNameColon(NameColonSyntax nameColon);

-        public AttributeArgumentSyntax WithNameEquals(NameEqualsSyntax nameEquals);

-    }
-    public sealed class AttributeListSyntax : CSharpSyntaxNode {
 {
-        public SeparatedSyntaxList<AttributeSyntax> Attributes { get; }

-        public SyntaxToken CloseBracketToken { get; }

-        public SyntaxToken OpenBracketToken { get; }

-        public AttributeTargetSpecifierSyntax Target { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public AttributeListSyntax AddAttributes(params AttributeSyntax[] items);

-        public AttributeListSyntax Update(SyntaxToken openBracketToken, AttributeTargetSpecifierSyntax target, SeparatedSyntaxList<AttributeSyntax> attributes, SyntaxToken closeBracketToken);

-        public AttributeListSyntax WithAttributes(SeparatedSyntaxList<AttributeSyntax> attributes);

-        public AttributeListSyntax WithCloseBracketToken(SyntaxToken closeBracketToken);

-        public AttributeListSyntax WithOpenBracketToken(SyntaxToken openBracketToken);

-        public AttributeListSyntax WithTarget(AttributeTargetSpecifierSyntax target);

-    }
-    public sealed class AttributeSyntax : CSharpSyntaxNode {
 {
-        public AttributeArgumentListSyntax ArgumentList { get; }

-        public NameSyntax Name { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public AttributeSyntax AddArgumentListArguments(params AttributeArgumentSyntax[] items);

-        public AttributeSyntax Update(NameSyntax name, AttributeArgumentListSyntax argumentList);

-        public AttributeSyntax WithArgumentList(AttributeArgumentListSyntax argumentList);

-        public AttributeSyntax WithName(NameSyntax name);

-    }
-    public sealed class AttributeTargetSpecifierSyntax : CSharpSyntaxNode {
 {
-        public SyntaxToken ColonToken { get; }

-        public SyntaxToken Identifier { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public AttributeTargetSpecifierSyntax Update(SyntaxToken identifier, SyntaxToken colonToken);

-        public AttributeTargetSpecifierSyntax WithColonToken(SyntaxToken colonToken);

-        public AttributeTargetSpecifierSyntax WithIdentifier(SyntaxToken identifier);

-    }
-    public sealed class AwaitExpressionSyntax : ExpressionSyntax {
 {
-        public SyntaxToken AwaitKeyword { get; }

-        public ExpressionSyntax Expression { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public AwaitExpressionSyntax Update(SyntaxToken awaitKeyword, ExpressionSyntax expression);

-        public AwaitExpressionSyntax WithAwaitKeyword(SyntaxToken awaitKeyword);

-        public AwaitExpressionSyntax WithExpression(ExpressionSyntax expression);

-    }
-    public sealed class BadDirectiveTriviaSyntax : DirectiveTriviaSyntax {
 {
-        public override SyntaxToken EndOfDirectiveToken { get; }

-        public override SyntaxToken HashToken { get; }

-        public SyntaxToken Identifier { get; }

-        public override bool IsActive { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public BadDirectiveTriviaSyntax Update(SyntaxToken hashToken, SyntaxToken identifier, SyntaxToken endOfDirectiveToken, bool isActive);

-        public BadDirectiveTriviaSyntax WithEndOfDirectiveToken(SyntaxToken endOfDirectiveToken);

-        public BadDirectiveTriviaSyntax WithHashToken(SyntaxToken hashToken);

-        public BadDirectiveTriviaSyntax WithIdentifier(SyntaxToken identifier);

-        public BadDirectiveTriviaSyntax WithIsActive(bool isActive);

-    }
-    public abstract class BaseArgumentListSyntax : CSharpSyntaxNode {
 {
-        public abstract SeparatedSyntaxList<ArgumentSyntax> Arguments { get; }

-    }
-    public abstract class BaseCrefParameterListSyntax : CSharpSyntaxNode {
 {
-        public abstract SeparatedSyntaxList<CrefParameterSyntax> Parameters { get; }

-    }
-    public sealed class BaseExpressionSyntax : InstanceExpressionSyntax {
 {
-        public SyntaxToken Token { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public BaseExpressionSyntax Update(SyntaxToken token);

-        public BaseExpressionSyntax WithToken(SyntaxToken token);

-    }
-    public abstract class BaseFieldDeclarationSyntax : MemberDeclarationSyntax {
 {
-        public abstract SyntaxList<AttributeListSyntax> AttributeLists { get; }

-        public abstract VariableDeclarationSyntax Declaration { get; }

-        public abstract SyntaxTokenList Modifiers { get; }

-        public abstract SyntaxToken SemicolonToken { get; }

-    }
-    public sealed class BaseListSyntax : CSharpSyntaxNode {
 {
-        public SyntaxToken ColonToken { get; }

-        public SeparatedSyntaxList<BaseTypeSyntax> Types { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public BaseListSyntax AddTypes(params BaseTypeSyntax[] items);

-        public BaseListSyntax Update(SyntaxToken colonToken, SeparatedSyntaxList<BaseTypeSyntax> types);

-        public BaseListSyntax WithColonToken(SyntaxToken colonToken);

-        public BaseListSyntax WithTypes(SeparatedSyntaxList<BaseTypeSyntax> types);

-    }
-    public abstract class BaseMethodDeclarationSyntax : MemberDeclarationSyntax {
 {
-        public abstract SyntaxList<AttributeListSyntax> AttributeLists { get; }

-        public abstract BlockSyntax Body { get; }

-        public abstract ArrowExpressionClauseSyntax ExpressionBody { get; }

-        public abstract SyntaxTokenList Modifiers { get; }

-        public abstract ParameterListSyntax ParameterList { get; }

-        public abstract SyntaxToken SemicolonToken { get; }

-    }
-    public abstract class BaseParameterListSyntax : CSharpSyntaxNode {
 {
-        public abstract SeparatedSyntaxList<ParameterSyntax> Parameters { get; }

-    }
-    public abstract class BasePropertyDeclarationSyntax : MemberDeclarationSyntax {
 {
-        public abstract AccessorListSyntax AccessorList { get; }

-        public abstract SyntaxList<AttributeListSyntax> AttributeLists { get; }

-        public abstract ExplicitInterfaceSpecifierSyntax ExplicitInterfaceSpecifier { get; }

-        public abstract SyntaxTokenList Modifiers { get; }

-        public abstract TypeSyntax Type { get; }

-    }
-    public abstract class BaseTypeDeclarationSyntax : MemberDeclarationSyntax {
 {
-        public abstract SyntaxList<AttributeListSyntax> AttributeLists { get; }

-        public abstract BaseListSyntax BaseList { get; }

-        public abstract SyntaxToken CloseBraceToken { get; }

-        public abstract SyntaxToken Identifier { get; }

-        public abstract SyntaxTokenList Modifiers { get; }

-        public abstract SyntaxToken OpenBraceToken { get; }

-        public abstract SyntaxToken SemicolonToken { get; }

-    }
-    public abstract class BaseTypeSyntax : CSharpSyntaxNode {
 {
-        public abstract TypeSyntax Type { get; }

-    }
-    public sealed class BinaryExpressionSyntax : ExpressionSyntax {
 {
-        public ExpressionSyntax Left { get; }

-        public SyntaxToken OperatorToken { get; }

-        public ExpressionSyntax Right { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public BinaryExpressionSyntax Update(ExpressionSyntax left, SyntaxToken operatorToken, ExpressionSyntax right);

-        public BinaryExpressionSyntax WithLeft(ExpressionSyntax left);

-        public BinaryExpressionSyntax WithOperatorToken(SyntaxToken operatorToken);

-        public BinaryExpressionSyntax WithRight(ExpressionSyntax right);

-    }
-    public sealed class BlockSyntax : StatementSyntax {
 {
-        public SyntaxToken CloseBraceToken { get; }

-        public SyntaxToken OpenBraceToken { get; }

-        public SyntaxList<StatementSyntax> Statements { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public BlockSyntax AddStatements(params StatementSyntax[] items);

-        public BlockSyntax Update(SyntaxToken openBraceToken, SyntaxList<StatementSyntax> statements, SyntaxToken closeBraceToken);

-        public BlockSyntax WithCloseBraceToken(SyntaxToken closeBraceToken);

-        public BlockSyntax WithOpenBraceToken(SyntaxToken openBraceToken);

-        public BlockSyntax WithStatements(SyntaxList<StatementSyntax> statements);

-    }
-    public sealed class BracketedArgumentListSyntax : BaseArgumentListSyntax {
 {
-        public override SeparatedSyntaxList<ArgumentSyntax> Arguments { get; }

-        public SyntaxToken CloseBracketToken { get; }

-        public SyntaxToken OpenBracketToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public BracketedArgumentListSyntax AddArguments(params ArgumentSyntax[] items);

-        public BracketedArgumentListSyntax Update(SyntaxToken openBracketToken, SeparatedSyntaxList<ArgumentSyntax> arguments, SyntaxToken closeBracketToken);

-        public BracketedArgumentListSyntax WithArguments(SeparatedSyntaxList<ArgumentSyntax> arguments);

-        public BracketedArgumentListSyntax WithCloseBracketToken(SyntaxToken closeBracketToken);

-        public BracketedArgumentListSyntax WithOpenBracketToken(SyntaxToken openBracketToken);

-    }
-    public sealed class BracketedParameterListSyntax : BaseParameterListSyntax {
 {
-        public SyntaxToken CloseBracketToken { get; }

-        public SyntaxToken OpenBracketToken { get; }

-        public override SeparatedSyntaxList<ParameterSyntax> Parameters { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public BracketedParameterListSyntax AddParameters(params ParameterSyntax[] items);

-        public BracketedParameterListSyntax Update(SyntaxToken openBracketToken, SeparatedSyntaxList<ParameterSyntax> parameters, SyntaxToken closeBracketToken);

-        public BracketedParameterListSyntax WithCloseBracketToken(SyntaxToken closeBracketToken);

-        public BracketedParameterListSyntax WithOpenBracketToken(SyntaxToken openBracketToken);

-        public BracketedParameterListSyntax WithParameters(SeparatedSyntaxList<ParameterSyntax> parameters);

-    }
-    public abstract class BranchingDirectiveTriviaSyntax : DirectiveTriviaSyntax {
 {
-        public abstract bool BranchTaken { get; }

-    }
-    public sealed class BreakStatementSyntax : StatementSyntax {
 {
-        public SyntaxToken BreakKeyword { get; }

-        public SyntaxToken SemicolonToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public BreakStatementSyntax Update(SyntaxToken breakKeyword, SyntaxToken semicolonToken);

-        public BreakStatementSyntax WithBreakKeyword(SyntaxToken breakKeyword);

-        public BreakStatementSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-    }
-    public sealed class CasePatternSwitchLabelSyntax : SwitchLabelSyntax {
 {
-        public override SyntaxToken ColonToken { get; }

-        public override SyntaxToken Keyword { get; }

-        public PatternSyntax Pattern { get; }

-        public WhenClauseSyntax WhenClause { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public CasePatternSwitchLabelSyntax Update(SyntaxToken keyword, PatternSyntax pattern, WhenClauseSyntax whenClause, SyntaxToken colonToken);

-        public CasePatternSwitchLabelSyntax WithColonToken(SyntaxToken colonToken);

-        public CasePatternSwitchLabelSyntax WithKeyword(SyntaxToken keyword);

-        public CasePatternSwitchLabelSyntax WithPattern(PatternSyntax pattern);

-        public CasePatternSwitchLabelSyntax WithWhenClause(WhenClauseSyntax whenClause);

-    }
-    public sealed class CaseSwitchLabelSyntax : SwitchLabelSyntax {
 {
-        public override SyntaxToken ColonToken { get; }

-        public override SyntaxToken Keyword { get; }

-        public ExpressionSyntax Value { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public CaseSwitchLabelSyntax Update(SyntaxToken keyword, ExpressionSyntax value, SyntaxToken colonToken);

-        public CaseSwitchLabelSyntax WithColonToken(SyntaxToken colonToken);

-        public CaseSwitchLabelSyntax WithKeyword(SyntaxToken keyword);

-        public CaseSwitchLabelSyntax WithValue(ExpressionSyntax value);

-    }
-    public sealed class CastExpressionSyntax : ExpressionSyntax {
 {
-        public SyntaxToken CloseParenToken { get; }

-        public ExpressionSyntax Expression { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public CastExpressionSyntax Update(SyntaxToken openParenToken, TypeSyntax type, SyntaxToken closeParenToken, ExpressionSyntax expression);

-        public CastExpressionSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public CastExpressionSyntax WithExpression(ExpressionSyntax expression);

-        public CastExpressionSyntax WithOpenParenToken(SyntaxToken openParenToken);

-        public CastExpressionSyntax WithType(TypeSyntax type);

-    }
-    public sealed class CatchClauseSyntax : CSharpSyntaxNode {
 {
-        public BlockSyntax Block { get; }

-        public SyntaxToken CatchKeyword { get; }

-        public CatchDeclarationSyntax Declaration { get; }

-        public CatchFilterClauseSyntax Filter { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public CatchClauseSyntax AddBlockStatements(params StatementSyntax[] items);

-        public CatchClauseSyntax Update(SyntaxToken catchKeyword, CatchDeclarationSyntax declaration, CatchFilterClauseSyntax filter, BlockSyntax block);

-        public CatchClauseSyntax WithBlock(BlockSyntax block);

-        public CatchClauseSyntax WithCatchKeyword(SyntaxToken catchKeyword);

-        public CatchClauseSyntax WithDeclaration(CatchDeclarationSyntax declaration);

-        public CatchClauseSyntax WithFilter(CatchFilterClauseSyntax filter);

-    }
-    public sealed class CatchDeclarationSyntax : CSharpSyntaxNode {
 {
-        public SyntaxToken CloseParenToken { get; }

-        public SyntaxToken Identifier { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public CatchDeclarationSyntax Update(SyntaxToken openParenToken, TypeSyntax type, SyntaxToken identifier, SyntaxToken closeParenToken);

-        public CatchDeclarationSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public CatchDeclarationSyntax WithIdentifier(SyntaxToken identifier);

-        public CatchDeclarationSyntax WithOpenParenToken(SyntaxToken openParenToken);

-        public CatchDeclarationSyntax WithType(TypeSyntax type);

-    }
-    public sealed class CatchFilterClauseSyntax : CSharpSyntaxNode {
 {
-        public SyntaxToken CloseParenToken { get; }

-        public ExpressionSyntax FilterExpression { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public SyntaxToken WhenKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public CatchFilterClauseSyntax Update(SyntaxToken whenKeyword, SyntaxToken openParenToken, ExpressionSyntax filterExpression, SyntaxToken closeParenToken);

-        public CatchFilterClauseSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public CatchFilterClauseSyntax WithFilterExpression(ExpressionSyntax filterExpression);

-        public CatchFilterClauseSyntax WithOpenParenToken(SyntaxToken openParenToken);

-        public CatchFilterClauseSyntax WithWhenKeyword(SyntaxToken whenKeyword);

-    }
-    public sealed class CheckedExpressionSyntax : ExpressionSyntax {
 {
-        public SyntaxToken CloseParenToken { get; }

-        public ExpressionSyntax Expression { get; }

-        public SyntaxToken Keyword { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public CheckedExpressionSyntax Update(SyntaxToken keyword, SyntaxToken openParenToken, ExpressionSyntax expression, SyntaxToken closeParenToken);

-        public CheckedExpressionSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public CheckedExpressionSyntax WithExpression(ExpressionSyntax expression);

-        public CheckedExpressionSyntax WithKeyword(SyntaxToken keyword);

-        public CheckedExpressionSyntax WithOpenParenToken(SyntaxToken openParenToken);

-    }
-    public sealed class CheckedStatementSyntax : StatementSyntax {
 {
-        public BlockSyntax Block { get; }

-        public SyntaxToken Keyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public CheckedStatementSyntax AddBlockStatements(params StatementSyntax[] items);

-        public CheckedStatementSyntax Update(SyntaxToken keyword, BlockSyntax block);

-        public CheckedStatementSyntax WithBlock(BlockSyntax block);

-        public CheckedStatementSyntax WithKeyword(SyntaxToken keyword);

-    }
-    public sealed class ClassDeclarationSyntax : TypeDeclarationSyntax {
 {
-        public override SyntaxList<AttributeListSyntax> AttributeLists { get; }

-        public override BaseListSyntax BaseList { get; }

-        public override SyntaxToken CloseBraceToken { get; }

-        public override SyntaxList<TypeParameterConstraintClauseSyntax> ConstraintClauses { get; }

-        public override SyntaxToken Identifier { get; }

-        public override SyntaxToken Keyword { get; }

-        public override SyntaxList<MemberDeclarationSyntax> Members { get; }

-        public override SyntaxTokenList Modifiers { get; }

-        public override SyntaxToken OpenBraceToken { get; }

-        public override SyntaxToken SemicolonToken { get; }

-        public override TypeParameterListSyntax TypeParameterList { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ClassDeclarationSyntax AddAttributeLists(params AttributeListSyntax[] items);

-        public ClassDeclarationSyntax AddBaseListTypes(params BaseTypeSyntax[] items);

-        public ClassDeclarationSyntax AddConstraintClauses(params TypeParameterConstraintClauseSyntax[] items);

-        public ClassDeclarationSyntax AddMembers(params MemberDeclarationSyntax[] items);

-        public ClassDeclarationSyntax AddModifiers(params SyntaxToken[] items);

-        public ClassDeclarationSyntax AddTypeParameterListParameters(params TypeParameterSyntax[] items);

-        public ClassDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken keyword, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, BaseListSyntax baseList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, SyntaxToken openBraceToken, SyntaxList<MemberDeclarationSyntax> members, SyntaxToken closeBraceToken, SyntaxToken semicolonToken);

-        public ClassDeclarationSyntax WithAttributeLists(SyntaxList<AttributeListSyntax> attributeLists);

-        public ClassDeclarationSyntax WithBaseList(BaseListSyntax baseList);

-        public ClassDeclarationSyntax WithCloseBraceToken(SyntaxToken closeBraceToken);

-        public ClassDeclarationSyntax WithConstraintClauses(SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses);

-        public ClassDeclarationSyntax WithIdentifier(SyntaxToken identifier);

-        public ClassDeclarationSyntax WithKeyword(SyntaxToken keyword);

-        public ClassDeclarationSyntax WithMembers(SyntaxList<MemberDeclarationSyntax> members);

-        public ClassDeclarationSyntax WithModifiers(SyntaxTokenList modifiers);

-        public ClassDeclarationSyntax WithOpenBraceToken(SyntaxToken openBraceToken);

-        public ClassDeclarationSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-        public ClassDeclarationSyntax WithTypeParameterList(TypeParameterListSyntax typeParameterList);

-    }
-    public sealed class ClassOrStructConstraintSyntax : TypeParameterConstraintSyntax {
 {
-        public SyntaxToken ClassOrStructKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ClassOrStructConstraintSyntax Update(SyntaxToken classOrStructKeyword);

-        public ClassOrStructConstraintSyntax WithClassOrStructKeyword(SyntaxToken classOrStructKeyword);

-    }
-    public abstract class CommonForEachStatementSyntax : StatementSyntax {
 {
-        public abstract SyntaxToken CloseParenToken { get; }

-        public abstract ExpressionSyntax Expression { get; }

-        public abstract SyntaxToken ForEachKeyword { get; }

-        public abstract SyntaxToken InKeyword { get; }

-        public abstract SyntaxToken OpenParenToken { get; }

-        public abstract StatementSyntax Statement { get; }

-    }
-    public sealed class CompilationUnitSyntax : CSharpSyntaxNode, ICompilationUnitSyntax {
 {
-        public SyntaxList<AttributeListSyntax> AttributeLists { get; }

-        public SyntaxToken EndOfFileToken { get; }

-        public SyntaxList<ExternAliasDirectiveSyntax> Externs { get; }

-        public SyntaxList<MemberDeclarationSyntax> Members { get; }

-        public SyntaxList<UsingDirectiveSyntax> Usings { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public CompilationUnitSyntax AddAttributeLists(params AttributeListSyntax[] items);

-        public CompilationUnitSyntax AddExterns(params ExternAliasDirectiveSyntax[] items);

-        public CompilationUnitSyntax AddMembers(params MemberDeclarationSyntax[] items);

-        public CompilationUnitSyntax AddUsings(params UsingDirectiveSyntax[] items);

-        public IList<LoadDirectiveTriviaSyntax> GetLoadDirectives();

-        public IList<ReferenceDirectiveTriviaSyntax> GetReferenceDirectives();

-        public CompilationUnitSyntax Update(SyntaxList<ExternAliasDirectiveSyntax> externs, SyntaxList<UsingDirectiveSyntax> usings, SyntaxList<AttributeListSyntax> attributeLists, SyntaxList<MemberDeclarationSyntax> members, SyntaxToken endOfFileToken);

-        public CompilationUnitSyntax WithAttributeLists(SyntaxList<AttributeListSyntax> attributeLists);

-        public CompilationUnitSyntax WithEndOfFileToken(SyntaxToken endOfFileToken);

-        public CompilationUnitSyntax WithExterns(SyntaxList<ExternAliasDirectiveSyntax> externs);

-        public CompilationUnitSyntax WithMembers(SyntaxList<MemberDeclarationSyntax> members);

-        public CompilationUnitSyntax WithUsings(SyntaxList<UsingDirectiveSyntax> usings);

-    }
-    public sealed class ConditionalAccessExpressionSyntax : ExpressionSyntax {
 {
-        public ExpressionSyntax Expression { get; }

-        public SyntaxToken OperatorToken { get; }

-        public ExpressionSyntax WhenNotNull { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ConditionalAccessExpressionSyntax Update(ExpressionSyntax expression, SyntaxToken operatorToken, ExpressionSyntax whenNotNull);

-        public ConditionalAccessExpressionSyntax WithExpression(ExpressionSyntax expression);

-        public ConditionalAccessExpressionSyntax WithOperatorToken(SyntaxToken operatorToken);

-        public ConditionalAccessExpressionSyntax WithWhenNotNull(ExpressionSyntax whenNotNull);

-    }
-    public abstract class ConditionalDirectiveTriviaSyntax : BranchingDirectiveTriviaSyntax {
 {
-        public abstract ExpressionSyntax Condition { get; }

-        public abstract bool ConditionValue { get; }

-    }
-    public sealed class ConditionalExpressionSyntax : ExpressionSyntax {
 {
-        public SyntaxToken ColonToken { get; }

-        public ExpressionSyntax Condition { get; }

-        public SyntaxToken QuestionToken { get; }

-        public ExpressionSyntax WhenFalse { get; }

-        public ExpressionSyntax WhenTrue { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ConditionalExpressionSyntax Update(ExpressionSyntax condition, SyntaxToken questionToken, ExpressionSyntax whenTrue, SyntaxToken colonToken, ExpressionSyntax whenFalse);

-        public ConditionalExpressionSyntax WithColonToken(SyntaxToken colonToken);

-        public ConditionalExpressionSyntax WithCondition(ExpressionSyntax condition);

-        public ConditionalExpressionSyntax WithQuestionToken(SyntaxToken questionToken);

-        public ConditionalExpressionSyntax WithWhenFalse(ExpressionSyntax whenFalse);

-        public ConditionalExpressionSyntax WithWhenTrue(ExpressionSyntax whenTrue);

-    }
-    public sealed class ConstantPatternSyntax : PatternSyntax {
 {
-        public ExpressionSyntax Expression { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ConstantPatternSyntax Update(ExpressionSyntax expression);

-        public ConstantPatternSyntax WithExpression(ExpressionSyntax expression);

-    }
-    public sealed class ConstructorConstraintSyntax : TypeParameterConstraintSyntax {
 {
-        public SyntaxToken CloseParenToken { get; }

-        public SyntaxToken NewKeyword { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ConstructorConstraintSyntax Update(SyntaxToken newKeyword, SyntaxToken openParenToken, SyntaxToken closeParenToken);

-        public ConstructorConstraintSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public ConstructorConstraintSyntax WithNewKeyword(SyntaxToken newKeyword);

-        public ConstructorConstraintSyntax WithOpenParenToken(SyntaxToken openParenToken);

-    }
-    public sealed class ConstructorDeclarationSyntax : BaseMethodDeclarationSyntax {
 {
-        public override SyntaxList<AttributeListSyntax> AttributeLists { get; }

-        public override BlockSyntax Body { get; }

-        public override ArrowExpressionClauseSyntax ExpressionBody { get; }

-        public SyntaxToken Identifier { get; }

-        public ConstructorInitializerSyntax Initializer { get; }

-        public override SyntaxTokenList Modifiers { get; }

-        public override ParameterListSyntax ParameterList { get; }

-        public override SyntaxToken SemicolonToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ConstructorDeclarationSyntax AddAttributeLists(params AttributeListSyntax[] items);

-        public ConstructorDeclarationSyntax AddBodyStatements(params StatementSyntax[] items);

-        public ConstructorDeclarationSyntax AddModifiers(params SyntaxToken[] items);

-        public ConstructorDeclarationSyntax AddParameterListParameters(params ParameterSyntax[] items);

-        public ConstructorDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken identifier, ParameterListSyntax parameterList, ConstructorInitializerSyntax initializer, BlockSyntax body, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolonToken);

-        public ConstructorDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken identifier, ParameterListSyntax parameterList, ConstructorInitializerSyntax initializer, BlockSyntax body, SyntaxToken semicolonToken);

-        public ConstructorDeclarationSyntax WithAttributeLists(SyntaxList<AttributeListSyntax> attributeLists);

-        public ConstructorDeclarationSyntax WithBody(BlockSyntax body);

-        public ConstructorDeclarationSyntax WithExpressionBody(ArrowExpressionClauseSyntax expressionBody);

-        public ConstructorDeclarationSyntax WithIdentifier(SyntaxToken identifier);

-        public ConstructorDeclarationSyntax WithInitializer(ConstructorInitializerSyntax initializer);

-        public ConstructorDeclarationSyntax WithModifiers(SyntaxTokenList modifiers);

-        public ConstructorDeclarationSyntax WithParameterList(ParameterListSyntax parameterList);

-        public ConstructorDeclarationSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-    }
-    public sealed class ConstructorInitializerSyntax : CSharpSyntaxNode {
 {
-        public ArgumentListSyntax ArgumentList { get; }

-        public SyntaxToken ColonToken { get; }

-        public SyntaxToken ThisOrBaseKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ConstructorInitializerSyntax AddArgumentListArguments(params ArgumentSyntax[] items);

-        public ConstructorInitializerSyntax Update(SyntaxToken colonToken, SyntaxToken thisOrBaseKeyword, ArgumentListSyntax argumentList);

-        public ConstructorInitializerSyntax WithArgumentList(ArgumentListSyntax argumentList);

-        public ConstructorInitializerSyntax WithColonToken(SyntaxToken colonToken);

-        public ConstructorInitializerSyntax WithThisOrBaseKeyword(SyntaxToken thisOrBaseKeyword);

-    }
-    public sealed class ContinueStatementSyntax : StatementSyntax {
 {
-        public SyntaxToken ContinueKeyword { get; }

-        public SyntaxToken SemicolonToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ContinueStatementSyntax Update(SyntaxToken continueKeyword, SyntaxToken semicolonToken);

-        public ContinueStatementSyntax WithContinueKeyword(SyntaxToken continueKeyword);

-        public ContinueStatementSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-    }
-    public sealed class ConversionOperatorDeclarationSyntax : BaseMethodDeclarationSyntax {
 {
-        public override SyntaxList<AttributeListSyntax> AttributeLists { get; }

-        public override BlockSyntax Body { get; }

-        public override ArrowExpressionClauseSyntax ExpressionBody { get; }

-        public SyntaxToken ImplicitOrExplicitKeyword { get; }

-        public override SyntaxTokenList Modifiers { get; }

-        public SyntaxToken OperatorKeyword { get; }

-        public override ParameterListSyntax ParameterList { get; }

-        public override SyntaxToken SemicolonToken { get; }

-        public TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ConversionOperatorDeclarationSyntax AddAttributeLists(params AttributeListSyntax[] items);

-        public ConversionOperatorDeclarationSyntax AddBodyStatements(params StatementSyntax[] items);

-        public ConversionOperatorDeclarationSyntax AddModifiers(params SyntaxToken[] items);

-        public ConversionOperatorDeclarationSyntax AddParameterListParameters(params ParameterSyntax[] items);

-        public ConversionOperatorDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken implicitOrExplicitKeyword, SyntaxToken operatorKeyword, TypeSyntax type, ParameterListSyntax parameterList, BlockSyntax body, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolonToken);

-        public ConversionOperatorDeclarationSyntax WithAttributeLists(SyntaxList<AttributeListSyntax> attributeLists);

-        public ConversionOperatorDeclarationSyntax WithBody(BlockSyntax body);

-        public ConversionOperatorDeclarationSyntax WithExpressionBody(ArrowExpressionClauseSyntax expressionBody);

-        public ConversionOperatorDeclarationSyntax WithImplicitOrExplicitKeyword(SyntaxToken implicitOrExplicitKeyword);

-        public ConversionOperatorDeclarationSyntax WithModifiers(SyntaxTokenList modifiers);

-        public ConversionOperatorDeclarationSyntax WithOperatorKeyword(SyntaxToken operatorKeyword);

-        public ConversionOperatorDeclarationSyntax WithParameterList(ParameterListSyntax parameterList);

-        public ConversionOperatorDeclarationSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-        public ConversionOperatorDeclarationSyntax WithType(TypeSyntax type);

-    }
-    public sealed class ConversionOperatorMemberCrefSyntax : MemberCrefSyntax {
 {
-        public SyntaxToken ImplicitOrExplicitKeyword { get; }

-        public SyntaxToken OperatorKeyword { get; }

-        public CrefParameterListSyntax Parameters { get; }

-        public TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ConversionOperatorMemberCrefSyntax AddParametersParameters(params CrefParameterSyntax[] items);

-        public ConversionOperatorMemberCrefSyntax Update(SyntaxToken implicitOrExplicitKeyword, SyntaxToken operatorKeyword, TypeSyntax type, CrefParameterListSyntax parameters);

-        public ConversionOperatorMemberCrefSyntax WithImplicitOrExplicitKeyword(SyntaxToken implicitOrExplicitKeyword);

-        public ConversionOperatorMemberCrefSyntax WithOperatorKeyword(SyntaxToken operatorKeyword);

-        public ConversionOperatorMemberCrefSyntax WithParameters(CrefParameterListSyntax parameters);

-        public ConversionOperatorMemberCrefSyntax WithType(TypeSyntax type);

-    }
-    public sealed class CrefBracketedParameterListSyntax : BaseCrefParameterListSyntax {
 {
-        public SyntaxToken CloseBracketToken { get; }

-        public SyntaxToken OpenBracketToken { get; }

-        public override SeparatedSyntaxList<CrefParameterSyntax> Parameters { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public CrefBracketedParameterListSyntax AddParameters(params CrefParameterSyntax[] items);

-        public CrefBracketedParameterListSyntax Update(SyntaxToken openBracketToken, SeparatedSyntaxList<CrefParameterSyntax> parameters, SyntaxToken closeBracketToken);

-        public CrefBracketedParameterListSyntax WithCloseBracketToken(SyntaxToken closeBracketToken);

-        public CrefBracketedParameterListSyntax WithOpenBracketToken(SyntaxToken openBracketToken);

-        public CrefBracketedParameterListSyntax WithParameters(SeparatedSyntaxList<CrefParameterSyntax> parameters);

-    }
-    public sealed class CrefParameterListSyntax : BaseCrefParameterListSyntax {
 {
-        public SyntaxToken CloseParenToken { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public override SeparatedSyntaxList<CrefParameterSyntax> Parameters { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public CrefParameterListSyntax AddParameters(params CrefParameterSyntax[] items);

-        public CrefParameterListSyntax Update(SyntaxToken openParenToken, SeparatedSyntaxList<CrefParameterSyntax> parameters, SyntaxToken closeParenToken);

-        public CrefParameterListSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public CrefParameterListSyntax WithOpenParenToken(SyntaxToken openParenToken);

-        public CrefParameterListSyntax WithParameters(SeparatedSyntaxList<CrefParameterSyntax> parameters);

-    }
-    public sealed class CrefParameterSyntax : CSharpSyntaxNode {
 {
-        public SyntaxToken RefKindKeyword { get; }

-        public SyntaxToken RefOrOutKeyword { get; }

-        public TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public CrefParameterSyntax Update(SyntaxToken refKindKeyword, TypeSyntax type);

-        public CrefParameterSyntax WithRefKindKeyword(SyntaxToken refKindKeyword);

-        public CrefParameterSyntax WithRefOrOutKeyword(SyntaxToken refOrOutKeyword);

-        public CrefParameterSyntax WithType(TypeSyntax type);

-    }
-    public abstract class CrefSyntax : CSharpSyntaxNode

-    public sealed class DeclarationExpressionSyntax : ExpressionSyntax {
 {
-        public VariableDesignationSyntax Designation { get; }

-        public TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public DeclarationExpressionSyntax Update(TypeSyntax type, VariableDesignationSyntax designation);

-        public DeclarationExpressionSyntax WithDesignation(VariableDesignationSyntax designation);

-        public DeclarationExpressionSyntax WithType(TypeSyntax type);

-    }
-    public sealed class DeclarationPatternSyntax : PatternSyntax {
 {
-        public VariableDesignationSyntax Designation { get; }

-        public TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public DeclarationPatternSyntax Update(TypeSyntax type, VariableDesignationSyntax designation);

-        public DeclarationPatternSyntax WithDesignation(VariableDesignationSyntax designation);

-        public DeclarationPatternSyntax WithType(TypeSyntax type);

-    }
-    public sealed class DefaultExpressionSyntax : ExpressionSyntax {
 {
-        public SyntaxToken CloseParenToken { get; }

-        public SyntaxToken Keyword { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public DefaultExpressionSyntax Update(SyntaxToken keyword, SyntaxToken openParenToken, TypeSyntax type, SyntaxToken closeParenToken);

-        public DefaultExpressionSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public DefaultExpressionSyntax WithKeyword(SyntaxToken keyword);

-        public DefaultExpressionSyntax WithOpenParenToken(SyntaxToken openParenToken);

-        public DefaultExpressionSyntax WithType(TypeSyntax type);

-    }
-    public sealed class DefaultSwitchLabelSyntax : SwitchLabelSyntax {
 {
-        public override SyntaxToken ColonToken { get; }

-        public override SyntaxToken Keyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public DefaultSwitchLabelSyntax Update(SyntaxToken keyword, SyntaxToken colonToken);

-        public DefaultSwitchLabelSyntax WithColonToken(SyntaxToken colonToken);

-        public DefaultSwitchLabelSyntax WithKeyword(SyntaxToken keyword);

-    }
-    public sealed class DefineDirectiveTriviaSyntax : DirectiveTriviaSyntax {
 {
-        public SyntaxToken DefineKeyword { get; }

-        public override SyntaxToken EndOfDirectiveToken { get; }

-        public override SyntaxToken HashToken { get; }

-        public override bool IsActive { get; }

-        public SyntaxToken Name { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public DefineDirectiveTriviaSyntax Update(SyntaxToken hashToken, SyntaxToken defineKeyword, SyntaxToken name, SyntaxToken endOfDirectiveToken, bool isActive);

-        public DefineDirectiveTriviaSyntax WithDefineKeyword(SyntaxToken defineKeyword);

-        public DefineDirectiveTriviaSyntax WithEndOfDirectiveToken(SyntaxToken endOfDirectiveToken);

-        public DefineDirectiveTriviaSyntax WithHashToken(SyntaxToken hashToken);

-        public DefineDirectiveTriviaSyntax WithIsActive(bool isActive);

-        public DefineDirectiveTriviaSyntax WithName(SyntaxToken name);

-    }
-    public sealed class DelegateDeclarationSyntax : MemberDeclarationSyntax {
 {
-        public int Arity { get; }

-        public SyntaxList<AttributeListSyntax> AttributeLists { get; }

-        public SyntaxList<TypeParameterConstraintClauseSyntax> ConstraintClauses { get; }

-        public SyntaxToken DelegateKeyword { get; }

-        public SyntaxToken Identifier { get; }

-        public SyntaxTokenList Modifiers { get; }

-        public ParameterListSyntax ParameterList { get; }

-        public TypeSyntax ReturnType { get; }

-        public SyntaxToken SemicolonToken { get; }

-        public TypeParameterListSyntax TypeParameterList { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public DelegateDeclarationSyntax AddAttributeLists(params AttributeListSyntax[] items);

-        public DelegateDeclarationSyntax AddConstraintClauses(params TypeParameterConstraintClauseSyntax[] items);

-        public DelegateDeclarationSyntax AddModifiers(params SyntaxToken[] items);

-        public DelegateDeclarationSyntax AddParameterListParameters(params ParameterSyntax[] items);

-        public DelegateDeclarationSyntax AddTypeParameterListParameters(params TypeParameterSyntax[] items);

-        public DelegateDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken delegateKeyword, TypeSyntax returnType, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, ParameterListSyntax parameterList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, SyntaxToken semicolonToken);

-        public DelegateDeclarationSyntax WithAttributeLists(SyntaxList<AttributeListSyntax> attributeLists);

-        public DelegateDeclarationSyntax WithConstraintClauses(SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses);

-        public DelegateDeclarationSyntax WithDelegateKeyword(SyntaxToken delegateKeyword);

-        public DelegateDeclarationSyntax WithIdentifier(SyntaxToken identifier);

-        public DelegateDeclarationSyntax WithModifiers(SyntaxTokenList modifiers);

-        public DelegateDeclarationSyntax WithParameterList(ParameterListSyntax parameterList);

-        public DelegateDeclarationSyntax WithReturnType(TypeSyntax returnType);

-        public DelegateDeclarationSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-        public DelegateDeclarationSyntax WithTypeParameterList(TypeParameterListSyntax typeParameterList);

-    }
-    public sealed class DestructorDeclarationSyntax : BaseMethodDeclarationSyntax {
 {
-        public override SyntaxList<AttributeListSyntax> AttributeLists { get; }

-        public override BlockSyntax Body { get; }

-        public override ArrowExpressionClauseSyntax ExpressionBody { get; }

-        public SyntaxToken Identifier { get; }

-        public override SyntaxTokenList Modifiers { get; }

-        public override ParameterListSyntax ParameterList { get; }

-        public override SyntaxToken SemicolonToken { get; }

-        public SyntaxToken TildeToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public DestructorDeclarationSyntax AddAttributeLists(params AttributeListSyntax[] items);

-        public DestructorDeclarationSyntax AddBodyStatements(params StatementSyntax[] items);

-        public DestructorDeclarationSyntax AddModifiers(params SyntaxToken[] items);

-        public DestructorDeclarationSyntax AddParameterListParameters(params ParameterSyntax[] items);

-        public DestructorDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken tildeToken, SyntaxToken identifier, ParameterListSyntax parameterList, BlockSyntax body, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolonToken);

-        public DestructorDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken tildeToken, SyntaxToken identifier, ParameterListSyntax parameterList, BlockSyntax body, SyntaxToken semicolonToken);

-        public DestructorDeclarationSyntax WithAttributeLists(SyntaxList<AttributeListSyntax> attributeLists);

-        public DestructorDeclarationSyntax WithBody(BlockSyntax body);

-        public DestructorDeclarationSyntax WithExpressionBody(ArrowExpressionClauseSyntax expressionBody);

-        public DestructorDeclarationSyntax WithIdentifier(SyntaxToken identifier);

-        public DestructorDeclarationSyntax WithModifiers(SyntaxTokenList modifiers);

-        public DestructorDeclarationSyntax WithParameterList(ParameterListSyntax parameterList);

-        public DestructorDeclarationSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-        public DestructorDeclarationSyntax WithTildeToken(SyntaxToken tildeToken);

-    }
-    public abstract class DirectiveTriviaSyntax : StructuredTriviaSyntax {
 {
-        public SyntaxToken DirectiveNameToken { get; }

-        public abstract SyntaxToken EndOfDirectiveToken { get; }

-        public abstract SyntaxToken HashToken { get; }

-        public abstract bool IsActive { get; }

-        public DirectiveTriviaSyntax GetNextDirective(Func<DirectiveTriviaSyntax, bool> predicate = null);

-        public DirectiveTriviaSyntax GetPreviousDirective(Func<DirectiveTriviaSyntax, bool> predicate = null);

-        public List<DirectiveTriviaSyntax> GetRelatedDirectives();

-    }
-    public sealed class DiscardDesignationSyntax : VariableDesignationSyntax {
 {
-        public SyntaxToken UnderscoreToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public DiscardDesignationSyntax Update(SyntaxToken underscoreToken);

-        public DiscardDesignationSyntax WithUnderscoreToken(SyntaxToken underscoreToken);

-    }
-    public sealed class DocumentationCommentTriviaSyntax : StructuredTriviaSyntax {
 {
-        public SyntaxList<XmlNodeSyntax> Content { get; }

-        public SyntaxToken EndOfComment { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public DocumentationCommentTriviaSyntax AddContent(params XmlNodeSyntax[] items);

-        public DocumentationCommentTriviaSyntax Update(SyntaxList<XmlNodeSyntax> content, SyntaxToken endOfComment);

-        public DocumentationCommentTriviaSyntax WithContent(SyntaxList<XmlNodeSyntax> content);

-        public DocumentationCommentTriviaSyntax WithEndOfComment(SyntaxToken endOfComment);

-    }
-    public sealed class DoStatementSyntax : StatementSyntax {
 {
-        public SyntaxToken CloseParenToken { get; }

-        public ExpressionSyntax Condition { get; }

-        public SyntaxToken DoKeyword { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public SyntaxToken SemicolonToken { get; }

-        public StatementSyntax Statement { get; }

-        public SyntaxToken WhileKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public DoStatementSyntax Update(SyntaxToken doKeyword, StatementSyntax statement, SyntaxToken whileKeyword, SyntaxToken openParenToken, ExpressionSyntax condition, SyntaxToken closeParenToken, SyntaxToken semicolonToken);

-        public DoStatementSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public DoStatementSyntax WithCondition(ExpressionSyntax condition);

-        public DoStatementSyntax WithDoKeyword(SyntaxToken doKeyword);

-        public DoStatementSyntax WithOpenParenToken(SyntaxToken openParenToken);

-        public DoStatementSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-        public DoStatementSyntax WithStatement(StatementSyntax statement);

-        public DoStatementSyntax WithWhileKeyword(SyntaxToken whileKeyword);

-    }
-    public sealed class ElementAccessExpressionSyntax : ExpressionSyntax {
 {
-        public BracketedArgumentListSyntax ArgumentList { get; }

-        public ExpressionSyntax Expression { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ElementAccessExpressionSyntax AddArgumentListArguments(params ArgumentSyntax[] items);

-        public ElementAccessExpressionSyntax Update(ExpressionSyntax expression, BracketedArgumentListSyntax argumentList);

-        public ElementAccessExpressionSyntax WithArgumentList(BracketedArgumentListSyntax argumentList);

-        public ElementAccessExpressionSyntax WithExpression(ExpressionSyntax expression);

-    }
-    public sealed class ElementBindingExpressionSyntax : ExpressionSyntax {
 {
-        public BracketedArgumentListSyntax ArgumentList { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ElementBindingExpressionSyntax AddArgumentListArguments(params ArgumentSyntax[] items);

-        public ElementBindingExpressionSyntax Update(BracketedArgumentListSyntax argumentList);

-        public ElementBindingExpressionSyntax WithArgumentList(BracketedArgumentListSyntax argumentList);

-    }
-    public sealed class ElifDirectiveTriviaSyntax : ConditionalDirectiveTriviaSyntax {
 {
-        public override bool BranchTaken { get; }

-        public override ExpressionSyntax Condition { get; }

-        public override bool ConditionValue { get; }

-        public SyntaxToken ElifKeyword { get; }

-        public override SyntaxToken EndOfDirectiveToken { get; }

-        public override SyntaxToken HashToken { get; }

-        public override bool IsActive { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ElifDirectiveTriviaSyntax Update(SyntaxToken hashToken, SyntaxToken elifKeyword, ExpressionSyntax condition, SyntaxToken endOfDirectiveToken, bool isActive, bool branchTaken, bool conditionValue);

-        public ElifDirectiveTriviaSyntax WithBranchTaken(bool branchTaken);

-        public ElifDirectiveTriviaSyntax WithCondition(ExpressionSyntax condition);

-        public ElifDirectiveTriviaSyntax WithConditionValue(bool conditionValue);

-        public ElifDirectiveTriviaSyntax WithElifKeyword(SyntaxToken elifKeyword);

-        public ElifDirectiveTriviaSyntax WithEndOfDirectiveToken(SyntaxToken endOfDirectiveToken);

-        public ElifDirectiveTriviaSyntax WithHashToken(SyntaxToken hashToken);

-        public ElifDirectiveTriviaSyntax WithIsActive(bool isActive);

-    }
-    public sealed class ElseClauseSyntax : CSharpSyntaxNode {
 {
-        public SyntaxToken ElseKeyword { get; }

-        public StatementSyntax Statement { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ElseClauseSyntax Update(SyntaxToken elseKeyword, StatementSyntax statement);

-        public ElseClauseSyntax WithElseKeyword(SyntaxToken elseKeyword);

-        public ElseClauseSyntax WithStatement(StatementSyntax statement);

-    }
-    public sealed class ElseDirectiveTriviaSyntax : BranchingDirectiveTriviaSyntax {
 {
-        public override bool BranchTaken { get; }

-        public SyntaxToken ElseKeyword { get; }

-        public override SyntaxToken EndOfDirectiveToken { get; }

-        public override SyntaxToken HashToken { get; }

-        public override bool IsActive { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ElseDirectiveTriviaSyntax Update(SyntaxToken hashToken, SyntaxToken elseKeyword, SyntaxToken endOfDirectiveToken, bool isActive, bool branchTaken);

-        public ElseDirectiveTriviaSyntax WithBranchTaken(bool branchTaken);

-        public ElseDirectiveTriviaSyntax WithElseKeyword(SyntaxToken elseKeyword);

-        public ElseDirectiveTriviaSyntax WithEndOfDirectiveToken(SyntaxToken endOfDirectiveToken);

-        public ElseDirectiveTriviaSyntax WithHashToken(SyntaxToken hashToken);

-        public ElseDirectiveTriviaSyntax WithIsActive(bool isActive);

-    }
-    public sealed class EmptyStatementSyntax : StatementSyntax {
 {
-        public SyntaxToken SemicolonToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public EmptyStatementSyntax Update(SyntaxToken semicolonToken);

-        public EmptyStatementSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-    }
-    public sealed class EndIfDirectiveTriviaSyntax : DirectiveTriviaSyntax {
 {
-        public SyntaxToken EndIfKeyword { get; }

-        public override SyntaxToken EndOfDirectiveToken { get; }

-        public override SyntaxToken HashToken { get; }

-        public override bool IsActive { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public EndIfDirectiveTriviaSyntax Update(SyntaxToken hashToken, SyntaxToken endIfKeyword, SyntaxToken endOfDirectiveToken, bool isActive);

-        public EndIfDirectiveTriviaSyntax WithEndIfKeyword(SyntaxToken endIfKeyword);

-        public EndIfDirectiveTriviaSyntax WithEndOfDirectiveToken(SyntaxToken endOfDirectiveToken);

-        public EndIfDirectiveTriviaSyntax WithHashToken(SyntaxToken hashToken);

-        public EndIfDirectiveTriviaSyntax WithIsActive(bool isActive);

-    }
-    public sealed class EndRegionDirectiveTriviaSyntax : DirectiveTriviaSyntax {
 {
-        public override SyntaxToken EndOfDirectiveToken { get; }

-        public SyntaxToken EndRegionKeyword { get; }

-        public override SyntaxToken HashToken { get; }

-        public override bool IsActive { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public EndRegionDirectiveTriviaSyntax Update(SyntaxToken hashToken, SyntaxToken endRegionKeyword, SyntaxToken endOfDirectiveToken, bool isActive);

-        public EndRegionDirectiveTriviaSyntax WithEndOfDirectiveToken(SyntaxToken endOfDirectiveToken);

-        public EndRegionDirectiveTriviaSyntax WithEndRegionKeyword(SyntaxToken endRegionKeyword);

-        public EndRegionDirectiveTriviaSyntax WithHashToken(SyntaxToken hashToken);

-        public EndRegionDirectiveTriviaSyntax WithIsActive(bool isActive);

-    }
-    public sealed class EnumDeclarationSyntax : BaseTypeDeclarationSyntax {
 {
-        public override SyntaxList<AttributeListSyntax> AttributeLists { get; }

-        public override BaseListSyntax BaseList { get; }

-        public override SyntaxToken CloseBraceToken { get; }

-        public SyntaxToken EnumKeyword { get; }

-        public override SyntaxToken Identifier { get; }

-        public SeparatedSyntaxList<EnumMemberDeclarationSyntax> Members { get; }

-        public override SyntaxTokenList Modifiers { get; }

-        public override SyntaxToken OpenBraceToken { get; }

-        public override SyntaxToken SemicolonToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public EnumDeclarationSyntax AddAttributeLists(params AttributeListSyntax[] items);

-        public EnumDeclarationSyntax AddBaseListTypes(params BaseTypeSyntax[] items);

-        public EnumDeclarationSyntax AddMembers(params EnumMemberDeclarationSyntax[] items);

-        public EnumDeclarationSyntax AddModifiers(params SyntaxToken[] items);

-        public EnumDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken enumKeyword, SyntaxToken identifier, BaseListSyntax baseList, SyntaxToken openBraceToken, SeparatedSyntaxList<EnumMemberDeclarationSyntax> members, SyntaxToken closeBraceToken, SyntaxToken semicolonToken);

-        public EnumDeclarationSyntax WithAttributeLists(SyntaxList<AttributeListSyntax> attributeLists);

-        public EnumDeclarationSyntax WithBaseList(BaseListSyntax baseList);

-        public EnumDeclarationSyntax WithCloseBraceToken(SyntaxToken closeBraceToken);

-        public EnumDeclarationSyntax WithEnumKeyword(SyntaxToken enumKeyword);

-        public EnumDeclarationSyntax WithIdentifier(SyntaxToken identifier);

-        public EnumDeclarationSyntax WithMembers(SeparatedSyntaxList<EnumMemberDeclarationSyntax> members);

-        public EnumDeclarationSyntax WithModifiers(SyntaxTokenList modifiers);

-        public EnumDeclarationSyntax WithOpenBraceToken(SyntaxToken openBraceToken);

-        public EnumDeclarationSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-    }
-    public sealed class EnumMemberDeclarationSyntax : MemberDeclarationSyntax {
 {
-        public SyntaxList<AttributeListSyntax> AttributeLists { get; }

-        public EqualsValueClauseSyntax EqualsValue { get; }

-        public SyntaxToken Identifier { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public EnumMemberDeclarationSyntax AddAttributeLists(params AttributeListSyntax[] items);

-        public EnumMemberDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxToken identifier, EqualsValueClauseSyntax equalsValue);

-        public EnumMemberDeclarationSyntax WithAttributeLists(SyntaxList<AttributeListSyntax> attributeLists);

-        public EnumMemberDeclarationSyntax WithEqualsValue(EqualsValueClauseSyntax equalsValue);

-        public EnumMemberDeclarationSyntax WithIdentifier(SyntaxToken identifier);

-    }
-    public sealed class EqualsValueClauseSyntax : CSharpSyntaxNode {
 {
-        public SyntaxToken EqualsToken { get; }

-        public ExpressionSyntax Value { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public EqualsValueClauseSyntax Update(SyntaxToken equalsToken, ExpressionSyntax value);

-        public EqualsValueClauseSyntax WithEqualsToken(SyntaxToken equalsToken);

-        public EqualsValueClauseSyntax WithValue(ExpressionSyntax value);

-    }
-    public sealed class ErrorDirectiveTriviaSyntax : DirectiveTriviaSyntax {
 {
-        public override SyntaxToken EndOfDirectiveToken { get; }

-        public SyntaxToken ErrorKeyword { get; }

-        public override SyntaxToken HashToken { get; }

-        public override bool IsActive { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ErrorDirectiveTriviaSyntax Update(SyntaxToken hashToken, SyntaxToken errorKeyword, SyntaxToken endOfDirectiveToken, bool isActive);

-        public ErrorDirectiveTriviaSyntax WithEndOfDirectiveToken(SyntaxToken endOfDirectiveToken);

-        public ErrorDirectiveTriviaSyntax WithErrorKeyword(SyntaxToken errorKeyword);

-        public ErrorDirectiveTriviaSyntax WithHashToken(SyntaxToken hashToken);

-        public ErrorDirectiveTriviaSyntax WithIsActive(bool isActive);

-    }
-    public sealed class EventDeclarationSyntax : BasePropertyDeclarationSyntax {
 {
-        public override AccessorListSyntax AccessorList { get; }

-        public override SyntaxList<AttributeListSyntax> AttributeLists { get; }

-        public SyntaxToken EventKeyword { get; }

-        public override ExplicitInterfaceSpecifierSyntax ExplicitInterfaceSpecifier { get; }

-        public SyntaxToken Identifier { get; }

-        public override SyntaxTokenList Modifiers { get; }

-        public override TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public EventDeclarationSyntax AddAccessorListAccessors(params AccessorDeclarationSyntax[] items);

-        public EventDeclarationSyntax AddAttributeLists(params AttributeListSyntax[] items);

-        public EventDeclarationSyntax AddModifiers(params SyntaxToken[] items);

-        public EventDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken eventKeyword, TypeSyntax type, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken identifier, AccessorListSyntax accessorList);

-        public EventDeclarationSyntax WithAccessorList(AccessorListSyntax accessorList);

-        public EventDeclarationSyntax WithAttributeLists(SyntaxList<AttributeListSyntax> attributeLists);

-        public EventDeclarationSyntax WithEventKeyword(SyntaxToken eventKeyword);

-        public EventDeclarationSyntax WithExplicitInterfaceSpecifier(ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier);

-        public EventDeclarationSyntax WithIdentifier(SyntaxToken identifier);

-        public EventDeclarationSyntax WithModifiers(SyntaxTokenList modifiers);

-        public EventDeclarationSyntax WithType(TypeSyntax type);

-    }
-    public sealed class EventFieldDeclarationSyntax : BaseFieldDeclarationSyntax {
 {
-        public override SyntaxList<AttributeListSyntax> AttributeLists { get; }

-        public override VariableDeclarationSyntax Declaration { get; }

-        public SyntaxToken EventKeyword { get; }

-        public override SyntaxTokenList Modifiers { get; }

-        public override SyntaxToken SemicolonToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public EventFieldDeclarationSyntax AddAttributeLists(params AttributeListSyntax[] items);

-        public EventFieldDeclarationSyntax AddDeclarationVariables(params VariableDeclaratorSyntax[] items);

-        public EventFieldDeclarationSyntax AddModifiers(params SyntaxToken[] items);

-        public EventFieldDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken eventKeyword, VariableDeclarationSyntax declaration, SyntaxToken semicolonToken);

-        public EventFieldDeclarationSyntax WithAttributeLists(SyntaxList<AttributeListSyntax> attributeLists);

-        public EventFieldDeclarationSyntax WithDeclaration(VariableDeclarationSyntax declaration);

-        public EventFieldDeclarationSyntax WithEventKeyword(SyntaxToken eventKeyword);

-        public EventFieldDeclarationSyntax WithModifiers(SyntaxTokenList modifiers);

-        public EventFieldDeclarationSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-    }
-    public sealed class ExplicitInterfaceSpecifierSyntax : CSharpSyntaxNode {
 {
-        public SyntaxToken DotToken { get; }

-        public NameSyntax Name { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ExplicitInterfaceSpecifierSyntax Update(NameSyntax name, SyntaxToken dotToken);

-        public ExplicitInterfaceSpecifierSyntax WithDotToken(SyntaxToken dotToken);

-        public ExplicitInterfaceSpecifierSyntax WithName(NameSyntax name);

-    }
-    public sealed class ExpressionStatementSyntax : StatementSyntax {
 {
-        public bool AllowsAnyExpression { get; }

-        public ExpressionSyntax Expression { get; }

-        public SyntaxToken SemicolonToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ExpressionStatementSyntax Update(ExpressionSyntax expression, SyntaxToken semicolonToken);

-        public ExpressionStatementSyntax WithExpression(ExpressionSyntax expression);

-        public ExpressionStatementSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-    }
-    public abstract class ExpressionSyntax : CSharpSyntaxNode

-    public sealed class ExternAliasDirectiveSyntax : CSharpSyntaxNode {
 {
-        public SyntaxToken AliasKeyword { get; }

-        public SyntaxToken ExternKeyword { get; }

-        public SyntaxToken Identifier { get; }

-        public SyntaxToken SemicolonToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ExternAliasDirectiveSyntax Update(SyntaxToken externKeyword, SyntaxToken aliasKeyword, SyntaxToken identifier, SyntaxToken semicolonToken);

-        public ExternAliasDirectiveSyntax WithAliasKeyword(SyntaxToken aliasKeyword);

-        public ExternAliasDirectiveSyntax WithExternKeyword(SyntaxToken externKeyword);

-        public ExternAliasDirectiveSyntax WithIdentifier(SyntaxToken identifier);

-        public ExternAliasDirectiveSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-    }
-    public sealed class FieldDeclarationSyntax : BaseFieldDeclarationSyntax {
 {
-        public override SyntaxList<AttributeListSyntax> AttributeLists { get; }

-        public override VariableDeclarationSyntax Declaration { get; }

-        public override SyntaxTokenList Modifiers { get; }

-        public override SyntaxToken SemicolonToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public FieldDeclarationSyntax AddAttributeLists(params AttributeListSyntax[] items);

-        public FieldDeclarationSyntax AddDeclarationVariables(params VariableDeclaratorSyntax[] items);

-        public FieldDeclarationSyntax AddModifiers(params SyntaxToken[] items);

-        public FieldDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, VariableDeclarationSyntax declaration, SyntaxToken semicolonToken);

-        public FieldDeclarationSyntax WithAttributeLists(SyntaxList<AttributeListSyntax> attributeLists);

-        public FieldDeclarationSyntax WithDeclaration(VariableDeclarationSyntax declaration);

-        public FieldDeclarationSyntax WithModifiers(SyntaxTokenList modifiers);

-        public FieldDeclarationSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-    }
-    public sealed class FinallyClauseSyntax : CSharpSyntaxNode {
 {
-        public BlockSyntax Block { get; }

-        public SyntaxToken FinallyKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public FinallyClauseSyntax AddBlockStatements(params StatementSyntax[] items);

-        public FinallyClauseSyntax Update(SyntaxToken finallyKeyword, BlockSyntax block);

-        public FinallyClauseSyntax WithBlock(BlockSyntax block);

-        public FinallyClauseSyntax WithFinallyKeyword(SyntaxToken finallyKeyword);

-    }
-    public sealed class FixedStatementSyntax : StatementSyntax {
 {
-        public SyntaxToken CloseParenToken { get; }

-        public VariableDeclarationSyntax Declaration { get; }

-        public SyntaxToken FixedKeyword { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public StatementSyntax Statement { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public FixedStatementSyntax AddDeclarationVariables(params VariableDeclaratorSyntax[] items);

-        public FixedStatementSyntax Update(SyntaxToken fixedKeyword, SyntaxToken openParenToken, VariableDeclarationSyntax declaration, SyntaxToken closeParenToken, StatementSyntax statement);

-        public FixedStatementSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public FixedStatementSyntax WithDeclaration(VariableDeclarationSyntax declaration);

-        public FixedStatementSyntax WithFixedKeyword(SyntaxToken fixedKeyword);

-        public FixedStatementSyntax WithOpenParenToken(SyntaxToken openParenToken);

-        public FixedStatementSyntax WithStatement(StatementSyntax statement);

-    }
-    public sealed class ForEachStatementSyntax : CommonForEachStatementSyntax {
 {
-        public override SyntaxToken CloseParenToken { get; }

-        public override ExpressionSyntax Expression { get; }

-        public override SyntaxToken ForEachKeyword { get; }

-        public SyntaxToken Identifier { get; }

-        public override SyntaxToken InKeyword { get; }

-        public override SyntaxToken OpenParenToken { get; }

-        public override StatementSyntax Statement { get; }

-        public TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ForEachStatementSyntax Update(SyntaxToken forEachKeyword, SyntaxToken openParenToken, TypeSyntax type, SyntaxToken identifier, SyntaxToken inKeyword, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement);

-        public ForEachStatementSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public ForEachStatementSyntax WithExpression(ExpressionSyntax expression);

-        public ForEachStatementSyntax WithForEachKeyword(SyntaxToken forEachKeyword);

-        public ForEachStatementSyntax WithIdentifier(SyntaxToken identifier);

-        public ForEachStatementSyntax WithInKeyword(SyntaxToken inKeyword);

-        public ForEachStatementSyntax WithOpenParenToken(SyntaxToken openParenToken);

-        public ForEachStatementSyntax WithStatement(StatementSyntax statement);

-        public ForEachStatementSyntax WithType(TypeSyntax type);

-    }
-    public sealed class ForEachVariableStatementSyntax : CommonForEachStatementSyntax {
 {
-        public override SyntaxToken CloseParenToken { get; }

-        public override ExpressionSyntax Expression { get; }

-        public override SyntaxToken ForEachKeyword { get; }

-        public override SyntaxToken InKeyword { get; }

-        public override SyntaxToken OpenParenToken { get; }

-        public override StatementSyntax Statement { get; }

-        public ExpressionSyntax Variable { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ForEachVariableStatementSyntax Update(SyntaxToken forEachKeyword, SyntaxToken openParenToken, ExpressionSyntax variable, SyntaxToken inKeyword, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement);

-        public ForEachVariableStatementSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public ForEachVariableStatementSyntax WithExpression(ExpressionSyntax expression);

-        public ForEachVariableStatementSyntax WithForEachKeyword(SyntaxToken forEachKeyword);

-        public ForEachVariableStatementSyntax WithInKeyword(SyntaxToken inKeyword);

-        public ForEachVariableStatementSyntax WithOpenParenToken(SyntaxToken openParenToken);

-        public ForEachVariableStatementSyntax WithStatement(StatementSyntax statement);

-        public ForEachVariableStatementSyntax WithVariable(ExpressionSyntax variable);

-    }
-    public sealed class ForStatementSyntax : StatementSyntax {
 {
-        public SyntaxToken CloseParenToken { get; }

-        public ExpressionSyntax Condition { get; }

-        public VariableDeclarationSyntax Declaration { get; }

-        public SyntaxToken FirstSemicolonToken { get; }

-        public SyntaxToken ForKeyword { get; }

-        public SeparatedSyntaxList<ExpressionSyntax> Incrementors { get; }

-        public SeparatedSyntaxList<ExpressionSyntax> Initializers { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public SyntaxToken SecondSemicolonToken { get; }

-        public StatementSyntax Statement { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ForStatementSyntax AddIncrementors(params ExpressionSyntax[] items);

-        public ForStatementSyntax AddInitializers(params ExpressionSyntax[] items);

-        public ForStatementSyntax Update(SyntaxToken forKeyword, SyntaxToken openParenToken, VariableDeclarationSyntax declaration, SeparatedSyntaxList<ExpressionSyntax> initializers, SyntaxToken firstSemicolonToken, ExpressionSyntax condition, SyntaxToken secondSemicolonToken, SeparatedSyntaxList<ExpressionSyntax> incrementors, SyntaxToken closeParenToken, StatementSyntax statement);

-        public ForStatementSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public ForStatementSyntax WithCondition(ExpressionSyntax condition);

-        public ForStatementSyntax WithDeclaration(VariableDeclarationSyntax declaration);

-        public ForStatementSyntax WithFirstSemicolonToken(SyntaxToken firstSemicolonToken);

-        public ForStatementSyntax WithForKeyword(SyntaxToken forKeyword);

-        public ForStatementSyntax WithIncrementors(SeparatedSyntaxList<ExpressionSyntax> incrementors);

-        public ForStatementSyntax WithInitializers(SeparatedSyntaxList<ExpressionSyntax> initializers);

-        public ForStatementSyntax WithOpenParenToken(SyntaxToken openParenToken);

-        public ForStatementSyntax WithSecondSemicolonToken(SyntaxToken secondSemicolonToken);

-        public ForStatementSyntax WithStatement(StatementSyntax statement);

-    }
-    public sealed class FromClauseSyntax : QueryClauseSyntax {
 {
-        public ExpressionSyntax Expression { get; }

-        public SyntaxToken FromKeyword { get; }

-        public SyntaxToken Identifier { get; }

-        public SyntaxToken InKeyword { get; }

-        public TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public FromClauseSyntax Update(SyntaxToken fromKeyword, TypeSyntax type, SyntaxToken identifier, SyntaxToken inKeyword, ExpressionSyntax expression);

-        public FromClauseSyntax WithExpression(ExpressionSyntax expression);

-        public FromClauseSyntax WithFromKeyword(SyntaxToken fromKeyword);

-        public FromClauseSyntax WithIdentifier(SyntaxToken identifier);

-        public FromClauseSyntax WithInKeyword(SyntaxToken inKeyword);

-        public FromClauseSyntax WithType(TypeSyntax type);

-    }
-    public sealed class GenericNameSyntax : SimpleNameSyntax {
 {
-        public override SyntaxToken Identifier { get; }

-        public bool IsUnboundGenericName { get; }

-        public TypeArgumentListSyntax TypeArgumentList { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public GenericNameSyntax AddTypeArgumentListArguments(params TypeSyntax[] items);

-        public GenericNameSyntax Update(SyntaxToken identifier, TypeArgumentListSyntax typeArgumentList);

-        public GenericNameSyntax WithIdentifier(SyntaxToken identifier);

-        public GenericNameSyntax WithTypeArgumentList(TypeArgumentListSyntax typeArgumentList);

-    }
-    public sealed class GlobalStatementSyntax : MemberDeclarationSyntax {
 {
-        public StatementSyntax Statement { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public GlobalStatementSyntax Update(StatementSyntax statement);

-        public GlobalStatementSyntax WithStatement(StatementSyntax statement);

-    }
-    public sealed class GotoStatementSyntax : StatementSyntax {
 {
-        public SyntaxToken CaseOrDefaultKeyword { get; }

-        public ExpressionSyntax Expression { get; }

-        public SyntaxToken GotoKeyword { get; }

-        public SyntaxToken SemicolonToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public GotoStatementSyntax Update(SyntaxToken gotoKeyword, SyntaxToken caseOrDefaultKeyword, ExpressionSyntax expression, SyntaxToken semicolonToken);

-        public GotoStatementSyntax WithCaseOrDefaultKeyword(SyntaxToken caseOrDefaultKeyword);

-        public GotoStatementSyntax WithExpression(ExpressionSyntax expression);

-        public GotoStatementSyntax WithGotoKeyword(SyntaxToken gotoKeyword);

-        public GotoStatementSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-    }
-    public sealed class GroupClauseSyntax : SelectOrGroupClauseSyntax {
 {
-        public ExpressionSyntax ByExpression { get; }

-        public SyntaxToken ByKeyword { get; }

-        public ExpressionSyntax GroupExpression { get; }

-        public SyntaxToken GroupKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public GroupClauseSyntax Update(SyntaxToken groupKeyword, ExpressionSyntax groupExpression, SyntaxToken byKeyword, ExpressionSyntax byExpression);

-        public GroupClauseSyntax WithByExpression(ExpressionSyntax byExpression);

-        public GroupClauseSyntax WithByKeyword(SyntaxToken byKeyword);

-        public GroupClauseSyntax WithGroupExpression(ExpressionSyntax groupExpression);

-        public GroupClauseSyntax WithGroupKeyword(SyntaxToken groupKeyword);

-    }
-    public sealed class IdentifierNameSyntax : SimpleNameSyntax {
 {
-        public override SyntaxToken Identifier { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public IdentifierNameSyntax Update(SyntaxToken identifier);

-        public IdentifierNameSyntax WithIdentifier(SyntaxToken identifier);

-    }
-    public sealed class IfDirectiveTriviaSyntax : ConditionalDirectiveTriviaSyntax {
 {
-        public override bool BranchTaken { get; }

-        public override ExpressionSyntax Condition { get; }

-        public override bool ConditionValue { get; }

-        public override SyntaxToken EndOfDirectiveToken { get; }

-        public override SyntaxToken HashToken { get; }

-        public SyntaxToken IfKeyword { get; }

-        public override bool IsActive { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public IfDirectiveTriviaSyntax Update(SyntaxToken hashToken, SyntaxToken ifKeyword, ExpressionSyntax condition, SyntaxToken endOfDirectiveToken, bool isActive, bool branchTaken, bool conditionValue);

-        public IfDirectiveTriviaSyntax WithBranchTaken(bool branchTaken);

-        public IfDirectiveTriviaSyntax WithCondition(ExpressionSyntax condition);

-        public IfDirectiveTriviaSyntax WithConditionValue(bool conditionValue);

-        public IfDirectiveTriviaSyntax WithEndOfDirectiveToken(SyntaxToken endOfDirectiveToken);

-        public IfDirectiveTriviaSyntax WithHashToken(SyntaxToken hashToken);

-        public IfDirectiveTriviaSyntax WithIfKeyword(SyntaxToken ifKeyword);

-        public IfDirectiveTriviaSyntax WithIsActive(bool isActive);

-    }
-    public sealed class IfStatementSyntax : StatementSyntax {
 {
-        public SyntaxToken CloseParenToken { get; }

-        public ExpressionSyntax Condition { get; }

-        public ElseClauseSyntax Else { get; }

-        public SyntaxToken IfKeyword { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public StatementSyntax Statement { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public IfStatementSyntax Update(SyntaxToken ifKeyword, SyntaxToken openParenToken, ExpressionSyntax condition, SyntaxToken closeParenToken, StatementSyntax statement, ElseClauseSyntax @else);

-        public IfStatementSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public IfStatementSyntax WithCondition(ExpressionSyntax condition);

-        public IfStatementSyntax WithElse(ElseClauseSyntax @else);

-        public IfStatementSyntax WithIfKeyword(SyntaxToken ifKeyword);

-        public IfStatementSyntax WithOpenParenToken(SyntaxToken openParenToken);

-        public IfStatementSyntax WithStatement(StatementSyntax statement);

-    }
-    public sealed class ImplicitArrayCreationExpressionSyntax : ExpressionSyntax {
 {
-        public SyntaxToken CloseBracketToken { get; }

-        public SyntaxTokenList Commas { get; }

-        public InitializerExpressionSyntax Initializer { get; }

-        public SyntaxToken NewKeyword { get; }

-        public SyntaxToken OpenBracketToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ImplicitArrayCreationExpressionSyntax AddCommas(params SyntaxToken[] items);

-        public ImplicitArrayCreationExpressionSyntax AddInitializerExpressions(params ExpressionSyntax[] items);

-        public ImplicitArrayCreationExpressionSyntax Update(SyntaxToken newKeyword, SyntaxToken openBracketToken, SyntaxTokenList commas, SyntaxToken closeBracketToken, InitializerExpressionSyntax initializer);

-        public ImplicitArrayCreationExpressionSyntax WithCloseBracketToken(SyntaxToken closeBracketToken);

-        public ImplicitArrayCreationExpressionSyntax WithCommas(SyntaxTokenList commas);

-        public ImplicitArrayCreationExpressionSyntax WithInitializer(InitializerExpressionSyntax initializer);

-        public ImplicitArrayCreationExpressionSyntax WithNewKeyword(SyntaxToken newKeyword);

-        public ImplicitArrayCreationExpressionSyntax WithOpenBracketToken(SyntaxToken openBracketToken);

-    }
-    public sealed class ImplicitElementAccessSyntax : ExpressionSyntax {
 {
-        public BracketedArgumentListSyntax ArgumentList { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ImplicitElementAccessSyntax AddArgumentListArguments(params ArgumentSyntax[] items);

-        public ImplicitElementAccessSyntax Update(BracketedArgumentListSyntax argumentList);

-        public ImplicitElementAccessSyntax WithArgumentList(BracketedArgumentListSyntax argumentList);

-    }
-    public sealed class ImplicitStackAllocArrayCreationExpressionSyntax : ExpressionSyntax {
 {
-        public SyntaxToken CloseBracketToken { get; }

-        public InitializerExpressionSyntax Initializer { get; }

-        public SyntaxToken OpenBracketToken { get; }

-        public SyntaxToken StackAllocKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ImplicitStackAllocArrayCreationExpressionSyntax AddInitializerExpressions(params ExpressionSyntax[] items);

-        public ImplicitStackAllocArrayCreationExpressionSyntax Update(SyntaxToken stackAllocKeyword, SyntaxToken openBracketToken, SyntaxToken closeBracketToken, InitializerExpressionSyntax initializer);

-        public ImplicitStackAllocArrayCreationExpressionSyntax WithCloseBracketToken(SyntaxToken closeBracketToken);

-        public ImplicitStackAllocArrayCreationExpressionSyntax WithInitializer(InitializerExpressionSyntax initializer);

-        public ImplicitStackAllocArrayCreationExpressionSyntax WithOpenBracketToken(SyntaxToken openBracketToken);

-        public ImplicitStackAllocArrayCreationExpressionSyntax WithStackAllocKeyword(SyntaxToken stackAllocKeyword);

-    }
-    public sealed class IncompleteMemberSyntax : MemberDeclarationSyntax {
 {
-        public SyntaxList<AttributeListSyntax> AttributeLists { get; }

-        public SyntaxTokenList Modifiers { get; }

-        public TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public IncompleteMemberSyntax AddAttributeLists(params AttributeListSyntax[] items);

-        public IncompleteMemberSyntax AddModifiers(params SyntaxToken[] items);

-        public IncompleteMemberSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax type);

-        public IncompleteMemberSyntax WithAttributeLists(SyntaxList<AttributeListSyntax> attributeLists);

-        public IncompleteMemberSyntax WithModifiers(SyntaxTokenList modifiers);

-        public IncompleteMemberSyntax WithType(TypeSyntax type);

-    }
-    public sealed class IndexerDeclarationSyntax : BasePropertyDeclarationSyntax {
 {
-        public override AccessorListSyntax AccessorList { get; }

-        public override SyntaxList<AttributeListSyntax> AttributeLists { get; }

-        public override ExplicitInterfaceSpecifierSyntax ExplicitInterfaceSpecifier { get; }

-        public ArrowExpressionClauseSyntax ExpressionBody { get; }

-        public override SyntaxTokenList Modifiers { get; }

-        public BracketedParameterListSyntax ParameterList { get; }

-        public SyntaxToken Semicolon { get; }

-        public SyntaxToken SemicolonToken { get; }

-        public SyntaxToken ThisKeyword { get; }

-        public override TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public IndexerDeclarationSyntax AddAccessorListAccessors(params AccessorDeclarationSyntax[] items);

-        public IndexerDeclarationSyntax AddAttributeLists(params AttributeListSyntax[] items);

-        public IndexerDeclarationSyntax AddModifiers(params SyntaxToken[] items);

-        public IndexerDeclarationSyntax AddParameterListParameters(params ParameterSyntax[] items);

-        public IndexerDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax type, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken thisKeyword, BracketedParameterListSyntax parameterList, AccessorListSyntax accessorList, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolonToken);

-        public IndexerDeclarationSyntax WithAccessorList(AccessorListSyntax accessorList);

-        public IndexerDeclarationSyntax WithAttributeLists(SyntaxList<AttributeListSyntax> attributeLists);

-        public IndexerDeclarationSyntax WithExplicitInterfaceSpecifier(ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier);

-        public IndexerDeclarationSyntax WithExpressionBody(ArrowExpressionClauseSyntax expressionBody);

-        public IndexerDeclarationSyntax WithModifiers(SyntaxTokenList modifiers);

-        public IndexerDeclarationSyntax WithParameterList(BracketedParameterListSyntax parameterList);

-        public IndexerDeclarationSyntax WithSemicolon(SyntaxToken semicolon);

-        public IndexerDeclarationSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-        public IndexerDeclarationSyntax WithThisKeyword(SyntaxToken thisKeyword);

-        public IndexerDeclarationSyntax WithType(TypeSyntax type);

-    }
-    public sealed class IndexerMemberCrefSyntax : MemberCrefSyntax {
 {
-        public CrefBracketedParameterListSyntax Parameters { get; }

-        public SyntaxToken ThisKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public IndexerMemberCrefSyntax AddParametersParameters(params CrefParameterSyntax[] items);

-        public IndexerMemberCrefSyntax Update(SyntaxToken thisKeyword, CrefBracketedParameterListSyntax parameters);

-        public IndexerMemberCrefSyntax WithParameters(CrefBracketedParameterListSyntax parameters);

-        public IndexerMemberCrefSyntax WithThisKeyword(SyntaxToken thisKeyword);

-    }
-    public sealed class InitializerExpressionSyntax : ExpressionSyntax {
 {
-        public SyntaxToken CloseBraceToken { get; }

-        public SeparatedSyntaxList<ExpressionSyntax> Expressions { get; }

-        public SyntaxToken OpenBraceToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public InitializerExpressionSyntax AddExpressions(params ExpressionSyntax[] items);

-        public InitializerExpressionSyntax Update(SyntaxToken openBraceToken, SeparatedSyntaxList<ExpressionSyntax> expressions, SyntaxToken closeBraceToken);

-        public InitializerExpressionSyntax WithCloseBraceToken(SyntaxToken closeBraceToken);

-        public InitializerExpressionSyntax WithExpressions(SeparatedSyntaxList<ExpressionSyntax> expressions);

-        public InitializerExpressionSyntax WithOpenBraceToken(SyntaxToken openBraceToken);

-    }
-    public abstract class InstanceExpressionSyntax : ExpressionSyntax

-    public sealed class InterfaceDeclarationSyntax : TypeDeclarationSyntax {
 {
-        public override SyntaxList<AttributeListSyntax> AttributeLists { get; }

-        public override BaseListSyntax BaseList { get; }

-        public override SyntaxToken CloseBraceToken { get; }

-        public override SyntaxList<TypeParameterConstraintClauseSyntax> ConstraintClauses { get; }

-        public override SyntaxToken Identifier { get; }

-        public override SyntaxToken Keyword { get; }

-        public override SyntaxList<MemberDeclarationSyntax> Members { get; }

-        public override SyntaxTokenList Modifiers { get; }

-        public override SyntaxToken OpenBraceToken { get; }

-        public override SyntaxToken SemicolonToken { get; }

-        public override TypeParameterListSyntax TypeParameterList { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public InterfaceDeclarationSyntax AddAttributeLists(params AttributeListSyntax[] items);

-        public InterfaceDeclarationSyntax AddBaseListTypes(params BaseTypeSyntax[] items);

-        public InterfaceDeclarationSyntax AddConstraintClauses(params TypeParameterConstraintClauseSyntax[] items);

-        public InterfaceDeclarationSyntax AddMembers(params MemberDeclarationSyntax[] items);

-        public InterfaceDeclarationSyntax AddModifiers(params SyntaxToken[] items);

-        public InterfaceDeclarationSyntax AddTypeParameterListParameters(params TypeParameterSyntax[] items);

-        public InterfaceDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken keyword, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, BaseListSyntax baseList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, SyntaxToken openBraceToken, SyntaxList<MemberDeclarationSyntax> members, SyntaxToken closeBraceToken, SyntaxToken semicolonToken);

-        public InterfaceDeclarationSyntax WithAttributeLists(SyntaxList<AttributeListSyntax> attributeLists);

-        public InterfaceDeclarationSyntax WithBaseList(BaseListSyntax baseList);

-        public InterfaceDeclarationSyntax WithCloseBraceToken(SyntaxToken closeBraceToken);

-        public InterfaceDeclarationSyntax WithConstraintClauses(SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses);

-        public InterfaceDeclarationSyntax WithIdentifier(SyntaxToken identifier);

-        public InterfaceDeclarationSyntax WithKeyword(SyntaxToken keyword);

-        public InterfaceDeclarationSyntax WithMembers(SyntaxList<MemberDeclarationSyntax> members);

-        public InterfaceDeclarationSyntax WithModifiers(SyntaxTokenList modifiers);

-        public InterfaceDeclarationSyntax WithOpenBraceToken(SyntaxToken openBraceToken);

-        public InterfaceDeclarationSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-        public InterfaceDeclarationSyntax WithTypeParameterList(TypeParameterListSyntax typeParameterList);

-    }
-    public abstract class InterpolatedStringContentSyntax : CSharpSyntaxNode

-    public sealed class InterpolatedStringExpressionSyntax : ExpressionSyntax {
 {
-        public SyntaxList<InterpolatedStringContentSyntax> Contents { get; }

-        public SyntaxToken StringEndToken { get; }

-        public SyntaxToken StringStartToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public InterpolatedStringExpressionSyntax AddContents(params InterpolatedStringContentSyntax[] items);

-        public InterpolatedStringExpressionSyntax Update(SyntaxToken stringStartToken, SyntaxList<InterpolatedStringContentSyntax> contents, SyntaxToken stringEndToken);

-        public InterpolatedStringExpressionSyntax WithContents(SyntaxList<InterpolatedStringContentSyntax> contents);

-        public InterpolatedStringExpressionSyntax WithStringEndToken(SyntaxToken stringEndToken);

-        public InterpolatedStringExpressionSyntax WithStringStartToken(SyntaxToken stringStartToken);

-    }
-    public sealed class InterpolatedStringTextSyntax : InterpolatedStringContentSyntax {
 {
-        public SyntaxToken TextToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public InterpolatedStringTextSyntax Update(SyntaxToken textToken);

-        public InterpolatedStringTextSyntax WithTextToken(SyntaxToken textToken);

-    }
-    public sealed class InterpolationAlignmentClauseSyntax : CSharpSyntaxNode {
 {
-        public SyntaxToken CommaToken { get; }

-        public ExpressionSyntax Value { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public InterpolationAlignmentClauseSyntax Update(SyntaxToken commaToken, ExpressionSyntax value);

-        public InterpolationAlignmentClauseSyntax WithCommaToken(SyntaxToken commaToken);

-        public InterpolationAlignmentClauseSyntax WithValue(ExpressionSyntax value);

-    }
-    public sealed class InterpolationFormatClauseSyntax : CSharpSyntaxNode {
 {
-        public SyntaxToken ColonToken { get; }

-        public SyntaxToken FormatStringToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public InterpolationFormatClauseSyntax Update(SyntaxToken colonToken, SyntaxToken formatStringToken);

-        public InterpolationFormatClauseSyntax WithColonToken(SyntaxToken colonToken);

-        public InterpolationFormatClauseSyntax WithFormatStringToken(SyntaxToken formatStringToken);

-    }
-    public sealed class InterpolationSyntax : InterpolatedStringContentSyntax {
 {
-        public InterpolationAlignmentClauseSyntax AlignmentClause { get; }

-        public SyntaxToken CloseBraceToken { get; }

-        public ExpressionSyntax Expression { get; }

-        public InterpolationFormatClauseSyntax FormatClause { get; }

-        public SyntaxToken OpenBraceToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public InterpolationSyntax Update(SyntaxToken openBraceToken, ExpressionSyntax expression, InterpolationAlignmentClauseSyntax alignmentClause, InterpolationFormatClauseSyntax formatClause, SyntaxToken closeBraceToken);

-        public InterpolationSyntax WithAlignmentClause(InterpolationAlignmentClauseSyntax alignmentClause);

-        public InterpolationSyntax WithCloseBraceToken(SyntaxToken closeBraceToken);

-        public InterpolationSyntax WithExpression(ExpressionSyntax expression);

-        public InterpolationSyntax WithFormatClause(InterpolationFormatClauseSyntax formatClause);

-        public InterpolationSyntax WithOpenBraceToken(SyntaxToken openBraceToken);

-    }
-    public sealed class InvocationExpressionSyntax : ExpressionSyntax {
 {
-        public ArgumentListSyntax ArgumentList { get; }

-        public ExpressionSyntax Expression { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public InvocationExpressionSyntax AddArgumentListArguments(params ArgumentSyntax[] items);

-        public InvocationExpressionSyntax Update(ExpressionSyntax expression, ArgumentListSyntax argumentList);

-        public InvocationExpressionSyntax WithArgumentList(ArgumentListSyntax argumentList);

-        public InvocationExpressionSyntax WithExpression(ExpressionSyntax expression);

-    }
-    public sealed class IsPatternExpressionSyntax : ExpressionSyntax {
 {
-        public ExpressionSyntax Expression { get; }

-        public SyntaxToken IsKeyword { get; }

-        public PatternSyntax Pattern { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public IsPatternExpressionSyntax Update(ExpressionSyntax expression, SyntaxToken isKeyword, PatternSyntax pattern);

-        public IsPatternExpressionSyntax WithExpression(ExpressionSyntax expression);

-        public IsPatternExpressionSyntax WithIsKeyword(SyntaxToken isKeyword);

-        public IsPatternExpressionSyntax WithPattern(PatternSyntax pattern);

-    }
-    public sealed class JoinClauseSyntax : QueryClauseSyntax {
 {
-        public SyntaxToken EqualsKeyword { get; }

-        public SyntaxToken Identifier { get; }

-        public ExpressionSyntax InExpression { get; }

-        public SyntaxToken InKeyword { get; }

-        public JoinIntoClauseSyntax Into { get; }

-        public SyntaxToken JoinKeyword { get; }

-        public ExpressionSyntax LeftExpression { get; }

-        public SyntaxToken OnKeyword { get; }

-        public ExpressionSyntax RightExpression { get; }

-        public TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public JoinClauseSyntax Update(SyntaxToken joinKeyword, TypeSyntax type, SyntaxToken identifier, SyntaxToken inKeyword, ExpressionSyntax inExpression, SyntaxToken onKeyword, ExpressionSyntax leftExpression, SyntaxToken equalsKeyword, ExpressionSyntax rightExpression, JoinIntoClauseSyntax into);

-        public JoinClauseSyntax WithEqualsKeyword(SyntaxToken equalsKeyword);

-        public JoinClauseSyntax WithIdentifier(SyntaxToken identifier);

-        public JoinClauseSyntax WithInExpression(ExpressionSyntax inExpression);

-        public JoinClauseSyntax WithInKeyword(SyntaxToken inKeyword);

-        public JoinClauseSyntax WithInto(JoinIntoClauseSyntax into);

-        public JoinClauseSyntax WithJoinKeyword(SyntaxToken joinKeyword);

-        public JoinClauseSyntax WithLeftExpression(ExpressionSyntax leftExpression);

-        public JoinClauseSyntax WithOnKeyword(SyntaxToken onKeyword);

-        public JoinClauseSyntax WithRightExpression(ExpressionSyntax rightExpression);

-        public JoinClauseSyntax WithType(TypeSyntax type);

-    }
-    public sealed class JoinIntoClauseSyntax : CSharpSyntaxNode {
 {
-        public SyntaxToken Identifier { get; }

-        public SyntaxToken IntoKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public JoinIntoClauseSyntax Update(SyntaxToken intoKeyword, SyntaxToken identifier);

-        public JoinIntoClauseSyntax WithIdentifier(SyntaxToken identifier);

-        public JoinIntoClauseSyntax WithIntoKeyword(SyntaxToken intoKeyword);

-    }
-    public sealed class LabeledStatementSyntax : StatementSyntax {
 {
-        public SyntaxToken ColonToken { get; }

-        public SyntaxToken Identifier { get; }

-        public StatementSyntax Statement { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public LabeledStatementSyntax Update(SyntaxToken identifier, SyntaxToken colonToken, StatementSyntax statement);

-        public LabeledStatementSyntax WithColonToken(SyntaxToken colonToken);

-        public LabeledStatementSyntax WithIdentifier(SyntaxToken identifier);

-        public LabeledStatementSyntax WithStatement(StatementSyntax statement);

-    }
-    public abstract class LambdaExpressionSyntax : AnonymousFunctionExpressionSyntax {
 {
-        public abstract SyntaxToken ArrowToken { get; }

-    }
-    public sealed class LetClauseSyntax : QueryClauseSyntax {
 {
-        public SyntaxToken EqualsToken { get; }

-        public ExpressionSyntax Expression { get; }

-        public SyntaxToken Identifier { get; }

-        public SyntaxToken LetKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public LetClauseSyntax Update(SyntaxToken letKeyword, SyntaxToken identifier, SyntaxToken equalsToken, ExpressionSyntax expression);

-        public LetClauseSyntax WithEqualsToken(SyntaxToken equalsToken);

-        public LetClauseSyntax WithExpression(ExpressionSyntax expression);

-        public LetClauseSyntax WithIdentifier(SyntaxToken identifier);

-        public LetClauseSyntax WithLetKeyword(SyntaxToken letKeyword);

-    }
-    public sealed class LineDirectiveTriviaSyntax : DirectiveTriviaSyntax {
 {
-        public override SyntaxToken EndOfDirectiveToken { get; }

-        public SyntaxToken File { get; }

-        public override SyntaxToken HashToken { get; }

-        public override bool IsActive { get; }

-        public SyntaxToken Line { get; }

-        public SyntaxToken LineKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public LineDirectiveTriviaSyntax Update(SyntaxToken hashToken, SyntaxToken lineKeyword, SyntaxToken line, SyntaxToken file, SyntaxToken endOfDirectiveToken, bool isActive);

-        public LineDirectiveTriviaSyntax WithEndOfDirectiveToken(SyntaxToken endOfDirectiveToken);

-        public LineDirectiveTriviaSyntax WithFile(SyntaxToken file);

-        public LineDirectiveTriviaSyntax WithHashToken(SyntaxToken hashToken);

-        public LineDirectiveTriviaSyntax WithIsActive(bool isActive);

-        public LineDirectiveTriviaSyntax WithLine(SyntaxToken line);

-        public LineDirectiveTriviaSyntax WithLineKeyword(SyntaxToken lineKeyword);

-    }
-    public sealed class LiteralExpressionSyntax : ExpressionSyntax {
 {
-        public SyntaxToken Token { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public LiteralExpressionSyntax Update(SyntaxToken token);

-        public LiteralExpressionSyntax WithToken(SyntaxToken token);

-    }
-    public sealed class LoadDirectiveTriviaSyntax : DirectiveTriviaSyntax {
 {
-        public override SyntaxToken EndOfDirectiveToken { get; }

-        public SyntaxToken File { get; }

-        public override SyntaxToken HashToken { get; }

-        public override bool IsActive { get; }

-        public SyntaxToken LoadKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public LoadDirectiveTriviaSyntax Update(SyntaxToken hashToken, SyntaxToken loadKeyword, SyntaxToken file, SyntaxToken endOfDirectiveToken, bool isActive);

-        public LoadDirectiveTriviaSyntax WithEndOfDirectiveToken(SyntaxToken endOfDirectiveToken);

-        public LoadDirectiveTriviaSyntax WithFile(SyntaxToken file);

-        public LoadDirectiveTriviaSyntax WithHashToken(SyntaxToken hashToken);

-        public LoadDirectiveTriviaSyntax WithIsActive(bool isActive);

-        public LoadDirectiveTriviaSyntax WithLoadKeyword(SyntaxToken loadKeyword);

-    }
-    public sealed class LocalDeclarationStatementSyntax : StatementSyntax {
 {
-        public VariableDeclarationSyntax Declaration { get; }

-        public bool IsConst { get; }

-        public SyntaxTokenList Modifiers { get; }

-        public SyntaxToken SemicolonToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public LocalDeclarationStatementSyntax AddDeclarationVariables(params VariableDeclaratorSyntax[] items);

-        public LocalDeclarationStatementSyntax AddModifiers(params SyntaxToken[] items);

-        public LocalDeclarationStatementSyntax Update(SyntaxTokenList modifiers, VariableDeclarationSyntax declaration, SyntaxToken semicolonToken);

-        public LocalDeclarationStatementSyntax WithDeclaration(VariableDeclarationSyntax declaration);

-        public LocalDeclarationStatementSyntax WithModifiers(SyntaxTokenList modifiers);

-        public LocalDeclarationStatementSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-    }
-    public sealed class LocalFunctionStatementSyntax : StatementSyntax {
 {
-        public BlockSyntax Body { get; }

-        public SyntaxList<TypeParameterConstraintClauseSyntax> ConstraintClauses { get; }

-        public ArrowExpressionClauseSyntax ExpressionBody { get; }

-        public SyntaxToken Identifier { get; }

-        public SyntaxTokenList Modifiers { get; }

-        public ParameterListSyntax ParameterList { get; }

-        public TypeSyntax ReturnType { get; }

-        public SyntaxToken SemicolonToken { get; }

-        public TypeParameterListSyntax TypeParameterList { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public LocalFunctionStatementSyntax AddBodyStatements(params StatementSyntax[] items);

-        public LocalFunctionStatementSyntax AddConstraintClauses(params TypeParameterConstraintClauseSyntax[] items);

-        public LocalFunctionStatementSyntax AddModifiers(params SyntaxToken[] items);

-        public LocalFunctionStatementSyntax AddParameterListParameters(params ParameterSyntax[] items);

-        public LocalFunctionStatementSyntax AddTypeParameterListParameters(params TypeParameterSyntax[] items);

-        public LocalFunctionStatementSyntax Update(SyntaxTokenList modifiers, TypeSyntax returnType, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, ParameterListSyntax parameterList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, BlockSyntax body, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolonToken);

-        public LocalFunctionStatementSyntax WithBody(BlockSyntax body);

-        public LocalFunctionStatementSyntax WithConstraintClauses(SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses);

-        public LocalFunctionStatementSyntax WithExpressionBody(ArrowExpressionClauseSyntax expressionBody);

-        public LocalFunctionStatementSyntax WithIdentifier(SyntaxToken identifier);

-        public LocalFunctionStatementSyntax WithModifiers(SyntaxTokenList modifiers);

-        public LocalFunctionStatementSyntax WithParameterList(ParameterListSyntax parameterList);

-        public LocalFunctionStatementSyntax WithReturnType(TypeSyntax returnType);

-        public LocalFunctionStatementSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-        public LocalFunctionStatementSyntax WithTypeParameterList(TypeParameterListSyntax typeParameterList);

-    }
-    public sealed class LockStatementSyntax : StatementSyntax {
 {
-        public SyntaxToken CloseParenToken { get; }

-        public ExpressionSyntax Expression { get; }

-        public SyntaxToken LockKeyword { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public StatementSyntax Statement { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public LockStatementSyntax Update(SyntaxToken lockKeyword, SyntaxToken openParenToken, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement);

-        public LockStatementSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public LockStatementSyntax WithExpression(ExpressionSyntax expression);

-        public LockStatementSyntax WithLockKeyword(SyntaxToken lockKeyword);

-        public LockStatementSyntax WithOpenParenToken(SyntaxToken openParenToken);

-        public LockStatementSyntax WithStatement(StatementSyntax statement);

-    }
-    public sealed class MakeRefExpressionSyntax : ExpressionSyntax {
 {
-        public SyntaxToken CloseParenToken { get; }

-        public ExpressionSyntax Expression { get; }

-        public SyntaxToken Keyword { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public MakeRefExpressionSyntax Update(SyntaxToken keyword, SyntaxToken openParenToken, ExpressionSyntax expression, SyntaxToken closeParenToken);

-        public MakeRefExpressionSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public MakeRefExpressionSyntax WithExpression(ExpressionSyntax expression);

-        public MakeRefExpressionSyntax WithKeyword(SyntaxToken keyword);

-        public MakeRefExpressionSyntax WithOpenParenToken(SyntaxToken openParenToken);

-    }
-    public sealed class MemberAccessExpressionSyntax : ExpressionSyntax {
 {
-        public ExpressionSyntax Expression { get; }

-        public SimpleNameSyntax Name { get; }

-        public SyntaxToken OperatorToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public MemberAccessExpressionSyntax Update(ExpressionSyntax expression, SyntaxToken operatorToken, SimpleNameSyntax name);

-        public MemberAccessExpressionSyntax WithExpression(ExpressionSyntax expression);

-        public MemberAccessExpressionSyntax WithName(SimpleNameSyntax name);

-        public MemberAccessExpressionSyntax WithOperatorToken(SyntaxToken operatorToken);

-    }
-    public sealed class MemberBindingExpressionSyntax : ExpressionSyntax {
 {
-        public SimpleNameSyntax Name { get; }

-        public SyntaxToken OperatorToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public MemberBindingExpressionSyntax Update(SyntaxToken operatorToken, SimpleNameSyntax name);

-        public MemberBindingExpressionSyntax WithName(SimpleNameSyntax name);

-        public MemberBindingExpressionSyntax WithOperatorToken(SyntaxToken operatorToken);

-    }
-    public abstract class MemberCrefSyntax : CrefSyntax

-    public abstract class MemberDeclarationSyntax : CSharpSyntaxNode

-    public sealed class MethodDeclarationSyntax : BaseMethodDeclarationSyntax {
 {
-        public int Arity { get; }

-        public override SyntaxList<AttributeListSyntax> AttributeLists { get; }

-        public override BlockSyntax Body { get; }

-        public SyntaxList<TypeParameterConstraintClauseSyntax> ConstraintClauses { get; }

-        public ExplicitInterfaceSpecifierSyntax ExplicitInterfaceSpecifier { get; }

-        public override ArrowExpressionClauseSyntax ExpressionBody { get; }

-        public SyntaxToken Identifier { get; }

-        public override SyntaxTokenList Modifiers { get; }

-        public override ParameterListSyntax ParameterList { get; }

-        public TypeSyntax ReturnType { get; }

-        public override SyntaxToken SemicolonToken { get; }

-        public TypeParameterListSyntax TypeParameterList { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public MethodDeclarationSyntax AddAttributeLists(params AttributeListSyntax[] items);

-        public MethodDeclarationSyntax AddBodyStatements(params StatementSyntax[] items);

-        public MethodDeclarationSyntax AddConstraintClauses(params TypeParameterConstraintClauseSyntax[] items);

-        public MethodDeclarationSyntax AddModifiers(params SyntaxToken[] items);

-        public MethodDeclarationSyntax AddParameterListParameters(params ParameterSyntax[] items);

-        public MethodDeclarationSyntax AddTypeParameterListParameters(params TypeParameterSyntax[] items);

-        public MethodDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax returnType, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, ParameterListSyntax parameterList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, BlockSyntax body, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolonToken);

-        public MethodDeclarationSyntax WithAttributeLists(SyntaxList<AttributeListSyntax> attributeLists);

-        public MethodDeclarationSyntax WithBody(BlockSyntax body);

-        public MethodDeclarationSyntax WithConstraintClauses(SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses);

-        public MethodDeclarationSyntax WithExplicitInterfaceSpecifier(ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier);

-        public MethodDeclarationSyntax WithExpressionBody(ArrowExpressionClauseSyntax expressionBody);

-        public MethodDeclarationSyntax WithIdentifier(SyntaxToken identifier);

-        public MethodDeclarationSyntax WithModifiers(SyntaxTokenList modifiers);

-        public MethodDeclarationSyntax WithParameterList(ParameterListSyntax parameterList);

-        public MethodDeclarationSyntax WithReturnType(TypeSyntax returnType);

-        public MethodDeclarationSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-        public MethodDeclarationSyntax WithTypeParameterList(TypeParameterListSyntax typeParameterList);

-    }
-    public sealed class NameColonSyntax : CSharpSyntaxNode {
 {
-        public SyntaxToken ColonToken { get; }

-        public IdentifierNameSyntax Name { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public NameColonSyntax Update(IdentifierNameSyntax name, SyntaxToken colonToken);

-        public NameColonSyntax WithColonToken(SyntaxToken colonToken);

-        public NameColonSyntax WithName(IdentifierNameSyntax name);

-    }
-    public sealed class NameEqualsSyntax : CSharpSyntaxNode {
 {
-        public SyntaxToken EqualsToken { get; }

-        public IdentifierNameSyntax Name { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public NameEqualsSyntax Update(IdentifierNameSyntax name, SyntaxToken equalsToken);

-        public NameEqualsSyntax WithEqualsToken(SyntaxToken equalsToken);

-        public NameEqualsSyntax WithName(IdentifierNameSyntax name);

-    }
-    public sealed class NameMemberCrefSyntax : MemberCrefSyntax {
 {
-        public TypeSyntax Name { get; }

-        public CrefParameterListSyntax Parameters { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public NameMemberCrefSyntax AddParametersParameters(params CrefParameterSyntax[] items);

-        public NameMemberCrefSyntax Update(TypeSyntax name, CrefParameterListSyntax parameters);

-        public NameMemberCrefSyntax WithName(TypeSyntax name);

-        public NameMemberCrefSyntax WithParameters(CrefParameterListSyntax parameters);

-    }
-    public sealed class NamespaceDeclarationSyntax : MemberDeclarationSyntax {
 {
-        public SyntaxToken CloseBraceToken { get; }

-        public SyntaxList<ExternAliasDirectiveSyntax> Externs { get; }

-        public SyntaxList<MemberDeclarationSyntax> Members { get; }

-        public NameSyntax Name { get; }

-        public SyntaxToken NamespaceKeyword { get; }

-        public SyntaxToken OpenBraceToken { get; }

-        public SyntaxToken SemicolonToken { get; }

-        public SyntaxList<UsingDirectiveSyntax> Usings { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public NamespaceDeclarationSyntax AddExterns(params ExternAliasDirectiveSyntax[] items);

-        public NamespaceDeclarationSyntax AddMembers(params MemberDeclarationSyntax[] items);

-        public NamespaceDeclarationSyntax AddUsings(params UsingDirectiveSyntax[] items);

-        public NamespaceDeclarationSyntax Update(SyntaxToken namespaceKeyword, NameSyntax name, SyntaxToken openBraceToken, SyntaxList<ExternAliasDirectiveSyntax> externs, SyntaxList<UsingDirectiveSyntax> usings, SyntaxList<MemberDeclarationSyntax> members, SyntaxToken closeBraceToken, SyntaxToken semicolonToken);

-        public NamespaceDeclarationSyntax WithCloseBraceToken(SyntaxToken closeBraceToken);

-        public NamespaceDeclarationSyntax WithExterns(SyntaxList<ExternAliasDirectiveSyntax> externs);

-        public NamespaceDeclarationSyntax WithMembers(SyntaxList<MemberDeclarationSyntax> members);

-        public NamespaceDeclarationSyntax WithName(NameSyntax name);

-        public NamespaceDeclarationSyntax WithNamespaceKeyword(SyntaxToken namespaceKeyword);

-        public NamespaceDeclarationSyntax WithOpenBraceToken(SyntaxToken openBraceToken);

-        public NamespaceDeclarationSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-        public NamespaceDeclarationSyntax WithUsings(SyntaxList<UsingDirectiveSyntax> usings);

-    }
-    public abstract class NameSyntax : TypeSyntax {
 {
-        public int Arity { get; }

-    }
-    public sealed class NullableTypeSyntax : TypeSyntax {
 {
-        public TypeSyntax ElementType { get; }

-        public SyntaxToken QuestionToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public NullableTypeSyntax Update(TypeSyntax elementType, SyntaxToken questionToken);

-        public NullableTypeSyntax WithElementType(TypeSyntax elementType);

-        public NullableTypeSyntax WithQuestionToken(SyntaxToken questionToken);

-    }
-    public sealed class ObjectCreationExpressionSyntax : ExpressionSyntax {
 {
-        public ArgumentListSyntax ArgumentList { get; }

-        public InitializerExpressionSyntax Initializer { get; }

-        public SyntaxToken NewKeyword { get; }

-        public TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ObjectCreationExpressionSyntax AddArgumentListArguments(params ArgumentSyntax[] items);

-        public ObjectCreationExpressionSyntax Update(SyntaxToken newKeyword, TypeSyntax type, ArgumentListSyntax argumentList, InitializerExpressionSyntax initializer);

-        public ObjectCreationExpressionSyntax WithArgumentList(ArgumentListSyntax argumentList);

-        public ObjectCreationExpressionSyntax WithInitializer(InitializerExpressionSyntax initializer);

-        public ObjectCreationExpressionSyntax WithNewKeyword(SyntaxToken newKeyword);

-        public ObjectCreationExpressionSyntax WithType(TypeSyntax type);

-    }
-    public sealed class OmittedArraySizeExpressionSyntax : ExpressionSyntax {
 {
-        public SyntaxToken OmittedArraySizeExpressionToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public OmittedArraySizeExpressionSyntax Update(SyntaxToken omittedArraySizeExpressionToken);

-        public OmittedArraySizeExpressionSyntax WithOmittedArraySizeExpressionToken(SyntaxToken omittedArraySizeExpressionToken);

-    }
-    public sealed class OmittedTypeArgumentSyntax : TypeSyntax {
 {
-        public SyntaxToken OmittedTypeArgumentToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public OmittedTypeArgumentSyntax Update(SyntaxToken omittedTypeArgumentToken);

-        public OmittedTypeArgumentSyntax WithOmittedTypeArgumentToken(SyntaxToken omittedTypeArgumentToken);

-    }
-    public sealed class OperatorDeclarationSyntax : BaseMethodDeclarationSyntax {
 {
-        public override SyntaxList<AttributeListSyntax> AttributeLists { get; }

-        public override BlockSyntax Body { get; }

-        public override ArrowExpressionClauseSyntax ExpressionBody { get; }

-        public override SyntaxTokenList Modifiers { get; }

-        public SyntaxToken OperatorKeyword { get; }

-        public SyntaxToken OperatorToken { get; }

-        public override ParameterListSyntax ParameterList { get; }

-        public TypeSyntax ReturnType { get; }

-        public override SyntaxToken SemicolonToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public OperatorDeclarationSyntax AddAttributeLists(params AttributeListSyntax[] items);

-        public OperatorDeclarationSyntax AddBodyStatements(params StatementSyntax[] items);

-        public OperatorDeclarationSyntax AddModifiers(params SyntaxToken[] items);

-        public OperatorDeclarationSyntax AddParameterListParameters(params ParameterSyntax[] items);

-        public OperatorDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax returnType, SyntaxToken operatorKeyword, SyntaxToken operatorToken, ParameterListSyntax parameterList, BlockSyntax body, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolonToken);

-        public OperatorDeclarationSyntax WithAttributeLists(SyntaxList<AttributeListSyntax> attributeLists);

-        public OperatorDeclarationSyntax WithBody(BlockSyntax body);

-        public OperatorDeclarationSyntax WithExpressionBody(ArrowExpressionClauseSyntax expressionBody);

-        public OperatorDeclarationSyntax WithModifiers(SyntaxTokenList modifiers);

-        public OperatorDeclarationSyntax WithOperatorKeyword(SyntaxToken operatorKeyword);

-        public OperatorDeclarationSyntax WithOperatorToken(SyntaxToken operatorToken);

-        public OperatorDeclarationSyntax WithParameterList(ParameterListSyntax parameterList);

-        public OperatorDeclarationSyntax WithReturnType(TypeSyntax returnType);

-        public OperatorDeclarationSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-    }
-    public sealed class OperatorMemberCrefSyntax : MemberCrefSyntax {
 {
-        public SyntaxToken OperatorKeyword { get; }

-        public SyntaxToken OperatorToken { get; }

-        public CrefParameterListSyntax Parameters { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public OperatorMemberCrefSyntax AddParametersParameters(params CrefParameterSyntax[] items);

-        public OperatorMemberCrefSyntax Update(SyntaxToken operatorKeyword, SyntaxToken operatorToken, CrefParameterListSyntax parameters);

-        public OperatorMemberCrefSyntax WithOperatorKeyword(SyntaxToken operatorKeyword);

-        public OperatorMemberCrefSyntax WithOperatorToken(SyntaxToken operatorToken);

-        public OperatorMemberCrefSyntax WithParameters(CrefParameterListSyntax parameters);

-    }
-    public sealed class OrderByClauseSyntax : QueryClauseSyntax {
 {
-        public SyntaxToken OrderByKeyword { get; }

-        public SeparatedSyntaxList<OrderingSyntax> Orderings { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public OrderByClauseSyntax AddOrderings(params OrderingSyntax[] items);

-        public OrderByClauseSyntax Update(SyntaxToken orderByKeyword, SeparatedSyntaxList<OrderingSyntax> orderings);

-        public OrderByClauseSyntax WithOrderByKeyword(SyntaxToken orderByKeyword);

-        public OrderByClauseSyntax WithOrderings(SeparatedSyntaxList<OrderingSyntax> orderings);

-    }
-    public sealed class OrderingSyntax : CSharpSyntaxNode {
 {
-        public SyntaxToken AscendingOrDescendingKeyword { get; }

-        public ExpressionSyntax Expression { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public OrderingSyntax Update(ExpressionSyntax expression, SyntaxToken ascendingOrDescendingKeyword);

-        public OrderingSyntax WithAscendingOrDescendingKeyword(SyntaxToken ascendingOrDescendingKeyword);

-        public OrderingSyntax WithExpression(ExpressionSyntax expression);

-    }
-    public sealed class ParameterListSyntax : BaseParameterListSyntax {
 {
-        public SyntaxToken CloseParenToken { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public override SeparatedSyntaxList<ParameterSyntax> Parameters { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ParameterListSyntax AddParameters(params ParameterSyntax[] items);

-        public ParameterListSyntax Update(SyntaxToken openParenToken, SeparatedSyntaxList<ParameterSyntax> parameters, SyntaxToken closeParenToken);

-        public ParameterListSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public ParameterListSyntax WithOpenParenToken(SyntaxToken openParenToken);

-        public ParameterListSyntax WithParameters(SeparatedSyntaxList<ParameterSyntax> parameters);

-    }
-    public sealed class ParameterSyntax : CSharpSyntaxNode {
 {
-        public SyntaxList<AttributeListSyntax> AttributeLists { get; }

-        public EqualsValueClauseSyntax Default { get; }

-        public SyntaxToken Identifier { get; }

-        public SyntaxTokenList Modifiers { get; }

-        public TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ParameterSyntax AddAttributeLists(params AttributeListSyntax[] items);

-        public ParameterSyntax AddModifiers(params SyntaxToken[] items);

-        public ParameterSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax type, SyntaxToken identifier, EqualsValueClauseSyntax @default);

-        public ParameterSyntax WithAttributeLists(SyntaxList<AttributeListSyntax> attributeLists);

-        public ParameterSyntax WithDefault(EqualsValueClauseSyntax @default);

-        public ParameterSyntax WithIdentifier(SyntaxToken identifier);

-        public ParameterSyntax WithModifiers(SyntaxTokenList modifiers);

-        public ParameterSyntax WithType(TypeSyntax type);

-    }
-    public sealed class ParenthesizedExpressionSyntax : ExpressionSyntax {
 {
-        public SyntaxToken CloseParenToken { get; }

-        public ExpressionSyntax Expression { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ParenthesizedExpressionSyntax Update(SyntaxToken openParenToken, ExpressionSyntax expression, SyntaxToken closeParenToken);

-        public ParenthesizedExpressionSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public ParenthesizedExpressionSyntax WithExpression(ExpressionSyntax expression);

-        public ParenthesizedExpressionSyntax WithOpenParenToken(SyntaxToken openParenToken);

-    }
-    public sealed class ParenthesizedLambdaExpressionSyntax : LambdaExpressionSyntax {
 {
-        public override SyntaxToken ArrowToken { get; }

-        public override SyntaxToken AsyncKeyword { get; }

-        public override CSharpSyntaxNode Body { get; }

-        public ParameterListSyntax ParameterList { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ParenthesizedLambdaExpressionSyntax AddParameterListParameters(params ParameterSyntax[] items);

-        public ParenthesizedLambdaExpressionSyntax Update(SyntaxToken asyncKeyword, ParameterListSyntax parameterList, SyntaxToken arrowToken, CSharpSyntaxNode body);

-        public ParenthesizedLambdaExpressionSyntax WithArrowToken(SyntaxToken arrowToken);

-        public ParenthesizedLambdaExpressionSyntax WithAsyncKeyword(SyntaxToken asyncKeyword);

-        public ParenthesizedLambdaExpressionSyntax WithBody(CSharpSyntaxNode body);

-        public ParenthesizedLambdaExpressionSyntax WithParameterList(ParameterListSyntax parameterList);

-    }
-    public sealed class ParenthesizedVariableDesignationSyntax : VariableDesignationSyntax {
 {
-        public SyntaxToken CloseParenToken { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public SeparatedSyntaxList<VariableDesignationSyntax> Variables { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ParenthesizedVariableDesignationSyntax AddVariables(params VariableDesignationSyntax[] items);

-        public ParenthesizedVariableDesignationSyntax Update(SyntaxToken openParenToken, SeparatedSyntaxList<VariableDesignationSyntax> variables, SyntaxToken closeParenToken);

-        public ParenthesizedVariableDesignationSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public ParenthesizedVariableDesignationSyntax WithOpenParenToken(SyntaxToken openParenToken);

-        public ParenthesizedVariableDesignationSyntax WithVariables(SeparatedSyntaxList<VariableDesignationSyntax> variables);

-    }
-    public abstract class PatternSyntax : CSharpSyntaxNode

-    public sealed class PointerTypeSyntax : TypeSyntax {
 {
-        public SyntaxToken AsteriskToken { get; }

-        public TypeSyntax ElementType { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public PointerTypeSyntax Update(TypeSyntax elementType, SyntaxToken asteriskToken);

-        public PointerTypeSyntax WithAsteriskToken(SyntaxToken asteriskToken);

-        public PointerTypeSyntax WithElementType(TypeSyntax elementType);

-    }
-    public sealed class PostfixUnaryExpressionSyntax : ExpressionSyntax {
 {
-        public ExpressionSyntax Operand { get; }

-        public SyntaxToken OperatorToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public PostfixUnaryExpressionSyntax Update(ExpressionSyntax operand, SyntaxToken operatorToken);

-        public PostfixUnaryExpressionSyntax WithOperand(ExpressionSyntax operand);

-        public PostfixUnaryExpressionSyntax WithOperatorToken(SyntaxToken operatorToken);

-    }
-    public sealed class PragmaChecksumDirectiveTriviaSyntax : DirectiveTriviaSyntax {
 {
-        public SyntaxToken Bytes { get; }

-        public SyntaxToken ChecksumKeyword { get; }

-        public override SyntaxToken EndOfDirectiveToken { get; }

-        public SyntaxToken File { get; }

-        public SyntaxToken Guid { get; }

-        public override SyntaxToken HashToken { get; }

-        public override bool IsActive { get; }

-        public SyntaxToken PragmaKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public PragmaChecksumDirectiveTriviaSyntax Update(SyntaxToken hashToken, SyntaxToken pragmaKeyword, SyntaxToken checksumKeyword, SyntaxToken file, SyntaxToken guid, SyntaxToken bytes, SyntaxToken endOfDirectiveToken, bool isActive);

-        public PragmaChecksumDirectiveTriviaSyntax WithBytes(SyntaxToken bytes);

-        public PragmaChecksumDirectiveTriviaSyntax WithChecksumKeyword(SyntaxToken checksumKeyword);

-        public PragmaChecksumDirectiveTriviaSyntax WithEndOfDirectiveToken(SyntaxToken endOfDirectiveToken);

-        public PragmaChecksumDirectiveTriviaSyntax WithFile(SyntaxToken file);

-        public PragmaChecksumDirectiveTriviaSyntax WithGuid(SyntaxToken guid);

-        public PragmaChecksumDirectiveTriviaSyntax WithHashToken(SyntaxToken hashToken);

-        public PragmaChecksumDirectiveTriviaSyntax WithIsActive(bool isActive);

-        public PragmaChecksumDirectiveTriviaSyntax WithPragmaKeyword(SyntaxToken pragmaKeyword);

-    }
-    public sealed class PragmaWarningDirectiveTriviaSyntax : DirectiveTriviaSyntax {
 {
-        public SyntaxToken DisableOrRestoreKeyword { get; }

-        public override SyntaxToken EndOfDirectiveToken { get; }

-        public SeparatedSyntaxList<ExpressionSyntax> ErrorCodes { get; }

-        public override SyntaxToken HashToken { get; }

-        public override bool IsActive { get; }

-        public SyntaxToken PragmaKeyword { get; }

-        public SyntaxToken WarningKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public PragmaWarningDirectiveTriviaSyntax AddErrorCodes(params ExpressionSyntax[] items);

-        public PragmaWarningDirectiveTriviaSyntax Update(SyntaxToken hashToken, SyntaxToken pragmaKeyword, SyntaxToken warningKeyword, SyntaxToken disableOrRestoreKeyword, SeparatedSyntaxList<ExpressionSyntax> errorCodes, SyntaxToken endOfDirectiveToken, bool isActive);

-        public PragmaWarningDirectiveTriviaSyntax WithDisableOrRestoreKeyword(SyntaxToken disableOrRestoreKeyword);

-        public PragmaWarningDirectiveTriviaSyntax WithEndOfDirectiveToken(SyntaxToken endOfDirectiveToken);

-        public PragmaWarningDirectiveTriviaSyntax WithErrorCodes(SeparatedSyntaxList<ExpressionSyntax> errorCodes);

-        public PragmaWarningDirectiveTriviaSyntax WithHashToken(SyntaxToken hashToken);

-        public PragmaWarningDirectiveTriviaSyntax WithIsActive(bool isActive);

-        public PragmaWarningDirectiveTriviaSyntax WithPragmaKeyword(SyntaxToken pragmaKeyword);

-        public PragmaWarningDirectiveTriviaSyntax WithWarningKeyword(SyntaxToken warningKeyword);

-    }
-    public sealed class PredefinedTypeSyntax : TypeSyntax {
 {
-        public SyntaxToken Keyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public PredefinedTypeSyntax Update(SyntaxToken keyword);

-        public PredefinedTypeSyntax WithKeyword(SyntaxToken keyword);

-    }
-    public sealed class PrefixUnaryExpressionSyntax : ExpressionSyntax {
 {
-        public ExpressionSyntax Operand { get; }

-        public SyntaxToken OperatorToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public PrefixUnaryExpressionSyntax Update(SyntaxToken operatorToken, ExpressionSyntax operand);

-        public PrefixUnaryExpressionSyntax WithOperand(ExpressionSyntax operand);

-        public PrefixUnaryExpressionSyntax WithOperatorToken(SyntaxToken operatorToken);

-    }
-    public sealed class PropertyDeclarationSyntax : BasePropertyDeclarationSyntax {
 {
-        public override AccessorListSyntax AccessorList { get; }

-        public override SyntaxList<AttributeListSyntax> AttributeLists { get; }

-        public override ExplicitInterfaceSpecifierSyntax ExplicitInterfaceSpecifier { get; }

-        public ArrowExpressionClauseSyntax ExpressionBody { get; }

-        public SyntaxToken Identifier { get; }

-        public EqualsValueClauseSyntax Initializer { get; }

-        public override SyntaxTokenList Modifiers { get; }

-        public SyntaxToken Semicolon { get; }

-        public SyntaxToken SemicolonToken { get; }

-        public override TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public PropertyDeclarationSyntax AddAccessorListAccessors(params AccessorDeclarationSyntax[] items);

-        public PropertyDeclarationSyntax AddAttributeLists(params AttributeListSyntax[] items);

-        public PropertyDeclarationSyntax AddModifiers(params SyntaxToken[] items);

-        public PropertyDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax type, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken identifier, AccessorListSyntax accessorList, ArrowExpressionClauseSyntax expressionBody, EqualsValueClauseSyntax initializer, SyntaxToken semicolonToken);

-        public PropertyDeclarationSyntax WithAccessorList(AccessorListSyntax accessorList);

-        public PropertyDeclarationSyntax WithAttributeLists(SyntaxList<AttributeListSyntax> attributeLists);

-        public PropertyDeclarationSyntax WithExplicitInterfaceSpecifier(ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier);

-        public PropertyDeclarationSyntax WithExpressionBody(ArrowExpressionClauseSyntax expressionBody);

-        public PropertyDeclarationSyntax WithIdentifier(SyntaxToken identifier);

-        public PropertyDeclarationSyntax WithInitializer(EqualsValueClauseSyntax initializer);

-        public PropertyDeclarationSyntax WithModifiers(SyntaxTokenList modifiers);

-        public PropertyDeclarationSyntax WithSemicolon(SyntaxToken semicolon);

-        public PropertyDeclarationSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-        public PropertyDeclarationSyntax WithType(TypeSyntax type);

-    }
-    public sealed class QualifiedCrefSyntax : CrefSyntax {
 {
-        public TypeSyntax Container { get; }

-        public SyntaxToken DotToken { get; }

-        public MemberCrefSyntax Member { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public QualifiedCrefSyntax Update(TypeSyntax container, SyntaxToken dotToken, MemberCrefSyntax member);

-        public QualifiedCrefSyntax WithContainer(TypeSyntax container);

-        public QualifiedCrefSyntax WithDotToken(SyntaxToken dotToken);

-        public QualifiedCrefSyntax WithMember(MemberCrefSyntax member);

-    }
-    public sealed class QualifiedNameSyntax : NameSyntax {
 {
-        public SyntaxToken DotToken { get; }

-        public NameSyntax Left { get; }

-        public SimpleNameSyntax Right { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public QualifiedNameSyntax Update(NameSyntax left, SyntaxToken dotToken, SimpleNameSyntax right);

-        public QualifiedNameSyntax WithDotToken(SyntaxToken dotToken);

-        public QualifiedNameSyntax WithLeft(NameSyntax left);

-        public QualifiedNameSyntax WithRight(SimpleNameSyntax right);

-    }
-    public sealed class QueryBodySyntax : CSharpSyntaxNode {
 {
-        public SyntaxList<QueryClauseSyntax> Clauses { get; }

-        public QueryContinuationSyntax Continuation { get; }

-        public SelectOrGroupClauseSyntax SelectOrGroup { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public QueryBodySyntax AddClauses(params QueryClauseSyntax[] items);

-        public QueryBodySyntax Update(SyntaxList<QueryClauseSyntax> clauses, SelectOrGroupClauseSyntax selectOrGroup, QueryContinuationSyntax continuation);

-        public QueryBodySyntax WithClauses(SyntaxList<QueryClauseSyntax> clauses);

-        public QueryBodySyntax WithContinuation(QueryContinuationSyntax continuation);

-        public QueryBodySyntax WithSelectOrGroup(SelectOrGroupClauseSyntax selectOrGroup);

-    }
-    public abstract class QueryClauseSyntax : CSharpSyntaxNode

-    public sealed class QueryContinuationSyntax : CSharpSyntaxNode {
 {
-        public QueryBodySyntax Body { get; }

-        public SyntaxToken Identifier { get; }

-        public SyntaxToken IntoKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public QueryContinuationSyntax AddBodyClauses(params QueryClauseSyntax[] items);

-        public QueryContinuationSyntax Update(SyntaxToken intoKeyword, SyntaxToken identifier, QueryBodySyntax body);

-        public QueryContinuationSyntax WithBody(QueryBodySyntax body);

-        public QueryContinuationSyntax WithIdentifier(SyntaxToken identifier);

-        public QueryContinuationSyntax WithIntoKeyword(SyntaxToken intoKeyword);

-    }
-    public sealed class QueryExpressionSyntax : ExpressionSyntax {
 {
-        public QueryBodySyntax Body { get; }

-        public FromClauseSyntax FromClause { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public QueryExpressionSyntax AddBodyClauses(params QueryClauseSyntax[] items);

-        public QueryExpressionSyntax Update(FromClauseSyntax fromClause, QueryBodySyntax body);

-        public QueryExpressionSyntax WithBody(QueryBodySyntax body);

-        public QueryExpressionSyntax WithFromClause(FromClauseSyntax fromClause);

-    }
-    public sealed class ReferenceDirectiveTriviaSyntax : DirectiveTriviaSyntax {
 {
-        public override SyntaxToken EndOfDirectiveToken { get; }

-        public SyntaxToken File { get; }

-        public override SyntaxToken HashToken { get; }

-        public override bool IsActive { get; }

-        public SyntaxToken ReferenceKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ReferenceDirectiveTriviaSyntax Update(SyntaxToken hashToken, SyntaxToken referenceKeyword, SyntaxToken file, SyntaxToken endOfDirectiveToken, bool isActive);

-        public ReferenceDirectiveTriviaSyntax WithEndOfDirectiveToken(SyntaxToken endOfDirectiveToken);

-        public ReferenceDirectiveTriviaSyntax WithFile(SyntaxToken file);

-        public ReferenceDirectiveTriviaSyntax WithHashToken(SyntaxToken hashToken);

-        public ReferenceDirectiveTriviaSyntax WithIsActive(bool isActive);

-        public ReferenceDirectiveTriviaSyntax WithReferenceKeyword(SyntaxToken referenceKeyword);

-    }
-    public sealed class RefExpressionSyntax : ExpressionSyntax {
 {
-        public ExpressionSyntax Expression { get; }

-        public SyntaxToken RefKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public RefExpressionSyntax Update(SyntaxToken refKeyword, ExpressionSyntax expression);

-        public RefExpressionSyntax WithExpression(ExpressionSyntax expression);

-        public RefExpressionSyntax WithRefKeyword(SyntaxToken refKeyword);

-    }
-    public sealed class RefTypeExpressionSyntax : ExpressionSyntax {
 {
-        public SyntaxToken CloseParenToken { get; }

-        public ExpressionSyntax Expression { get; }

-        public SyntaxToken Keyword { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public RefTypeExpressionSyntax Update(SyntaxToken keyword, SyntaxToken openParenToken, ExpressionSyntax expression, SyntaxToken closeParenToken);

-        public RefTypeExpressionSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public RefTypeExpressionSyntax WithExpression(ExpressionSyntax expression);

-        public RefTypeExpressionSyntax WithKeyword(SyntaxToken keyword);

-        public RefTypeExpressionSyntax WithOpenParenToken(SyntaxToken openParenToken);

-    }
-    public sealed class RefTypeSyntax : TypeSyntax {
 {
-        public SyntaxToken ReadOnlyKeyword { get; }

-        public SyntaxToken RefKeyword { get; }

-        public TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public RefTypeSyntax Update(SyntaxToken refKeyword, TypeSyntax type);

-        public RefTypeSyntax Update(SyntaxToken refKeyword, SyntaxToken readOnlyKeyword, TypeSyntax type);

-        public RefTypeSyntax WithReadOnlyKeyword(SyntaxToken readOnlyKeyword);

-        public RefTypeSyntax WithRefKeyword(SyntaxToken refKeyword);

-        public RefTypeSyntax WithType(TypeSyntax type);

-    }
-    public sealed class RefValueExpressionSyntax : ExpressionSyntax {
 {
-        public SyntaxToken CloseParenToken { get; }

-        public SyntaxToken Comma { get; }

-        public ExpressionSyntax Expression { get; }

-        public SyntaxToken Keyword { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public RefValueExpressionSyntax Update(SyntaxToken keyword, SyntaxToken openParenToken, ExpressionSyntax expression, SyntaxToken comma, TypeSyntax type, SyntaxToken closeParenToken);

-        public RefValueExpressionSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public RefValueExpressionSyntax WithComma(SyntaxToken comma);

-        public RefValueExpressionSyntax WithExpression(ExpressionSyntax expression);

-        public RefValueExpressionSyntax WithKeyword(SyntaxToken keyword);

-        public RefValueExpressionSyntax WithOpenParenToken(SyntaxToken openParenToken);

-        public RefValueExpressionSyntax WithType(TypeSyntax type);

-    }
-    public sealed class RegionDirectiveTriviaSyntax : DirectiveTriviaSyntax {
 {
-        public override SyntaxToken EndOfDirectiveToken { get; }

-        public override SyntaxToken HashToken { get; }

-        public override bool IsActive { get; }

-        public SyntaxToken RegionKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public RegionDirectiveTriviaSyntax Update(SyntaxToken hashToken, SyntaxToken regionKeyword, SyntaxToken endOfDirectiveToken, bool isActive);

-        public RegionDirectiveTriviaSyntax WithEndOfDirectiveToken(SyntaxToken endOfDirectiveToken);

-        public RegionDirectiveTriviaSyntax WithHashToken(SyntaxToken hashToken);

-        public RegionDirectiveTriviaSyntax WithIsActive(bool isActive);

-        public RegionDirectiveTriviaSyntax WithRegionKeyword(SyntaxToken regionKeyword);

-    }
-    public sealed class ReturnStatementSyntax : StatementSyntax {
 {
-        public ExpressionSyntax Expression { get; }

-        public SyntaxToken ReturnKeyword { get; }

-        public SyntaxToken SemicolonToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ReturnStatementSyntax Update(SyntaxToken returnKeyword, ExpressionSyntax expression, SyntaxToken semicolonToken);

-        public ReturnStatementSyntax WithExpression(ExpressionSyntax expression);

-        public ReturnStatementSyntax WithReturnKeyword(SyntaxToken returnKeyword);

-        public ReturnStatementSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-    }
-    public sealed class SelectClauseSyntax : SelectOrGroupClauseSyntax {
 {
-        public ExpressionSyntax Expression { get; }

-        public SyntaxToken SelectKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public SelectClauseSyntax Update(SyntaxToken selectKeyword, ExpressionSyntax expression);

-        public SelectClauseSyntax WithExpression(ExpressionSyntax expression);

-        public SelectClauseSyntax WithSelectKeyword(SyntaxToken selectKeyword);

-    }
-    public abstract class SelectOrGroupClauseSyntax : CSharpSyntaxNode

-    public sealed class ShebangDirectiveTriviaSyntax : DirectiveTriviaSyntax {
 {
-        public override SyntaxToken EndOfDirectiveToken { get; }

-        public SyntaxToken ExclamationToken { get; }

-        public override SyntaxToken HashToken { get; }

-        public override bool IsActive { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ShebangDirectiveTriviaSyntax Update(SyntaxToken hashToken, SyntaxToken exclamationToken, SyntaxToken endOfDirectiveToken, bool isActive);

-        public ShebangDirectiveTriviaSyntax WithEndOfDirectiveToken(SyntaxToken endOfDirectiveToken);

-        public ShebangDirectiveTriviaSyntax WithExclamationToken(SyntaxToken exclamationToken);

-        public ShebangDirectiveTriviaSyntax WithHashToken(SyntaxToken hashToken);

-        public ShebangDirectiveTriviaSyntax WithIsActive(bool isActive);

-    }
-    public sealed class SimpleBaseTypeSyntax : BaseTypeSyntax {
 {
-        public override TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public SimpleBaseTypeSyntax Update(TypeSyntax type);

-        public SimpleBaseTypeSyntax WithType(TypeSyntax type);

-    }
-    public sealed class SimpleLambdaExpressionSyntax : LambdaExpressionSyntax {
 {
-        public override SyntaxToken ArrowToken { get; }

-        public override SyntaxToken AsyncKeyword { get; }

-        public override CSharpSyntaxNode Body { get; }

-        public ParameterSyntax Parameter { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public SimpleLambdaExpressionSyntax AddParameterAttributeLists(params AttributeListSyntax[] items);

-        public SimpleLambdaExpressionSyntax AddParameterModifiers(params SyntaxToken[] items);

-        public SimpleLambdaExpressionSyntax Update(SyntaxToken asyncKeyword, ParameterSyntax parameter, SyntaxToken arrowToken, CSharpSyntaxNode body);

-        public SimpleLambdaExpressionSyntax WithArrowToken(SyntaxToken arrowToken);

-        public SimpleLambdaExpressionSyntax WithAsyncKeyword(SyntaxToken asyncKeyword);

-        public SimpleLambdaExpressionSyntax WithBody(CSharpSyntaxNode body);

-        public SimpleLambdaExpressionSyntax WithParameter(ParameterSyntax parameter);

-    }
-    public abstract class SimpleNameSyntax : NameSyntax {
 {
-        public abstract SyntaxToken Identifier { get; }

-    }
-    public sealed class SingleVariableDesignationSyntax : VariableDesignationSyntax {
 {
-        public SyntaxToken Identifier { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public SingleVariableDesignationSyntax Update(SyntaxToken identifier);

-        public SingleVariableDesignationSyntax WithIdentifier(SyntaxToken identifier);

-    }
-    public sealed class SizeOfExpressionSyntax : ExpressionSyntax {
 {
-        public SyntaxToken CloseParenToken { get; }

-        public SyntaxToken Keyword { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public SizeOfExpressionSyntax Update(SyntaxToken keyword, SyntaxToken openParenToken, TypeSyntax type, SyntaxToken closeParenToken);

-        public SizeOfExpressionSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public SizeOfExpressionSyntax WithKeyword(SyntaxToken keyword);

-        public SizeOfExpressionSyntax WithOpenParenToken(SyntaxToken openParenToken);

-        public SizeOfExpressionSyntax WithType(TypeSyntax type);

-    }
-    public sealed class SkippedTokensTriviaSyntax : StructuredTriviaSyntax, ISkippedTokensTriviaSyntax {
 {
-        public SyntaxTokenList Tokens { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public SkippedTokensTriviaSyntax AddTokens(params SyntaxToken[] items);

-        public SkippedTokensTriviaSyntax Update(SyntaxTokenList tokens);

-        public SkippedTokensTriviaSyntax WithTokens(SyntaxTokenList tokens);

-    }
-    public sealed class StackAllocArrayCreationExpressionSyntax : ExpressionSyntax {
 {
-        public InitializerExpressionSyntax Initializer { get; }

-        public SyntaxToken StackAllocKeyword { get; }

-        public TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public StackAllocArrayCreationExpressionSyntax Update(SyntaxToken stackAllocKeyword, TypeSyntax type);

-        public StackAllocArrayCreationExpressionSyntax Update(SyntaxToken stackAllocKeyword, TypeSyntax type, InitializerExpressionSyntax initializer);

-        public StackAllocArrayCreationExpressionSyntax WithInitializer(InitializerExpressionSyntax initializer);

-        public StackAllocArrayCreationExpressionSyntax WithStackAllocKeyword(SyntaxToken stackAllocKeyword);

-        public StackAllocArrayCreationExpressionSyntax WithType(TypeSyntax type);

-    }
-    public abstract class StatementSyntax : CSharpSyntaxNode

-    public sealed class StructDeclarationSyntax : TypeDeclarationSyntax {
 {
-        public override SyntaxList<AttributeListSyntax> AttributeLists { get; }

-        public override BaseListSyntax BaseList { get; }

-        public override SyntaxToken CloseBraceToken { get; }

-        public override SyntaxList<TypeParameterConstraintClauseSyntax> ConstraintClauses { get; }

-        public override SyntaxToken Identifier { get; }

-        public override SyntaxToken Keyword { get; }

-        public override SyntaxList<MemberDeclarationSyntax> Members { get; }

-        public override SyntaxTokenList Modifiers { get; }

-        public override SyntaxToken OpenBraceToken { get; }

-        public override SyntaxToken SemicolonToken { get; }

-        public override TypeParameterListSyntax TypeParameterList { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public StructDeclarationSyntax AddAttributeLists(params AttributeListSyntax[] items);

-        public StructDeclarationSyntax AddBaseListTypes(params BaseTypeSyntax[] items);

-        public StructDeclarationSyntax AddConstraintClauses(params TypeParameterConstraintClauseSyntax[] items);

-        public StructDeclarationSyntax AddMembers(params MemberDeclarationSyntax[] items);

-        public StructDeclarationSyntax AddModifiers(params SyntaxToken[] items);

-        public StructDeclarationSyntax AddTypeParameterListParameters(params TypeParameterSyntax[] items);

-        public StructDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken keyword, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, BaseListSyntax baseList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, SyntaxToken openBraceToken, SyntaxList<MemberDeclarationSyntax> members, SyntaxToken closeBraceToken, SyntaxToken semicolonToken);

-        public StructDeclarationSyntax WithAttributeLists(SyntaxList<AttributeListSyntax> attributeLists);

-        public StructDeclarationSyntax WithBaseList(BaseListSyntax baseList);

-        public StructDeclarationSyntax WithCloseBraceToken(SyntaxToken closeBraceToken);

-        public StructDeclarationSyntax WithConstraintClauses(SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses);

-        public StructDeclarationSyntax WithIdentifier(SyntaxToken identifier);

-        public StructDeclarationSyntax WithKeyword(SyntaxToken keyword);

-        public StructDeclarationSyntax WithMembers(SyntaxList<MemberDeclarationSyntax> members);

-        public StructDeclarationSyntax WithModifiers(SyntaxTokenList modifiers);

-        public StructDeclarationSyntax WithOpenBraceToken(SyntaxToken openBraceToken);

-        public StructDeclarationSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-        public StructDeclarationSyntax WithTypeParameterList(TypeParameterListSyntax typeParameterList);

-    }
-    public abstract class StructuredTriviaSyntax : CSharpSyntaxNode, IStructuredTriviaSyntax {
 {
-        public override SyntaxTrivia ParentTrivia { get; }

-    }
-    public abstract class SwitchLabelSyntax : CSharpSyntaxNode {
 {
-        public abstract SyntaxToken ColonToken { get; }

-        public abstract SyntaxToken Keyword { get; }

-    }
-    public sealed class SwitchSectionSyntax : CSharpSyntaxNode {
 {
-        public SyntaxList<SwitchLabelSyntax> Labels { get; }

-        public SyntaxList<StatementSyntax> Statements { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public SwitchSectionSyntax AddLabels(params SwitchLabelSyntax[] items);

-        public SwitchSectionSyntax AddStatements(params StatementSyntax[] items);

-        public SwitchSectionSyntax Update(SyntaxList<SwitchLabelSyntax> labels, SyntaxList<StatementSyntax> statements);

-        public SwitchSectionSyntax WithLabels(SyntaxList<SwitchLabelSyntax> labels);

-        public SwitchSectionSyntax WithStatements(SyntaxList<StatementSyntax> statements);

-    }
-    public sealed class SwitchStatementSyntax : StatementSyntax {
 {
-        public SyntaxToken CloseBraceToken { get; }

-        public SyntaxToken CloseParenToken { get; }

-        public ExpressionSyntax Expression { get; }

-        public SyntaxToken OpenBraceToken { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public SyntaxList<SwitchSectionSyntax> Sections { get; }

-        public SyntaxToken SwitchKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public SwitchStatementSyntax AddSections(params SwitchSectionSyntax[] items);

-        public SwitchStatementSyntax Update(SyntaxToken switchKeyword, SyntaxToken openParenToken, ExpressionSyntax expression, SyntaxToken closeParenToken, SyntaxToken openBraceToken, SyntaxList<SwitchSectionSyntax> sections, SyntaxToken closeBraceToken);

-        public SwitchStatementSyntax WithCloseBraceToken(SyntaxToken closeBraceToken);

-        public SwitchStatementSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public SwitchStatementSyntax WithExpression(ExpressionSyntax expression);

-        public SwitchStatementSyntax WithOpenBraceToken(SyntaxToken openBraceToken);

-        public SwitchStatementSyntax WithOpenParenToken(SyntaxToken openParenToken);

-        public SwitchStatementSyntax WithSections(SyntaxList<SwitchSectionSyntax> sections);

-        public SwitchStatementSyntax WithSwitchKeyword(SyntaxToken switchKeyword);

-    }
-    public sealed class ThisExpressionSyntax : InstanceExpressionSyntax {
 {
-        public SyntaxToken Token { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ThisExpressionSyntax Update(SyntaxToken token);

-        public ThisExpressionSyntax WithToken(SyntaxToken token);

-    }
-    public sealed class ThrowExpressionSyntax : ExpressionSyntax {
 {
-        public ExpressionSyntax Expression { get; }

-        public SyntaxToken ThrowKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ThrowExpressionSyntax Update(SyntaxToken throwKeyword, ExpressionSyntax expression);

-        public ThrowExpressionSyntax WithExpression(ExpressionSyntax expression);

-        public ThrowExpressionSyntax WithThrowKeyword(SyntaxToken throwKeyword);

-    }
-    public sealed class ThrowStatementSyntax : StatementSyntax {
 {
-        public ExpressionSyntax Expression { get; }

-        public SyntaxToken SemicolonToken { get; }

-        public SyntaxToken ThrowKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public ThrowStatementSyntax Update(SyntaxToken throwKeyword, ExpressionSyntax expression, SyntaxToken semicolonToken);

-        public ThrowStatementSyntax WithExpression(ExpressionSyntax expression);

-        public ThrowStatementSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-        public ThrowStatementSyntax WithThrowKeyword(SyntaxToken throwKeyword);

-    }
-    public sealed class TryStatementSyntax : StatementSyntax {
 {
-        public BlockSyntax Block { get; }

-        public SyntaxList<CatchClauseSyntax> Catches { get; }

-        public FinallyClauseSyntax Finally { get; }

-        public SyntaxToken TryKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public TryStatementSyntax AddBlockStatements(params StatementSyntax[] items);

-        public TryStatementSyntax AddCatches(params CatchClauseSyntax[] items);

-        public TryStatementSyntax Update(SyntaxToken tryKeyword, BlockSyntax block, SyntaxList<CatchClauseSyntax> catches, FinallyClauseSyntax @finally);

-        public TryStatementSyntax WithBlock(BlockSyntax block);

-        public TryStatementSyntax WithCatches(SyntaxList<CatchClauseSyntax> catches);

-        public TryStatementSyntax WithFinally(FinallyClauseSyntax @finally);

-        public TryStatementSyntax WithTryKeyword(SyntaxToken tryKeyword);

-    }
-    public sealed class TupleElementSyntax : CSharpSyntaxNode {
 {
-        public SyntaxToken Identifier { get; }

-        public TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public TupleElementSyntax Update(TypeSyntax type, SyntaxToken identifier);

-        public TupleElementSyntax WithIdentifier(SyntaxToken identifier);

-        public TupleElementSyntax WithType(TypeSyntax type);

-    }
-    public sealed class TupleExpressionSyntax : ExpressionSyntax {
 {
-        public SeparatedSyntaxList<ArgumentSyntax> Arguments { get; }

-        public SyntaxToken CloseParenToken { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public TupleExpressionSyntax AddArguments(params ArgumentSyntax[] items);

-        public TupleExpressionSyntax Update(SyntaxToken openParenToken, SeparatedSyntaxList<ArgumentSyntax> arguments, SyntaxToken closeParenToken);

-        public TupleExpressionSyntax WithArguments(SeparatedSyntaxList<ArgumentSyntax> arguments);

-        public TupleExpressionSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public TupleExpressionSyntax WithOpenParenToken(SyntaxToken openParenToken);

-    }
-    public sealed class TupleTypeSyntax : TypeSyntax {
 {
-        public SyntaxToken CloseParenToken { get; }

-        public SeparatedSyntaxList<TupleElementSyntax> Elements { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public TupleTypeSyntax AddElements(params TupleElementSyntax[] items);

-        public TupleTypeSyntax Update(SyntaxToken openParenToken, SeparatedSyntaxList<TupleElementSyntax> elements, SyntaxToken closeParenToken);

-        public TupleTypeSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public TupleTypeSyntax WithElements(SeparatedSyntaxList<TupleElementSyntax> elements);

-        public TupleTypeSyntax WithOpenParenToken(SyntaxToken openParenToken);

-    }
-    public sealed class TypeArgumentListSyntax : CSharpSyntaxNode {
 {
-        public SeparatedSyntaxList<TypeSyntax> Arguments { get; }

-        public SyntaxToken GreaterThanToken { get; }

-        public SyntaxToken LessThanToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public TypeArgumentListSyntax AddArguments(params TypeSyntax[] items);

-        public TypeArgumentListSyntax Update(SyntaxToken lessThanToken, SeparatedSyntaxList<TypeSyntax> arguments, SyntaxToken greaterThanToken);

-        public TypeArgumentListSyntax WithArguments(SeparatedSyntaxList<TypeSyntax> arguments);

-        public TypeArgumentListSyntax WithGreaterThanToken(SyntaxToken greaterThanToken);

-        public TypeArgumentListSyntax WithLessThanToken(SyntaxToken lessThanToken);

-    }
-    public sealed class TypeConstraintSyntax : TypeParameterConstraintSyntax {
 {
-        public TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public TypeConstraintSyntax Update(TypeSyntax type);

-        public TypeConstraintSyntax WithType(TypeSyntax type);

-    }
-    public sealed class TypeCrefSyntax : CrefSyntax {
 {
-        public TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public TypeCrefSyntax Update(TypeSyntax type);

-        public TypeCrefSyntax WithType(TypeSyntax type);

-    }
-    public abstract class TypeDeclarationSyntax : BaseTypeDeclarationSyntax {
 {
-        public int Arity { get; }

-        public abstract SyntaxList<TypeParameterConstraintClauseSyntax> ConstraintClauses { get; }

-        public abstract SyntaxToken Keyword { get; }

-        public abstract SyntaxList<MemberDeclarationSyntax> Members { get; }

-        public abstract TypeParameterListSyntax TypeParameterList { get; }

-    }
-    public sealed class TypeOfExpressionSyntax : ExpressionSyntax {
 {
-        public SyntaxToken CloseParenToken { get; }

-        public SyntaxToken Keyword { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public TypeSyntax Type { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public TypeOfExpressionSyntax Update(SyntaxToken keyword, SyntaxToken openParenToken, TypeSyntax type, SyntaxToken closeParenToken);

-        public TypeOfExpressionSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public TypeOfExpressionSyntax WithKeyword(SyntaxToken keyword);

-        public TypeOfExpressionSyntax WithOpenParenToken(SyntaxToken openParenToken);

-        public TypeOfExpressionSyntax WithType(TypeSyntax type);

-    }
-    public sealed class TypeParameterConstraintClauseSyntax : CSharpSyntaxNode {
 {
-        public SyntaxToken ColonToken { get; }

-        public SeparatedSyntaxList<TypeParameterConstraintSyntax> Constraints { get; }

-        public IdentifierNameSyntax Name { get; }

-        public SyntaxToken WhereKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public TypeParameterConstraintClauseSyntax AddConstraints(params TypeParameterConstraintSyntax[] items);

-        public TypeParameterConstraintClauseSyntax Update(SyntaxToken whereKeyword, IdentifierNameSyntax name, SyntaxToken colonToken, SeparatedSyntaxList<TypeParameterConstraintSyntax> constraints);

-        public TypeParameterConstraintClauseSyntax WithColonToken(SyntaxToken colonToken);

-        public TypeParameterConstraintClauseSyntax WithConstraints(SeparatedSyntaxList<TypeParameterConstraintSyntax> constraints);

-        public TypeParameterConstraintClauseSyntax WithName(IdentifierNameSyntax name);

-        public TypeParameterConstraintClauseSyntax WithWhereKeyword(SyntaxToken whereKeyword);

-    }
-    public abstract class TypeParameterConstraintSyntax : CSharpSyntaxNode

-    public sealed class TypeParameterListSyntax : CSharpSyntaxNode {
 {
-        public SyntaxToken GreaterThanToken { get; }

-        public SyntaxToken LessThanToken { get; }

-        public SeparatedSyntaxList<TypeParameterSyntax> Parameters { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public TypeParameterListSyntax AddParameters(params TypeParameterSyntax[] items);

-        public TypeParameterListSyntax Update(SyntaxToken lessThanToken, SeparatedSyntaxList<TypeParameterSyntax> parameters, SyntaxToken greaterThanToken);

-        public TypeParameterListSyntax WithGreaterThanToken(SyntaxToken greaterThanToken);

-        public TypeParameterListSyntax WithLessThanToken(SyntaxToken lessThanToken);

-        public TypeParameterListSyntax WithParameters(SeparatedSyntaxList<TypeParameterSyntax> parameters);

-    }
-    public sealed class TypeParameterSyntax : CSharpSyntaxNode {
 {
-        public SyntaxList<AttributeListSyntax> AttributeLists { get; }

-        public SyntaxToken Identifier { get; }

-        public SyntaxToken VarianceKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public TypeParameterSyntax AddAttributeLists(params AttributeListSyntax[] items);

-        public TypeParameterSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxToken varianceKeyword, SyntaxToken identifier);

-        public TypeParameterSyntax WithAttributeLists(SyntaxList<AttributeListSyntax> attributeLists);

-        public TypeParameterSyntax WithIdentifier(SyntaxToken identifier);

-        public TypeParameterSyntax WithVarianceKeyword(SyntaxToken varianceKeyword);

-    }
-    public abstract class TypeSyntax : ExpressionSyntax {
 {
-        public bool IsUnmanaged { get; }

-        public bool IsVar { get; }

-    }
-    public sealed class UndefDirectiveTriviaSyntax : DirectiveTriviaSyntax {
 {
-        public override SyntaxToken EndOfDirectiveToken { get; }

-        public override SyntaxToken HashToken { get; }

-        public override bool IsActive { get; }

-        public SyntaxToken Name { get; }

-        public SyntaxToken UndefKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public UndefDirectiveTriviaSyntax Update(SyntaxToken hashToken, SyntaxToken undefKeyword, SyntaxToken name, SyntaxToken endOfDirectiveToken, bool isActive);

-        public UndefDirectiveTriviaSyntax WithEndOfDirectiveToken(SyntaxToken endOfDirectiveToken);

-        public UndefDirectiveTriviaSyntax WithHashToken(SyntaxToken hashToken);

-        public UndefDirectiveTriviaSyntax WithIsActive(bool isActive);

-        public UndefDirectiveTriviaSyntax WithName(SyntaxToken name);

-        public UndefDirectiveTriviaSyntax WithUndefKeyword(SyntaxToken undefKeyword);

-    }
-    public sealed class UnsafeStatementSyntax : StatementSyntax {
 {
-        public BlockSyntax Block { get; }

-        public SyntaxToken UnsafeKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public UnsafeStatementSyntax AddBlockStatements(params StatementSyntax[] items);

-        public UnsafeStatementSyntax Update(SyntaxToken unsafeKeyword, BlockSyntax block);

-        public UnsafeStatementSyntax WithBlock(BlockSyntax block);

-        public UnsafeStatementSyntax WithUnsafeKeyword(SyntaxToken unsafeKeyword);

-    }
-    public sealed class UsingDirectiveSyntax : CSharpSyntaxNode {
 {
-        public NameEqualsSyntax Alias { get; }

-        public NameSyntax Name { get; }

-        public SyntaxToken SemicolonToken { get; }

-        public SyntaxToken StaticKeyword { get; }

-        public SyntaxToken UsingKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public UsingDirectiveSyntax Update(SyntaxToken usingKeyword, SyntaxToken staticKeyword, NameEqualsSyntax alias, NameSyntax name, SyntaxToken semicolonToken);

-        public UsingDirectiveSyntax WithAlias(NameEqualsSyntax alias);

-        public UsingDirectiveSyntax WithName(NameSyntax name);

-        public UsingDirectiveSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-        public UsingDirectiveSyntax WithStaticKeyword(SyntaxToken staticKeyword);

-        public UsingDirectiveSyntax WithUsingKeyword(SyntaxToken usingKeyword);

-    }
-    public sealed class UsingStatementSyntax : StatementSyntax {
 {
-        public SyntaxToken CloseParenToken { get; }

-        public VariableDeclarationSyntax Declaration { get; }

-        public ExpressionSyntax Expression { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public StatementSyntax Statement { get; }

-        public SyntaxToken UsingKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public UsingStatementSyntax Update(SyntaxToken usingKeyword, SyntaxToken openParenToken, VariableDeclarationSyntax declaration, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement);

-        public UsingStatementSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public UsingStatementSyntax WithDeclaration(VariableDeclarationSyntax declaration);

-        public UsingStatementSyntax WithExpression(ExpressionSyntax expression);

-        public UsingStatementSyntax WithOpenParenToken(SyntaxToken openParenToken);

-        public UsingStatementSyntax WithStatement(StatementSyntax statement);

-        public UsingStatementSyntax WithUsingKeyword(SyntaxToken usingKeyword);

-    }
-    public sealed class VariableDeclarationSyntax : CSharpSyntaxNode {
 {
-        public TypeSyntax Type { get; }

-        public SeparatedSyntaxList<VariableDeclaratorSyntax> Variables { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public VariableDeclarationSyntax AddVariables(params VariableDeclaratorSyntax[] items);

-        public VariableDeclarationSyntax Update(TypeSyntax type, SeparatedSyntaxList<VariableDeclaratorSyntax> variables);

-        public VariableDeclarationSyntax WithType(TypeSyntax type);

-        public VariableDeclarationSyntax WithVariables(SeparatedSyntaxList<VariableDeclaratorSyntax> variables);

-    }
-    public sealed class VariableDeclaratorSyntax : CSharpSyntaxNode {
 {
-        public BracketedArgumentListSyntax ArgumentList { get; }

-        public SyntaxToken Identifier { get; }

-        public EqualsValueClauseSyntax Initializer { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public VariableDeclaratorSyntax AddArgumentListArguments(params ArgumentSyntax[] items);

-        public VariableDeclaratorSyntax Update(SyntaxToken identifier, BracketedArgumentListSyntax argumentList, EqualsValueClauseSyntax initializer);

-        public VariableDeclaratorSyntax WithArgumentList(BracketedArgumentListSyntax argumentList);

-        public VariableDeclaratorSyntax WithIdentifier(SyntaxToken identifier);

-        public VariableDeclaratorSyntax WithInitializer(EqualsValueClauseSyntax initializer);

-    }
-    public abstract class VariableDesignationSyntax : CSharpSyntaxNode

-    public sealed class WarningDirectiveTriviaSyntax : DirectiveTriviaSyntax {
 {
-        public override SyntaxToken EndOfDirectiveToken { get; }

-        public override SyntaxToken HashToken { get; }

-        public override bool IsActive { get; }

-        public SyntaxToken WarningKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public WarningDirectiveTriviaSyntax Update(SyntaxToken hashToken, SyntaxToken warningKeyword, SyntaxToken endOfDirectiveToken, bool isActive);

-        public WarningDirectiveTriviaSyntax WithEndOfDirectiveToken(SyntaxToken endOfDirectiveToken);

-        public WarningDirectiveTriviaSyntax WithHashToken(SyntaxToken hashToken);

-        public WarningDirectiveTriviaSyntax WithIsActive(bool isActive);

-        public WarningDirectiveTriviaSyntax WithWarningKeyword(SyntaxToken warningKeyword);

-    }
-    public sealed class WhenClauseSyntax : CSharpSyntaxNode {
 {
-        public ExpressionSyntax Condition { get; }

-        public SyntaxToken WhenKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public WhenClauseSyntax Update(SyntaxToken whenKeyword, ExpressionSyntax condition);

-        public WhenClauseSyntax WithCondition(ExpressionSyntax condition);

-        public WhenClauseSyntax WithWhenKeyword(SyntaxToken whenKeyword);

-    }
-    public sealed class WhereClauseSyntax : QueryClauseSyntax {
 {
-        public ExpressionSyntax Condition { get; }

-        public SyntaxToken WhereKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public WhereClauseSyntax Update(SyntaxToken whereKeyword, ExpressionSyntax condition);

-        public WhereClauseSyntax WithCondition(ExpressionSyntax condition);

-        public WhereClauseSyntax WithWhereKeyword(SyntaxToken whereKeyword);

-    }
-    public sealed class WhileStatementSyntax : StatementSyntax {
 {
-        public SyntaxToken CloseParenToken { get; }

-        public ExpressionSyntax Condition { get; }

-        public SyntaxToken OpenParenToken { get; }

-        public StatementSyntax Statement { get; }

-        public SyntaxToken WhileKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public WhileStatementSyntax Update(SyntaxToken whileKeyword, SyntaxToken openParenToken, ExpressionSyntax condition, SyntaxToken closeParenToken, StatementSyntax statement);

-        public WhileStatementSyntax WithCloseParenToken(SyntaxToken closeParenToken);

-        public WhileStatementSyntax WithCondition(ExpressionSyntax condition);

-        public WhileStatementSyntax WithOpenParenToken(SyntaxToken openParenToken);

-        public WhileStatementSyntax WithStatement(StatementSyntax statement);

-        public WhileStatementSyntax WithWhileKeyword(SyntaxToken whileKeyword);

-    }
-    public abstract class XmlAttributeSyntax : CSharpSyntaxNode {
 {
-        public abstract SyntaxToken EndQuoteToken { get; }

-        public abstract SyntaxToken EqualsToken { get; }

-        public abstract XmlNameSyntax Name { get; }

-        public abstract SyntaxToken StartQuoteToken { get; }

-    }
-    public sealed class XmlCDataSectionSyntax : XmlNodeSyntax {
 {
-        public SyntaxToken EndCDataToken { get; }

-        public SyntaxToken StartCDataToken { get; }

-        public SyntaxTokenList TextTokens { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public XmlCDataSectionSyntax AddTextTokens(params SyntaxToken[] items);

-        public XmlCDataSectionSyntax Update(SyntaxToken startCDataToken, SyntaxTokenList textTokens, SyntaxToken endCDataToken);

-        public XmlCDataSectionSyntax WithEndCDataToken(SyntaxToken endCDataToken);

-        public XmlCDataSectionSyntax WithStartCDataToken(SyntaxToken startCDataToken);

-        public XmlCDataSectionSyntax WithTextTokens(SyntaxTokenList textTokens);

-    }
-    public sealed class XmlCommentSyntax : XmlNodeSyntax {
 {
-        public SyntaxToken LessThanExclamationMinusMinusToken { get; }

-        public SyntaxToken MinusMinusGreaterThanToken { get; }

-        public SyntaxTokenList TextTokens { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public XmlCommentSyntax AddTextTokens(params SyntaxToken[] items);

-        public XmlCommentSyntax Update(SyntaxToken lessThanExclamationMinusMinusToken, SyntaxTokenList textTokens, SyntaxToken minusMinusGreaterThanToken);

-        public XmlCommentSyntax WithLessThanExclamationMinusMinusToken(SyntaxToken lessThanExclamationMinusMinusToken);

-        public XmlCommentSyntax WithMinusMinusGreaterThanToken(SyntaxToken minusMinusGreaterThanToken);

-        public XmlCommentSyntax WithTextTokens(SyntaxTokenList textTokens);

-    }
-    public sealed class XmlCrefAttributeSyntax : XmlAttributeSyntax {
 {
-        public CrefSyntax Cref { get; }

-        public override SyntaxToken EndQuoteToken { get; }

-        public override SyntaxToken EqualsToken { get; }

-        public override XmlNameSyntax Name { get; }

-        public override SyntaxToken StartQuoteToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public XmlCrefAttributeSyntax Update(XmlNameSyntax name, SyntaxToken equalsToken, SyntaxToken startQuoteToken, CrefSyntax cref, SyntaxToken endQuoteToken);

-        public XmlCrefAttributeSyntax WithCref(CrefSyntax cref);

-        public XmlCrefAttributeSyntax WithEndQuoteToken(SyntaxToken endQuoteToken);

-        public XmlCrefAttributeSyntax WithEqualsToken(SyntaxToken equalsToken);

-        public XmlCrefAttributeSyntax WithName(XmlNameSyntax name);

-        public XmlCrefAttributeSyntax WithStartQuoteToken(SyntaxToken startQuoteToken);

-    }
-    public sealed class XmlElementEndTagSyntax : CSharpSyntaxNode {
 {
-        public SyntaxToken GreaterThanToken { get; }

-        public SyntaxToken LessThanSlashToken { get; }

-        public XmlNameSyntax Name { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public XmlElementEndTagSyntax Update(SyntaxToken lessThanSlashToken, XmlNameSyntax name, SyntaxToken greaterThanToken);

-        public XmlElementEndTagSyntax WithGreaterThanToken(SyntaxToken greaterThanToken);

-        public XmlElementEndTagSyntax WithLessThanSlashToken(SyntaxToken lessThanSlashToken);

-        public XmlElementEndTagSyntax WithName(XmlNameSyntax name);

-    }
-    public sealed class XmlElementStartTagSyntax : CSharpSyntaxNode {
 {
-        public SyntaxList<XmlAttributeSyntax> Attributes { get; }

-        public SyntaxToken GreaterThanToken { get; }

-        public SyntaxToken LessThanToken { get; }

-        public XmlNameSyntax Name { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public XmlElementStartTagSyntax AddAttributes(params XmlAttributeSyntax[] items);

-        public XmlElementStartTagSyntax Update(SyntaxToken lessThanToken, XmlNameSyntax name, SyntaxList<XmlAttributeSyntax> attributes, SyntaxToken greaterThanToken);

-        public XmlElementStartTagSyntax WithAttributes(SyntaxList<XmlAttributeSyntax> attributes);

-        public XmlElementStartTagSyntax WithGreaterThanToken(SyntaxToken greaterThanToken);

-        public XmlElementStartTagSyntax WithLessThanToken(SyntaxToken lessThanToken);

-        public XmlElementStartTagSyntax WithName(XmlNameSyntax name);

-    }
-    public sealed class XmlElementSyntax : XmlNodeSyntax {
 {
-        public SyntaxList<XmlNodeSyntax> Content { get; }

-        public XmlElementEndTagSyntax EndTag { get; }

-        public XmlElementStartTagSyntax StartTag { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public XmlElementSyntax AddContent(params XmlNodeSyntax[] items);

-        public XmlElementSyntax AddStartTagAttributes(params XmlAttributeSyntax[] items);

-        public XmlElementSyntax Update(XmlElementStartTagSyntax startTag, SyntaxList<XmlNodeSyntax> content, XmlElementEndTagSyntax endTag);

-        public XmlElementSyntax WithContent(SyntaxList<XmlNodeSyntax> content);

-        public XmlElementSyntax WithEndTag(XmlElementEndTagSyntax endTag);

-        public XmlElementSyntax WithStartTag(XmlElementStartTagSyntax startTag);

-    }
-    public sealed class XmlEmptyElementSyntax : XmlNodeSyntax {
 {
-        public SyntaxList<XmlAttributeSyntax> Attributes { get; }

-        public SyntaxToken LessThanToken { get; }

-        public XmlNameSyntax Name { get; }

-        public SyntaxToken SlashGreaterThanToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public XmlEmptyElementSyntax AddAttributes(params XmlAttributeSyntax[] items);

-        public XmlEmptyElementSyntax Update(SyntaxToken lessThanToken, XmlNameSyntax name, SyntaxList<XmlAttributeSyntax> attributes, SyntaxToken slashGreaterThanToken);

-        public XmlEmptyElementSyntax WithAttributes(SyntaxList<XmlAttributeSyntax> attributes);

-        public XmlEmptyElementSyntax WithLessThanToken(SyntaxToken lessThanToken);

-        public XmlEmptyElementSyntax WithName(XmlNameSyntax name);

-        public XmlEmptyElementSyntax WithSlashGreaterThanToken(SyntaxToken slashGreaterThanToken);

-    }
-    public enum XmlNameAttributeElementKind : byte {
 {
-        Parameter = (byte)0,

-        ParameterReference = (byte)1,

-        TypeParameter = (byte)2,

-        TypeParameterReference = (byte)3,

-    }
-    public sealed class XmlNameAttributeSyntax : XmlAttributeSyntax {
 {
-        public override SyntaxToken EndQuoteToken { get; }

-        public override SyntaxToken EqualsToken { get; }

-        public IdentifierNameSyntax Identifier { get; }

-        public override XmlNameSyntax Name { get; }

-        public override SyntaxToken StartQuoteToken { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public XmlNameAttributeSyntax Update(XmlNameSyntax name, SyntaxToken equalsToken, SyntaxToken startQuoteToken, IdentifierNameSyntax identifier, SyntaxToken endQuoteToken);

-        public XmlNameAttributeSyntax WithEndQuoteToken(SyntaxToken endQuoteToken);

-        public XmlNameAttributeSyntax WithEqualsToken(SyntaxToken equalsToken);

-        public XmlNameAttributeSyntax WithIdentifier(IdentifierNameSyntax identifier);

-        public XmlNameAttributeSyntax WithName(XmlNameSyntax name);

-        public XmlNameAttributeSyntax WithStartQuoteToken(SyntaxToken startQuoteToken);

-    }
-    public sealed class XmlNameSyntax : CSharpSyntaxNode {
 {
-        public SyntaxToken LocalName { get; }

-        public XmlPrefixSyntax Prefix { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public XmlNameSyntax Update(XmlPrefixSyntax prefix, SyntaxToken localName);

-        public XmlNameSyntax WithLocalName(SyntaxToken localName);

-        public XmlNameSyntax WithPrefix(XmlPrefixSyntax prefix);

-    }
-    public abstract class XmlNodeSyntax : CSharpSyntaxNode

-    public sealed class XmlPrefixSyntax : CSharpSyntaxNode {
 {
-        public SyntaxToken ColonToken { get; }

-        public SyntaxToken Prefix { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public XmlPrefixSyntax Update(SyntaxToken prefix, SyntaxToken colonToken);

-        public XmlPrefixSyntax WithColonToken(SyntaxToken colonToken);

-        public XmlPrefixSyntax WithPrefix(SyntaxToken prefix);

-    }
-    public sealed class XmlProcessingInstructionSyntax : XmlNodeSyntax {
 {
-        public SyntaxToken EndProcessingInstructionToken { get; }

-        public XmlNameSyntax Name { get; }

-        public SyntaxToken StartProcessingInstructionToken { get; }

-        public SyntaxTokenList TextTokens { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public XmlProcessingInstructionSyntax AddTextTokens(params SyntaxToken[] items);

-        public XmlProcessingInstructionSyntax Update(SyntaxToken startProcessingInstructionToken, XmlNameSyntax name, SyntaxTokenList textTokens, SyntaxToken endProcessingInstructionToken);

-        public XmlProcessingInstructionSyntax WithEndProcessingInstructionToken(SyntaxToken endProcessingInstructionToken);

-        public XmlProcessingInstructionSyntax WithName(XmlNameSyntax name);

-        public XmlProcessingInstructionSyntax WithStartProcessingInstructionToken(SyntaxToken startProcessingInstructionToken);

-        public XmlProcessingInstructionSyntax WithTextTokens(SyntaxTokenList textTokens);

-    }
-    public sealed class XmlTextAttributeSyntax : XmlAttributeSyntax {
 {
-        public override SyntaxToken EndQuoteToken { get; }

-        public override SyntaxToken EqualsToken { get; }

-        public override XmlNameSyntax Name { get; }

-        public override SyntaxToken StartQuoteToken { get; }

-        public SyntaxTokenList TextTokens { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public XmlTextAttributeSyntax AddTextTokens(params SyntaxToken[] items);

-        public XmlTextAttributeSyntax Update(XmlNameSyntax name, SyntaxToken equalsToken, SyntaxToken startQuoteToken, SyntaxTokenList textTokens, SyntaxToken endQuoteToken);

-        public XmlTextAttributeSyntax WithEndQuoteToken(SyntaxToken endQuoteToken);

-        public XmlTextAttributeSyntax WithEqualsToken(SyntaxToken equalsToken);

-        public XmlTextAttributeSyntax WithName(XmlNameSyntax name);

-        public XmlTextAttributeSyntax WithStartQuoteToken(SyntaxToken startQuoteToken);

-        public XmlTextAttributeSyntax WithTextTokens(SyntaxTokenList textTokens);

-    }
-    public sealed class XmlTextSyntax : XmlNodeSyntax {
 {
-        public SyntaxTokenList TextTokens { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public XmlTextSyntax AddTextTokens(params SyntaxToken[] items);

-        public XmlTextSyntax Update(SyntaxTokenList textTokens);

-        public XmlTextSyntax WithTextTokens(SyntaxTokenList textTokens);

-    }
-    public sealed class YieldStatementSyntax : StatementSyntax {
 {
-        public ExpressionSyntax Expression { get; }

-        public SyntaxToken ReturnOrBreakKeyword { get; }

-        public SyntaxToken SemicolonToken { get; }

-        public SyntaxToken YieldKeyword { get; }

-        public override void Accept(CSharpSyntaxVisitor visitor);

-        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public YieldStatementSyntax Update(SyntaxToken yieldKeyword, SyntaxToken returnOrBreakKeyword, ExpressionSyntax expression, SyntaxToken semicolonToken);

-        public YieldStatementSyntax WithExpression(ExpressionSyntax expression);

-        public YieldStatementSyntax WithReturnOrBreakKeyword(SyntaxToken returnOrBreakKeyword);

-        public YieldStatementSyntax WithSemicolonToken(SyntaxToken semicolonToken);

-        public YieldStatementSyntax WithYieldKeyword(SyntaxToken yieldKeyword);

-    }
-}
```


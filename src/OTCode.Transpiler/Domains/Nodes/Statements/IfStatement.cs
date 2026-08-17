// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using OTCode.Core.Models.Transpiler.Nodes.Expressions;
using OTCode.Core.Models.Transpiler.Text;

namespace OTCode.Core.Models.Transpiler.Nodes.Statements;

public sealed class IfStatement : Statement
{
    public Expression Condition {get;}
    public IReadOnlyList<Statement> IfBranches {get;}
    public IReadOnlyList<Statement>? ElseBranches {get;}

    public IfStatement(
        Expression condition,
        IReadOnlyList<Statement> ifBranches,
        IReadOnlyList<Statement>? elseBranches,
        TextSpan span,
        LinePosition position
    ) : base(span, position)
    {
        Condition = condition ?? throw new ArgumentNullException(nameof(condition));
        IfBranches = ifBranches ?? throw new ArgumentNullException(nameof(ifBranches));
        ElseBranches = elseBranches;
    }
}
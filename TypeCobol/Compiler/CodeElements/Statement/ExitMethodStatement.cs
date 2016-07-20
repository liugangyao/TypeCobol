﻿using System;

namespace TypeCobol.Compiler.CodeElements
{
    /// <summary>
    /// p336: EXIT METHOD statement
    /// The EXIT METHOD statement specifies the end of an invoked method.
    /// </summary>
    public class ExitMethodStatement : StatementElement
    {
        public ExitMethodStatement() : base(CodeElementType.ExitMethodStatement, StatementType.ExitMethodStatement)
        { }
    }
}

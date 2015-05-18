﻿using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using TypeCobol.Compiler.AntlrUtils;
using TypeCobol.Compiler.CodeModel;
using TypeCobol.Compiler.Directives;
using TypeCobol.Compiler.File;
using TypeCobol.Compiler.Parser;
using TypeCobol.Compiler.Text;

namespace TypeCobol.Compiler.TypeChecker
{
    public class SemanticsDocument : IObserver<ParseNodeChangedEvent>
    {
        /// <summary>
        /// Underlying SyntaxTree
        /// </summary>
        public SyntaxDocument SyntaxDocument { get; private set; }

        /// <summary>
        /// Root object representing the Cobol program
        /// </summary>
        public Program Program { get; private set; }

        /// <summary>
        /// Root object representing the Cobol class
        /// </summary>
        public Class Class { get; private set; }

        /// <summary>
        /// List of errors found while analyzing the program or the class
        /// </summary>
        public IList<TypeDiagnostic> Diagnostics { get; private set; }

        /// <summary>
        /// Cobol text generated by the TypeCobol transpiler
        /// </summary>
        public ITextDocument GeneratedTextDocument { get; private set; }

        /// <summary>
        /// Cobol file where the text generated by the TypeCobol transpiler is saved
        /// </summary>
        public CobolFile GeneratedCobolFile { get; private set; }

        /// <summary>
        /// Compiler options directing the type checker operations
        /// </summary>
        public TypeCobolOptions CompilerOptions { get; private set; }

        public SemanticsDocument(SyntaxDocument parseTree, TypeCobolOptions compilerOptions)
        {
            SyntaxDocument = parseTree;
            CompilerOptions = compilerOptions;
        }

        public void OnNext(ParseNodeChangedEvent parseEvent)
        {
            // Analyse result with the type checker
            CobolTypeChecker typeChecker = new CobolTypeChecker();
            //walker.Walk(typeChecker, SyntaxDocument.ParseTree);
            Diagnostics = typeChecker.Diagnostics;

            // Send compilation errors event
            IList<CompilationError> errors = new List<CompilationError>();
            foreach (TypeDiagnostic diag in typeChecker.Diagnostics)
            {
                errors.Add(new CompilationError() { LineNumber = diag.StartToken.Line, StartColumn = diag.StartToken.Column, Message = diag.Message });
            }
            foreach (ParserDiagnostic diag in SyntaxDocument.Diagnostics)
            {
                errors.Add(new CompilationError() { LineNumber = diag.OffendingSymbol.Line /* temp patch -> */ == 0 ? 1 : diag.OffendingSymbol.Line, StartColumn = diag.ColumnStart, EndColumn = diag.ColumnEnd, Message = diag.Message });
            }
            compilationErrorsEventsSource.OnNext(errors);
        }

        public void OnCompleted()
        {
            // do nothing here
        }

        public void OnError(Exception error)
        {
            // do nothing here
        }

        // --- Implement IObservable<CodeModelChangedEvent>

        private ISubject<CodeModelChangedEvent> codeModelChangedEventsSource = new Subject<CodeModelChangedEvent>();

        public IObservable<CodeModelChangedEvent> CodeModelChangedEventsSource
        {
            get { return codeModelChangedEventsSource; }
        }

        // --- Implement IObservable<IList<CompilationError>>

        private ISubject<IList<CompilationError>> compilationErrorsEventsSource = new Subject<IList<CompilationError>>();

        public IObservable<IList<CompilationError>> CompilationErrorsEventsSource
        {
            get { return compilationErrorsEventsSource; }
        }
    }

    /// <summary>
    /// Temporary diagnostic class for the demo
    /// </summary>
    public class CompilationError
    {
        public int LineNumber { get; set; }
        public int StartColumn { get; set; }
        public int EndColumn { get; set; }

        public string Message { get; set; }
    }
}
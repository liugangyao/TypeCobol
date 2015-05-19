# TypeCobol

Prototype of an incremental Cobol compiler front-end for IBM Enterprise Cobol 5.1 for zOS syntax.

This prototype is a work in progress and is currently written in C#.

# Open Source License

The code of this repository is published under the CeCILL-C v1 Open Source Licence, which is very similar to the **LGPL** but was adapted to be compatible with the french law.

http://www.cecill.info/licences/Licence_CeCILL-C_V1-en.html

Summary :

- The Licensee is authorized to use the Software, without any limitation as to its fields of application.

- The Licensee acknowledges that the Software is supplied "as is" by the Licensor without any other express or tacit warranty.

- DISTRIBUTION OF SOFTWARE WITHOUT MODIFICATION : 
  The Licensee is authorized to distribute true copies of the Software in Source Code or Object Code form, provided that said distribution complies with all the provisions of the Agreement and is accompanied by:
    1.a copy of the Agreement,
    2.a notice relating to the limitation of both the Licensor's warranty and liability as set forth in Articles 8 and 9, 
    and 3.the Licensee allows effective access to the full Source Code of the Software.
    
- DISTRIBUTION OF MODIFIED SOFTWARE : 
  The Licensee is authorized to distribute the Modified Software, in source code or object code form, provided that said distribution complies with all the provisions of the Agreement and is accompanied by: 
    1.a copy of the Agreement,
    2.a notice relating to the limitation of both the Licensor's warranty and liability as set forth in Articles 8 and 9, 
    and 3.the Licensee allows effective access to the full source code of the Modified Software.
    
- DISTRIBUTION OF DERIVATIVE SOFTWARE :
  When the Licensee creates Derivative Software, this Derivative Software may be distributed under a license agreement other than this Agreement, subject to compliance with the requirement to include a notice concerning the rights over the Software.


# Architecture overview

## Visual Studio solution and projects

The solution was uploaded to Github using [Visual Studio 2015 Community RC](http://go.microsoft.com/fwlink/?LinkId=524433) and the [GitHub Extension for Visual Studio](https://visualstudiogallery.msdn.microsoft.com/75be44fb-0794-4391-8865-c3279527e97d).

The best way to test this project is to download and install both tools (for free) on your local machine, [login to Github from Visual Studio Team Explorer](http://channel9.msdn.com/Series/ConnectOn-Demand/217), then refresh this page and click on the *Open in Visual Studio* button which should appear on the right of the repository : this action will clone the solution in your local Git repository and open it in Visual Studio.

The solution contains 4 projects :
- **TypeCobol** is the main project, it implements a complete Cobol compiler front-end
- **TypeCobol.Grammar** uses Antlr4 to generate C# parsers for Cobol compiler directives and Cobol statements
- **TypeCobol.Test** provides unit tests which can be launched from the *Test Explorer* in Visual Studio
- **TypeCobolStudio** is a sample code editor used for visual demos

## Dependencies on third party librairies

The following librairies are included in the Visual Studio projects by the Nuget package manager :

- [ANTLR 4](http://www.antlr.org/) : The [C# target](https://github.com/sharwell/antlr4cs) of the ANTLR 4 parser generator for Visual Studio 2010+ projects.

- [ANTLR 4 Runtime](https://github.com/sharwell/antlr4cs) : The runtime library for parsers generated by the C# target of ANTLR 4. This package supports projects targeting .NET 2.0 or newer, and built using Visual Studio 2008 or newer.

- [Reactive Extensions - Main Library](http://introtorx.com/) : Reactive Extensions Main Library combining the interfaces, core, LINQ, and platform services libraries. The Reactive Extensions (Rx) is a library to compose asynchronous and event-based programs using observable collections and LINQ-style query operators.

- [System.Collections.Immutable](http://blogs.msdn.com/b/dotnet/archive/2013/09/25/immutable-collections-ready-for-prime-time.aspx) : This package provides collections that are thread safe and guaranteed to never change their contents, also known as immutable collections. Like strings, any methods that perform modifications will not change the existing instance but instead return a new instance. For efficiency reasons, the implementation uses a sharing mechanism to ensure that newly created instances share as much data as possible with the previous instance while ensuring that operations have a predictable time complexity.
 
- [AvalonEdit](http://avalonedit.net/) : AvalonEdit is a WPF-based text editor component. It was written by Daniel Grunwald for the SharpDevelop IDE. Starting with version 5.0, AvalonEdit is released under the MIT license.

## TypeCobol project - Code analysis steps

### Compilation pipeline : Compiler/

**Input** : libraryName, textName, sourceFileProvider, compilerOptions

**Output** : TextDocument, TokensDocument, ProcessedTokensDocument, SyntaxDocument, SemanticsDocument

**Namespace** : TypeCobol.Compiler

Class | Description
---|---
CompilationProject | Collection of Cobol files compiled together, maintains a shared cache of preprocessed files.
CompilationDocument | Partial compilation pipeline, from file loading to preprocessor step, used for COPY files.
CompilationUnit | Complete compilation pipeline, from file loading to semantic analysis step, used for PROGRAM files.

### Step 1 : Compiler/File - File loading, Characters decoding, Line endings

#### Step 1.1 : File loading

**Input** : libraryName, textName

**Output** : binary Stream

**Namespace** : TypeCobol.Compiler.File

Class | Description
---|---
SourceFileProvider | Enables the compiler to find Cobol source files referenced by name in the Cobol syntax (collection of Cobol text libraries filtered by libraryName).
ICobolLibray | Common interface for Cobol text libraries (dictionary of files indexed by textName), could be implemented as a remote dataset on the mainframe, a repository in a version control system, or a simple directory on the local machine.
CobolFile | Abstract class representing a Cobol text file, with OpenInputStream and OpenOutputStream methods.
 
Implementation for files found in Windows directories on the local machine :

Class | Description
---|---
LocalDirectoryLibrary | Implementation of an ICobolLibrary as a local directory containing Cobol text files.
LocalCobolFile | Implementation of a CobolFile as a text file in the local filesystem.

#### Step 1.2 : Characters decoding and Line endings

**Input** : binary Stream, ibmCCSID (IBM Coded Character Set ID), fixedLineLength or endOfLineDelimiter

**Output** : IEnumerable<char> (stream of Unicode chars with normalized Cr/Lf line endings)

**Namespace** : TypeCobol.Compiler.File

Class / Method | Description
---|---
IBMCodePages | Gets the .Net Encoding equivalent for an IBM Coded Character Set ID.
CobolFile.ReadChars  | Reads the characters of the source file as Unicode characters. Inserts additional Cr/Lf characters at end of line for fixed length lines.

### Step 2 : Compiler/Text - Text lines, Source text areas

#### Step 2.1 : Text lines

**Input** : IEnumerable<char> (stream of Unicode chars with normalized Cr/Lf line endings)

**Output** : ITextDocument, a list of ITextLines

**Namespace** : TypeCobol.Compiler.Text

Class | Description
---|---
ITextDocument | Interface enabling the integration of the Cobol compiler with any kind of text editor. A document is both : an array of characters which can be accessed by offset from the beginning of the document, and a list of text lines, which can be accessed by their index in the list. A document sends notifications each time one of its lines is changed. 
ITextLine | Interface enabling the integration of the Cobol compiler with any kind of text editor. Each line has an index to describe its position in the document.

Implementation for a read-only text document in memory :

Class | Description
---|---
TextDocument | Immutable Cobol text document for batch compilation. Document loaded once from a file and never modified.
TextLine | Immutable Cobol line for batch compilation. Text line loaded once from a file and never modified.

Implementation for an interactive text editor in TypeCobolStudio :

Class | Description
---|---
AvalonEditTextDocument | Adapter used to implement the TypeCobol.Compiler.Text.ITextDocument interfaceon top of an AvalonEdit.TextDocument instance.

#### Step 2.2 : Source text areas (columns reference format)

**Input** : ITextLine, ColumnsLayout

**Output** : TextLineMap, a list of source text areas (SequenceNumber, Indicator, Source, Comment)

**Namespace** : TypeCobol.Compiler.Text

Class | Description
---|---
ColumnsLayout | *CobolReferenceFormat* : fixed-form reference format / Columns 1-6 = Sequence number / Column 7 = Indicator / Columns 8-72 = Text Area A and Area B / Columns 73+ = Comment, or *Free-form format* : there is not limit on the size a source line / the first seven characters of each line are considered part of the normal source line and may contain COBOL source code / column 1 takes the role of the indicator area / there is no fixed right margin, but floating comment indicators : *>.
TextAreaType | Enumeration of the standard text areas : SequenceNumber, Indicator, Source, Comment.
TextArea | Portion of a text line (StartIndex / EndIndex) with a specific meaning.
TextLineMap | Partition of a COBOL source line into reference format areas (also detects a list of compiler directives keywords which can be encountered before column 8 even in Cobol reference format).
TextLineType | Line types defined in the Cobol reference format : Source, Debug, Comment, Continuation, Invalid, Blank.

### Step 3 : Compiler/Scanner - Lexical analysis & Line continuations

### Step 4 : Compiler/Preprocessor - Compiler directives, COPY & REPLACE

### Step 5 : Compiler/Parser - Code elements parsing & Code model

### Step 6 : Compiler/TypeChecker - Semantic analysis & Type checking

### Step 7 : Compiler/Generator - Cobol source code generation (from TypeCobol extended syntax)

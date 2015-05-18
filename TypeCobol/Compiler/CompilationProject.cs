﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TypeCobol.Compiler.CodeModel;
using TypeCobol.Compiler.Directives;
using TypeCobol.Compiler.File;
using TypeCobol.Compiler.Preprocessor;
using TypeCobol.Compiler.Text;

namespace TypeCobol.Compiler
{
    /// <summary>
    /// Collection of linked Cobol files grouped to be compiled together
    /// </summary>
    public class CompilationProject : IProcessedTokensDocumentProvider
    {
        // -- Project creation and persistence --

        /// <summary>
        /// Create a new Cobol compilation project in a local directory
        /// </summary>
        public CompilationProject(string projectName, string rootDirectory, string[] fileExtensions, Encoding encoding, EndOfLineDelimiter endOfLineDelimiter, int fixedLineLength, ColumnsLayout columnsLayout, TypeCobolOptions compilationOptions)
        {
            Name = projectName;
            RootDirectory = rootDirectory;
            SourceFileProvider = new SourceFileProvider();
            rootDirectoryLibrary = SourceFileProvider.AddLocalDirectoryLibrary(rootDirectory, true, fileExtensions, encoding, endOfLineDelimiter, fixedLineLength);
            defaultColumnsLayout = columnsLayout;
            defaultCompilationOptions = compilationOptions;
                        
            CobolFiles = new Dictionary<string, CobolFile>();
            CobolTextReferences = new Dictionary<string, CobolFile>();
            CobolProgramCalls = new Dictionary<string, CobolFile>();
        }
        
        /// <summary>
        /// Save project configuration to an XML file in the project root directory
        /// </summary>
        public void Save()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Load project configuration from an XML file in the project root directory
        /// </summary>
        public static CompilationProject Load(string projectName, string rootDirectory)
        {
            throw new NotImplementedException();
        }

        // -- Project properties --

        /// <summary>
        /// Project Name (read-only)
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Root directory for all project files (read-only)
        /// </summary>
        public string RootDirectory { get; private set; }

        /// <summary>
        /// Set of text libraries where all the source files referenced by this project can be found.
        /// Initialized with the project root directory and its subdirectories.
        /// Call SourceFileProvider.AddLocalDirectoryLibrary() or SourceFileProvider.AddCobolLibrary()
        /// to enable other text libraries.
        /// </summary>
        public SourceFileProvider SourceFileProvider { get; private set; }
        
        // Used for file creation and file import in the root directory
        ICobolLibrary rootDirectoryLibrary;

        // Default ColumnsLayout for text in this project
        ColumnsLayout defaultColumnsLayout;
        // Default Compilation options for programs in this project
        TypeCobolOptions defaultCompilationOptions;

        // -- Files manipulation --

        /// <summary>
        /// Files added explicitely to the projet by the developper (read-only)
        /// </summary>
        public IDictionary<string, CobolFile> CobolFiles { get; private set; }

        /// <summary>
        /// Create a new file and add it to the project
        /// </summary>
        public CobolFile CreateNewFile(string cobolFileName)
        {
            CobolFile cobolFile = rootDirectoryLibrary.CreateNewFile(cobolFileName, RootDirectory + "/" + cobolFileName);
            CobolFiles.Add(cobolFileName, cobolFile);
            return cobolFile;
        }

        /// <summary>
        /// Import an existing file in the project
        /// </summary>
        public CobolFile ImportExistingFile(string cobolFileName)
        {
            CobolFile cobolFile = null;
            if(rootDirectoryLibrary.TryGetFile(cobolFileName, out cobolFile))
            {
                CobolFiles.Add(cobolFileName, cobolFile);
            }
            return cobolFile;
        }

        /// <summary>
        /// Remove a file from the project
        /// </summary>
        public void RemoveFile(CobolFile cobolFile)
        {
            if (rootDirectoryLibrary.ContainsFile(cobolFile.Name))
            {
                rootDirectoryLibrary.RemoveFile(cobolFile.Name, cobolFile.FullPath);
            }
            CobolFiles.Remove(cobolFile.Name);
        }

        /// <summary>
        /// Add a Cobol text reference found while compiling one file of the project
        /// </summary>
        public CobolFile AddCobolTextReference(string cobolTextName)
        {
            CobolFile cobolFile = null;
            if (SourceFileProvider.TryGetFile(cobolTextName, out cobolFile))
            {
                CobolTextReferences.Add(cobolTextName, cobolFile);
            }
            return cobolFile;
        }

        /// <summary>
        /// Text file referenced by at least one file in the project and automatically imported by the compiler (read-only)
        /// </summary>
        public IDictionary<string, CobolFile> CobolTextReferences { get; private set; }

        /// <summary>
        /// Add a Cobol program call found while compiling one file of the project
        /// </summary>
        public CobolFile AddCobolProgramCalls(string cobolProgramName)
        {
            CobolFile cobolFile = null;
            if (SourceFileProvider.TryGetFile(cobolProgramName, out cobolFile))
            {
                CobolTextReferences.Add(cobolProgramName, cobolFile);
            }
            return cobolFile;
        }

        /// <summary>
        /// Program external to the project and called by at least one file in the project and automatically imported by the compiler (read-only)
        /// </summary>
        public IDictionary<string, CobolFile> CobolProgramCalls { get; private set; }

        // -- Implementation of IProcessedTokensDocumentProvider interface --

        // Cache for all the compilation documents imported by COPY directives in this project
        IDictionary<string, CompilationDocument> importedCompilationDocumentsCache = new Dictionary<string, CompilationDocument>();

        /// <summary>
        /// Returns a ProcessedTokensDocument already in cache or loads, scans and processes a new CompilationDocument
        /// </summary>
        public ProcessedTokensDocument GetProcessedTokensDocument(string libraryName, string textName)
        {
            string cacheKey = (libraryName == null ? SourceFileProvider.DEFAULT_LIBRARY_NAME : libraryName.ToUpper()) + "." + textName.ToUpper();
            CompilationDocument resultDocument = null;
            if(importedCompilationDocumentsCache.ContainsKey(cacheKey))
            {
                resultDocument = importedCompilationDocumentsCache[cacheKey];
            }
            else
            {
                resultDocument = new CompilationDocument(libraryName, textName, SourceFileProvider, this, defaultColumnsLayout, defaultCompilationOptions);
                resultDocument.SetupDocumentProcessingPipeline(null, 0);
                resultDocument.StartDocumentProcessing();

                importedCompilationDocumentsCache[cacheKey] = resultDocument;
            }
            return resultDocument.ProcessedTokensDocument;
        }
    } 
}
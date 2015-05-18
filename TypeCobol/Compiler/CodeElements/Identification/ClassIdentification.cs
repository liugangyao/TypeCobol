﻿using System;

namespace TypeCobol.Compiler.CodeElements
{
    public class ClassIdentification : CodeElement
    {
        public ClassIdentification() : base(CodeElementType.ClassIdentification)
        {
            AuthoringProperties = new AuthoringProperties();
        }

        /// <summary>
        /// class-name
        /// A user-defined word that identifies the class. class-name can optionally
        /// have an entry in the REPOSITORY paragraph of the configuration section
        /// of the class definition.
        /// </summary>
        public ClassName Name { get; set; }

        /// <summary>
        /// INHERITS
        /// A clause that defines class-name-1 to be a subclass (or derived class) of
        /// class-name-2 (the parent class). class-name-1 cannot directly or indirectly
        /// inherit from class-name-1.
        /// class-name-2
        /// The name of a class inherited by class-name-1. You must specify class-name-2
        /// in the REPOSITORY paragraph of the configuration section of the class
        /// definition.
        /// The semantics of inheritance are as defined by Java. All classes must be derived
        /// directly or directly from the java.lang.Object class.
        /// Java supports single inheritance; that is, no class can inherit directly from more
        /// than one parent. Only one class-name can be specified in the INHERITS phrase of
        /// a class definition.
        /// </summary>
        public SymbolReference<ClassName> InheritsFromClassName { get; set; }

        /// <summary>
        /// Some optional paragraphs in the IDENTIFICATION DIVISION can be omitted.
        /// The optional paragraphs are: 
        /// AUTHOR, INSTALLATION, DATE-WRITTEN, DATE-COMPILED, SECURITY
        /// </summary>
        public AuthoringProperties AuthoringProperties { get; set; }
    }
}
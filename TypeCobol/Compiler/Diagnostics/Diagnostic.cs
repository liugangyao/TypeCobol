﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeCobol.Compiler.Diagnostics
{
    /// <summary>
    /// Detailed error message for the end user, with the location of the problem dected in the source text
    /// </summary>
    public class Diagnostic
    {
        public Diagnostic(MessageCode messageCode, int columnStart, int columnEnd, int lineNumber, params object[] messageArgs)
        {
            Info = DiagnosticMessage.GetFromCode[(int)messageCode];

            ColumnStart = Math.Max(columnStart, 0);
            ColumnEnd   = Math.Max(columnEnd,   0);

            Line = lineNumber;

            Message = String.Format(Info.MessageTemplate, messageArgs ?? new object[0]);
            MessageArgs = messageArgs;
        }

        public DiagnosticMessage Info { get; set; }

        public int ColumnStart { get; set; }
        public int ColumnEnd { get; set; }

        public int Line { get; set; }

        public string Message { get; set; }
        internal object[] MessageArgs { get; set; }

         /// <summary>
        /// Text representation of a token for debugging or test purposes
        /// </summary>
        public override string ToString()
        {
            return string.Format("Line {0}[{1},{2}] <{3}, {4}, {5}> - {6}", Line, ColumnStart, ColumnEnd, Info.Code, Info.Severity, Info.Category, Message);
        }
    }
}

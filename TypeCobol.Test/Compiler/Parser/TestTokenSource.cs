﻿using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TypeCobol.Compiler;
using TypeCobol.Compiler.AntlrUtils;
using TypeCobol.Compiler.Parser;
using TypeCobol.Compiler.Scanner;
using TypeCobol.Compiler.Text;

namespace TypeCobol.Test.Compiler.Parser
{
    static class TestTokenSource
    {
        public static void Check_CobolCharStream()
        {
            // Test file properties
            string relativePath = @"Compiler\Parser\Samples";
            string textName = "MSVCOUT";
            DocumentFormat documentFormat = DocumentFormat.RDZReferenceFormat;

            // Compile test file
            CompilationDocument compilationDocument = ParserUtils.ScanCobolFile(relativePath, textName, documentFormat);

            // Create a token iterator on top of tokens lines
            TokensLinesIterator tokensIterator = new TokensLinesIterator(
                compilationDocument.TextDocument.FileName,
                compilationDocument.TokensDocument.TokensLines,
                null,
                Token.CHANNEL_SourceTokens);

            // Crate an Antlr compatible token source on top a the token iterator
            TokensDocumentTokenSource tokenSource = new TokensDocumentTokenSource(
                compilationDocument.TextDocument,
                tokensIterator);

            // Get underlying CharStream
            ICharStream charStream = tokenSource.InputStream;

            if(charStream.Index != 0)
            {
                throw new Exception("Char stream index should start at 0");
            }
            if (charStream.La(0) != 0)
            {
                throw new Exception("La(0) should be 0");
            }
            if (charStream.La(1) != '0')
            {
                throw new Exception("La(1) should be 0");
            }
            if (charStream.La(5) != '1')
            {
                throw new Exception("La(5) should be 1");
            }
            if (charStream.La(5) != '1')
            {
                throw new Exception("La(5) should be 1");
            }

            charStream.Consume();
            if (charStream.Index != 1)
            {
                throw new Exception("Char stream index should be 1 after consume");
            }
            if (charStream.La(4) != '1')
            {
                throw new Exception("La(4) should be 1 after consume");
            }
            if (charStream.La(17921) != IntStreamConstants.Eof)
            {
                throw new Exception("La(17921) should be Eof");
            }
            
            charStream.Seek(88);
            if(charStream.Index != 88)
            {
                throw new Exception("Char stream index should be 88 after seek");
            }
            if (charStream.La(-1) != '-')
            {
                throw new Exception("La(-1) should be - after seek");
            }
            if (charStream.La(1) != 'M')
            {
                throw new Exception("La(1) should be M after seek");
            }
            // should do nothing
            int marker = charStream.Mark();
            charStream.Release(marker);
            if (charStream.La(2) != 'a')
            {
                throw new Exception("La(2) should be a after release");
            }

            string text = charStream.GetText(new Interval(220,300));
            if (text != "========    00002101000040*-Descent : 14/10/13 at 17:27:08         MemoId  : JAEG")
            {
                throw new Exception("Char stream GetText method KO");
            }

            if (charStream.Size != 17920)
            {
                throw new Exception("Char stream size KO");
            }

            if (charStream.SourceName != "MSVCOUT")
            {
                throw new Exception("Char stream name KO");
            }
        }

        public static void Check_CobolTokenSource()
        {
            // Test file properties
            string relativePath = @"Compiler\Parser\Samples";
            string textName = "MSVCOUT";
            DocumentFormat docFormat = DocumentFormat.RDZReferenceFormat;

            // Compile test file
            CompilationDocument compilationDocument = ParserUtils.ScanCobolFile(relativePath, textName, docFormat);

            // Create a token iterator on top of tokens lines
            TokensLinesIterator tokensIterator = new TokensLinesIterator(
                compilationDocument.TextDocument.FileName,
                compilationDocument.TokensDocument.TokensLines,
                null,
                Token.CHANNEL_SourceTokens);

            // Crate an Antlr compatible token source on top a the token iterator
            TokensDocumentTokenSource tokenSource = new TokensDocumentTokenSource(
                compilationDocument.TextDocument,
                tokensIterator);

            if (tokenSource.SourceName != "MSVCOUT")
            {
                throw new Exception("Token source name KO");
            }

            IToken token = tokenSource.TokenFactory.Create((int)TokenType.ACCEPT, "AccePt");
            if (token.Channel != Token.CHANNEL_SourceTokens || token.Column != 1 || token.Line != 1 || 
                token.StartIndex != 0 || token.StopIndex != 5 || token.Text != "AccePt" || 
                token.TokenIndex != -1 || token.InputStream != null || token.TokenSource !=null ||
                token.Type != (int)TokenType.ACCEPT)
            {
                throw new Exception("TokenFactory first Create method KO");
            }

            var source = new Tuple<ITokenSource, ICharStream>(tokenSource, tokenSource.InputStream);
            token = tokenSource.TokenFactory.Create(source, (int)TokenType.IntegerLiteral, "314", Token.CHANNEL_CompilerDirectives, 10, 20, 30, 17);
            if (token.Channel != Token.CHANNEL_CompilerDirectives || token.Column != 1 || token.Line != 30 ||
                token.StartIndex != 0 || token.StopIndex != 2 || token.Text != "314" ||
                token.TokenIndex != -1 || token.InputStream == null || token.TokenSource == null ||
                token.Type != (int)TokenType.IntegerLiteral || ((IntegerLiteralValue)((Token)token).LiteralValue).Number != 314)
            {
                throw new Exception("TokenFactory second Create method KO");
            }
            
            if(tokenSource.Column != 0)
            {
                throw new Exception("Token source column should be 0 at start");
            }
            if (tokenSource.Line != 1)
            {
                throw new Exception("Token source line should be 1 at start");
            }

            IList<IToken> tokensList = new List<IToken>();
            for(int i=0 ; token.Type != (int)TokenType.EndOfFile; i++)
            {
                token = tokenSource.NextToken();
                tokensList.Add(token);
            }
            if(tokensList.Count != 293 ||
                tokensList[0].Text != "01" ||
                tokensList[1].Text != ":MSVCOUT:" ||
                tokensList[290].Text != "'/MSVCOUT'" ||
                tokensList[292].Type != (int)TokenType.EndOfFile)
            {
                throw new Exception("Token source nextToken method KO");
            }
        }

        public static void Check_CobolTokenSource_WithStartToken()
        {
            // Test file properties
            string relativePath = @"Compiler\Parser\Samples";
            string textName = "MSVCOUT";
            Encoding encoding = Encoding.GetEncoding(1252);
            DocumentFormat docFormat = DocumentFormat.RDZReferenceFormat;

            // Compile test file
            CompilationDocument compilationDocument = ParserUtils.ScanCobolFile(relativePath, textName, docFormat);

            // Search for first level 88 as a start token
            Token startToken = compilationDocument.TokensDocument.SourceTokens.First(t => (t.TokenType == TokenType.IntegerLiteral && ((IntegerLiteralValue)t.LiteralValue).Number == 88));

            // Create a token iterator on top of tokens lines
            TokensLinesIterator tokensIterator = new TokensLinesIterator(
                compilationDocument.TextDocument.FileName,
                compilationDocument.TokensDocument.TokensLines,
                startToken,
                Token.CHANNEL_SourceTokens);

            // Crate an Antlr compatible token source on top a the token iterator
            TokensDocumentTokenSource tokenSource = new TokensDocumentTokenSource(
                compilationDocument.TextDocument,
                tokensIterator);

            IToken token = null;
            IList<IToken> tokensList = new List<IToken>();
            for (int i = 0; i < 9 ; i++)
            {
                token = tokenSource.NextToken();
                tokensList.Add(token);
            }
            if (tokensList[0].Text != "88" ||
                tokensList[1].Text != ":MSVCOUT:-RtnCod-OK" ||
                tokensList[7].Text != "VALUE" ||
                tokensList[8].Text != "'STUB'")
            {
                throw new Exception("Token source nextToken method with start token KO");
            }
        }
    }
}
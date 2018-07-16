using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
// using Antlr4.Runtime;


namespace test_task
{


    public enum TokenType
    {
        LBracket,
        RBracket,
        LSquareBracket,
        RSquareBracket,

//        [Token(Example = "(")]
        LParentheses,
        
//        [Token(Example = ")")]
        RParentheses,

//        [Token(Example = ":")]
        Semicolon,

//        [Token(Example = "=")]
        Equals,

        Other,

//        [Token(Example = "int")]
        IntType,

//        [Token(Example = "if")]
        If,

//        [Token(Example = "System.out.println")]
        Sink,

        Number,
        Space,
        
        // Although JSON doesn't have an "identifier" or "keyword"
        // concept that groups `true`, `false`, and `null`, it's useful
        // for the tokenizer to be very permissive - it's more informative
        // to generate an error later at the parsing stage, e.g.
        // "unexpected identifier `flase`", instead of failing at the
        // tokenization stage where all we'd have is "unexpected `l`".
        Identifier,
        ThreeDots,
    }


    public class Token
    {
        public Token(string value, TokenType token) {
            Type = token;
            Value = value;
        }

        public TokenType Type { get; }
        public string Value { get; }

        public bool isSpace() {
            return this.Type == TokenType.Space;
        }

        public bool isIdentifier() {
            return this.Type == TokenType.Identifier;
        }

        public bool isConcatable() {
            return (
                this.isSpace() || 
                this.isIdentifier() && 
                !(new Regex(@"\d+$").IsMatch(this.Value))
            );
        }

        public static Token Match(string value) {
            switch(value)
            {
                case "{":
                    return new Token(value, TokenType.LBracket);
                case "}":
                    return new Token(value, TokenType.RBracket);
                case "[":
                    return new Token(value, TokenType.LSquareBracket);
                case "]":
                    return new Token(value, TokenType.RSquareBracket);
                case "(":
                    return new Token(value, TokenType.LParentheses);
                case ")":
                    return new Token(value, TokenType.RParentheses);
                case ";":
                    return new Token(value, TokenType.Semicolon);
                case "=":
                    return new Token(value, TokenType.Equals);
                case "if":
                    return new Token(value, TokenType.If);
                case "int":
                    return new Token(value, TokenType.IntType);
                case "...":
                    return new Token(value, TokenType.ThreeDots);
                case "System.out.println":
                    return new Token(value, TokenType.Sink);
                case var sv when new Regex(@"^[\s\r\n]+$").IsMatch(sv):
                    return new Token(value, TokenType.Space);
                case var sv when new Regex(@"^\d+$").IsMatch(sv):
                    return new Token(value, TokenType.Number);
                case var sv when new Regex(@"^[\w+_][\w_\.\d]*[\d\w_]*$").IsMatch(sv):
                    return new Token(value, TokenType.Identifier);
                default:
                    return null;
            }
      
            return null;
        }

    }

    public class Lexer
    {
        public static List<Token> Tokenize(string source)
        {
            List<Token> tokens = new List<Token>();
            List<char> buff = new List<char>();
            Token tok;
            List<char> old_buff = null;
            foreach (char letter in source) {
                buff.Add(letter);
                tok = Lexer.Match(buff);
                if (tok == null && old_buff != null) {
                    tokens.Add(Lexer.Match(old_buff));
                    old_buff = null;
                    buff = new List<char>();
                    buff.Add(letter);
                    tok = Lexer.Match(buff);
                }
                if (tok != null && !tok.isConcatable()) {
                    tokens.Add(tok);
                    buff = new List<char>();
                    old_buff = null;
                }
                else {
                    old_buff = new List<char>(buff);
                }
            }
            return tokens;
        }

        private static Token Match(List<char> buffer) {
            return Token.Match(
                string.Join("", buffer.ToArray())
            );
        }
    }


    static class Programm
    {
        public static int Main(string[] args)
        {

            //StreamReader sr;
            string source;
            if (args.Length == 0)
            {
                System.Console.WriteLine("You need to specify source file");
                return 1;
            }

            try
            {   // Open the text file using a stream reader.
                // using (StreamReader sr = new StreamReader(args[0]))
                // {
                // // Read the stream to a string, and write the string to the console.
                //     String line = sr.ReadToEnd();
                //     Console.WriteLine(line);
                // }
                // sr = new StreamReader(args[0]);
                source = System.IO.File.ReadAllText(args[0]);
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
                return 2;
            }
            // AntlrInputStream inputStream = new AntlrInputStream(sr);
            // JavaLexer javaLexer = new JavaLexer(inputStream);            
            // CommonTokenStream commonTokenStream = new CommonTokenStream(javaLexer);
            // JavaParser javaParser = new JavaParser(commonTokenStream);

            // JavaParser.ExpressionContext expressionContext = javaParser.expression();
            // JavaParserBaseVisitor visitor = new JavaParserBaseVisitor();

            // Console.WriteLine(visitor.Visit(expressionContext));
            // foreach (char letter in source) {
            //      Console.Write(letter);
            // }

            List<Token> tokens = Lexer.Tokenize(source);
            // List<Token> tokens = Lexer.Tokenize(source).Aggregate(
            //     new List<Token>(),
            //     (acc, val)
            //      => {
            //         //Console.Write("-->>>" + acc.Last().Value + " " + acc.Last().isSpace() +"\n");
            //         if (!(val.isSpace() && acc.Count() > 0 && acc.Last().isSpace())) {
            //             acc.Add(val);
            //         }
            //         return acc;
            //     } 
            // );
            foreach (Token tok in tokens) {
                 Console.Write(tok.Type + " " + tok.Value + "\n");
            }
            return 0;
        }
    }
}
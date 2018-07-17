using System;
using System.IO;
using System.Collections;
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
        LParentheses,
        RParentheses,
        Semicolon,
        Equals,
        Other,
        IntType,
        If,
        Sink,
        Number,
        Space,
        Identifier,
        ThreeDots,
    }

    public enum NodeType
    {
       Block,
       If,
       Declaration,
       Assigment,
       Lookup,
       VarName,
       Number,
    }

    public class TreeNode
    {
        public TreeNode(string value, NodeType type) {
            Type = type;
            Value = value;
        }
        
        public NodeType Type { get; }
        public string Value { get; }

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

    public class Parser
    {
        public Parser(IEnumerable<Token> input) {
            Source = input;
            Ast = new AST(null, null);
            this.Prepare();
        }


        public AST Ast { get; set; }
        public IEnumerable<Token> Source { get; set; }

        // due restrictions no need to handle anything before method body
        private void Prepare() {
            this.Source = (
                this.Source
                    .SkipWhile(t => t.Type != TokenType.LBracket)
                    .SkipWhile(t => t.Type != TokenType.LBracket)
            );
        }

        private void dumpParents(AST node) {
            while (!node.isRoot()) {
                Console.WriteLine(node.Node.Type);
                node = node.Parent;
            }
        }

        public AST Parse() {
            AST current = this.Ast;
            AST tmp = null;
            TreeNode tmpNode = null;
            foreach(Token token in this.Source) {
                switch(token.Type)
                {
                    case TokenType.LBracket:
                        current.Node = new TreeNode(token.Value, NodeType.Block);
                        tmp = new AST(current);
                        current.Children.Add(tmp);
                        current = tmp;
                        break;
                    case TokenType.RBracket:
                        if (current.isRoot()) {
                            Console.WriteLine("Parsing Error. No mathing bracket");
                            Environment.Exit(3);
                        }
                        current = current.Parent;
                        break;
                    case TokenType.If:
                        current.Node = new TreeNode(token.Value, NodeType.If);
                        tmp = new AST(current);
                        current.Children.Add(tmp);
                        current = tmp;
                        break;
                    case TokenType.LParentheses:
                        break;
                    case TokenType.RParentheses:
                        break;
                    case TokenType.Identifier:
                        if (current.Node != null && current.Node.Type == NodeType.VarName) {
                            tmpNode = current.Node;
                            current.Node = new TreeNode(null, NodeType.Declaration);
                            tmp = new AST(current);
                            tmp.Node = tmpNode;
                            current.Children.Add(tmp);
                            tmp = new AST(current);
                            tmp.Node = new TreeNode(token.Value, NodeType.VarName);
                            current.Children.Add(tmp);
                        } else {
                            current.Node = new TreeNode(token.Value, NodeType.VarName);
                        }
                        break;
                    case TokenType.Equals:
                        if (current.Node.Type != NodeType.VarName) {
                            Console.WriteLine("Equality not after varname");
                            this.dumpParents(current);
                            Environment.Exit(3);
                        }
                        tmpNode = current.Node;
                        current.Node = new TreeNode(null, NodeType.Assigment);
                        tmp = new AST(current);
                        tmp.Node = tmpNode;
                        current.Children.Add(tmp);
                        current.Node = new TreeNode(token.Value, NodeType.VarName);
                        break;
                    case TokenType.RSquareBracket:
                        break;
                    case TokenType.LSquareBracket:
                        if (current.Node.Type != NodeType.If) {
                            Console.WriteLine("Square bracket not after identifier");
                            Environment.Exit(3);
                        }
                        tmpNode = current.Node;
                        current.Node = new TreeNode(null, NodeType.Lookup);
                        tmp = new AST(current);
                        tmp.Node = tmpNode;
                        current.Children.Add(tmp);
                        break;
                    case TokenType.Number:

                        if (current.Node.Type == NodeType.Lookup && 
                            current.Node.Type == NodeType.Assigment ) {
                            
                            tmp = new AST(current);
                            tmp.Node = new TreeNode(token.Value, NodeType.Number);
                            current.Children.Add(tmp);
                        } else {
                            Console.WriteLine("Unexpected number");
                            Environment.Exit(3);
                        }
                        break;
                    default:
                        break;
                }
            }
            return this.Ast;
        }




    }


    public class AST
    {
        public AST(TreeNode node, AST parent) {
            Node = node;
            Parent = parent;
            Children = new List <AST>();
        }

        public AST(AST parent) {
            Node = null;
            Parent = parent;
            Children = new List <AST>();
        }


        public TreeNode Node { get; set; }
        public AST Parent { get; }
        public List<AST> Children { get; }


        public bool isLeaf() {
            return this.Children == null;
        }

        public bool isRoot() {
            return this.Parent == null;
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
            AST ast = new Parser(tokens).Parse();
            return 0;
        }
    }
}